using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LiteAnim.EditorView
{
    /// <summary>
    /// BlendTree 权重可视化预览组件（圆形扇形图）。
    /// 参照 BlendTreeMotionState 的阈值/权重计算方式，可视化展示：
    /// 1. 参数滑块（0~1）控制混合位置
    /// 2. 圆形扇形图：每个 Clip 占一个扇形，角度按阈值比例分配，激活的扇形高亮
    /// 3. 参数游标线从圆心指向当前参数角度
    /// 4. 各 Clip 当前激活权重标签
    /// </summary>
    public class BlendWeightPreviewElement : VisualElement
    {
        private readonly Label paramNameLabel;
        private readonly Slider paramSlider;
        private readonly Label paramValueLabel;
        private readonly WeightPieElement pieChart;
        private readonly VisualElement weightLabelsContainer;

        private LiteAnimMotion motion;

        public BlendWeightPreviewElement()
        {
            style.marginTop = 4;
            style.marginBottom = 4;

            // ---- 标题 ----
            var header = new Label("混合权重预览");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 2;
            header.style.marginLeft = 2;
            Add(header);

            // ---- 参数滑块行 ----
            var sliderRow = new VisualElement();
            sliderRow.style.flexDirection = FlexDirection.Row;
            sliderRow.style.alignItems = Align.Center;
            sliderRow.style.marginBottom = 2;

            paramNameLabel = new Label("Param");
            paramNameLabel.style.width = 50;
            paramNameLabel.style.marginLeft = 2;
            sliderRow.Add(paramNameLabel);

            paramSlider = new Slider(0f, 1f);
            paramSlider.style.flexGrow = 1;
            paramSlider.RegisterValueChangedCallback(OnParamChanged);
            sliderRow.Add(paramSlider);

            paramValueLabel = new Label("0.00");
            paramValueLabel.style.width = 36;
            paramValueLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            sliderRow.Add(paramValueLabel);

            Add(sliderRow);

            // ---- 圆形扇形图（左）+ 标签列表（右）----
            var chartRow = new VisualElement();
            chartRow.style.flexDirection = FlexDirection.Row;
            chartRow.style.alignItems = Align.FlexStart;
            chartRow.style.marginTop = 2;

            pieChart = new WeightPieElement();
            pieChart.style.width = 140;
            pieChart.style.height = 140;
            pieChart.style.flexShrink = 0;
            chartRow.Add(pieChart);

            weightLabelsContainer = new VisualElement();
            weightLabelsContainer.style.flexGrow = 1;
            weightLabelsContainer.style.marginLeft = 8;
            weightLabelsContainer.style.justifyContent = Justify.Center;
            chartRow.Add(weightLabelsContainer);

            Add(chartRow);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  外部接口
        // ─────────────────────────────────────────────────────────────────────

        public void SetMotion(LiteAnimMotion motion)
        {
            this.motion = motion;
            Refresh();
        }

        /// <summary>
        /// 完整刷新（Clip 数量或属性变化后调用）
        /// </summary>
        public void Refresh()
        {
            if (motion == null || motion.Clips.Count == 0)
            {
                style.display = DisplayStyle.None;
                return;
            }
            style.display = DisplayStyle.Flex;

            string pName = string.IsNullOrEmpty(motion.Param) ? "Param" : motion.Param;
            paramNameLabel.text = pName;

            UpdateVisualization();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  内部
        // ─────────────────────────────────────────────────────────────────────

        private void OnParamChanged(ChangeEvent<float> evt)
        {
            UpdateVisualization();

            using var e = AnimParamValueChangedEvent.GetPooled(evt.newValue);
            e.target = this;
            this.SendEvent(e);
        }

        private void UpdateVisualization()
        {
            if (motion == null || motion.Clips.Count == 0) return;

            var clips = motion.Clips;
            int count = clips.Count;
            float paramValue = paramSlider.value;

            paramValueLabel.text = paramValue.ToString("F2");

            // ---- 构建中心位置（与 BlendTreeMotionState.Create 一致）----
            float totalWeight = 0;
            for (int i = 0; i < count; i++)
                totalWeight += clips[i].Weight;
            if (totalWeight <= 0) totalWeight = 1;

            float[] centers = new float[count];
            float halfFirst = clips[0].Weight * 0.5f;
            float cumWeight = 0;
            for (int i = 0; i < count; i++)
            {
                centers[i] = (cumWeight + clips[i].Weight * 0.5f - halfFirst) / totalWeight;
                cumWeight += clips[i].Weight;
            }

            // ---- 计算当前参数下的激活权重 ----
            float[] activeWeights = ComputeActiveWeights(centers, count, paramValue);

            // ---- 构建扇形数据（Clip 0 居中于圆顶部）----
            var sectors = new PieSector[count];
            for (int i = 0; i < count; i++)
            {
                float halfWidth = clips[i].Weight / (2f * totalWeight);
                sectors[i] = new PieSector
                {
                    Label = clips[i].Asset ? clips[i].Asset.name : $"Clip {i}",
                    AngleStart = (centers[i] - halfWidth) * 360f,
                    AngleEnd = (centers[i] + halfWidth) * 360f,
                    ActiveWeight = activeWeights[i],
                    Color = GetClipColor(i)
                };
            }
            pieChart.SetData(sectors, paramValue);

            // ---- 更新权重标签 ----
            weightLabelsContainer.Clear();
            for (int i = 0; i < count; i++)
            {
                var item = new VisualElement();
                item.style.flexDirection = FlexDirection.Row;
                item.style.alignItems = Align.Center;
                item.style.marginBottom = 2;

                var colorDot = new VisualElement();
                colorDot.style.width = 10;
                colorDot.style.height = 10;
                colorDot.style.borderTopLeftRadius = 5;
                colorDot.style.borderTopRightRadius = 5;
                colorDot.style.borderBottomLeftRadius = 5;
                colorDot.style.borderBottomRightRadius = 5;
                colorDot.style.backgroundColor = GetClipColor(i);
                colorDot.style.marginRight = 4;
                item.Add(colorDot);

                string clipName = clips[i].Asset ? clips[i].Asset.name : $"Clip {i}";
                var label = new Label($"{clipName}: {activeWeights[i]:P0}");
                label.style.fontSize = 11;
                if (activeWeights[i] > 0.01f)
                    label.style.unityFontStyleAndWeight = FontStyle.Bold;
                item.Add(label);

                weightLabelsContainer.Add(item);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  权重计算（与 BlendTreeMotionState.Evaluate 一致，圆形中心模型）
        // ─────────────────────────────────────────────────────────────────────

        internal static float[] ComputeActiveWeights(float[] centers, int clipCount, float paramValue)
        {
            float[] weights = new float[clipCount];
            if (clipCount == 0) return weights;
            if (clipCount == 1) { weights[0] = 1f; return weights; }

            paramValue = Mathf.Clamp01(paramValue);

            // 在圆上找 paramValue 落在哪两个相邻 center 之间
            int prevIdx = clipCount - 1;
            int nextIdx = 0;
            float prevCenter = centers[clipCount - 1];
            float nextCenter = centers[0] + 1f; // 环绕

            for (int i = 0; i < clipCount; i++)
            {
                if (paramValue < centers[i])
                {
                    nextIdx = i;
                    nextCenter = centers[i];
                    prevIdx = (i - 1 + clipCount) % clipCount;
                    prevCenter = i > 0 ? centers[i - 1] : centers[clipCount - 1] - 1f;
                    break;
                }
            }

            float range = nextCenter - prevCenter;
            float blend = range > 0 ? (paramValue - prevCenter) / range : 0;
            blend = Mathf.Clamp01(blend);

            if (prevIdx == nextIdx)
                weights[prevIdx] = 1f;
            else
            {
                weights[prevIdx] = 1f - blend;
                weights[nextIdx] = blend;
            }
            return weights;
        }

        internal static Color GetClipColor(int index)
        {
            float hue = (index * 0.618033988749f) % 1f;
            return Color.HSVToRGB(hue, 0.6f, 0.85f);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PieSector + WeightPieElement：圆形扇形图
    // ─────────────────────────────────────────────────────────────────────────

    public struct PieSector
    {
        public string Label;
        public float AngleStart; // 度
        public float AngleEnd;   // 度
        public float ActiveWeight;
        public Color Color;
    }

    /// <summary>
    /// 用 Handles 绘制圆形扇形图，每个 Clip 一个扇形，
    /// 激活的扇形高亮、未激活的变暗，参数位置用白色指针线标出
    /// </summary>
    public class WeightPieElement : ImmediateModeElement
    {
        private PieSector[] sectors;
        private float paramValue;

        private const int ArcSegments = 64; // 每个扇形的弧线段数
        private static readonly Color BorderColor = new Color(0.12f, 0.12f, 0.12f, 1f);
        private static readonly Color CursorColor = new Color(1f, 1f, 1f, 0.95f);
        private static readonly Color DimOverlay = new Color(0, 0, 0, 0.5f);

        public WeightPieElement()
        {
            pickingMode = PickingMode.Ignore;
        }

        public void SetData(PieSector[] sectors, float paramValue)
        {
            this.sectors = sectors;
            this.paramValue = paramValue;
            MarkDirtyRepaint();
        }

        protected override void ImmediateRepaint()
        {
            if (sectors == null || sectors.Length == 0) return;

            float size = Mathf.Min(contentRect.width, contentRect.height);
            if (size <= 0) return;

            float radius = size * 0.45f;
            Vector2 center = new Vector2(contentRect.width * 0.5f, contentRect.height * 0.5f);

            // ---- 绘制各扇形 ----
            for (int i = 0; i < sectors.Length; i++)
            {
                var sector = sectors[i];
                float startDeg = sector.AngleStart - 90f; // 0度在顶部
                float endDeg = sector.AngleEnd - 90f;
                float sweepDeg = endDeg - startDeg;
                if (sweepDeg <= 0.01f) continue;

                // 激活时用亮色，未激活时变暗
                Color fillColor = sector.Color;
                if (sector.ActiveWeight < 0.01f)
                    fillColor = DimColor(fillColor, 0.3f);
                else if (sector.ActiveWeight < 0.99f)
                    fillColor = Color.Lerp(DimColor(fillColor, 0.3f), fillColor, sector.ActiveWeight);

                // 激活的扇形稍微外扩
                float r = radius;
                if (sector.ActiveWeight > 0.01f)
                    r = radius + 4f * sector.ActiveWeight;

                DrawFilledArc(center, r, startDeg, endDeg, fillColor);

                // 扇形边界线
                DrawRadiusLine(center, radius + 4f, startDeg, BorderColor);
            }

            // 最后一个扇形的结束线
            if (sectors.Length > 0)
            {
                float lastEnd = sectors[sectors.Length - 1].AngleEnd - 90f;
                DrawRadiusLine(center, radius + 4f, lastEnd, BorderColor);
            }

            // ---- 圆形边框 ----
            DrawCircleOutline(center, radius, BorderColor, 1.5f);

            // ---- 参数游标（白色指针线）----
            float cursorDeg = paramValue * 360f - 90f;
            float cursorRad = cursorDeg * Mathf.Deg2Rad;
            Vector3 cursorEnd = new Vector3(
                center.x + Mathf.Cos(cursorRad) * (radius + 10f),
                center.y + Mathf.Sin(cursorRad) * (radius + 10f),
                0);
            var prevColor = Handles.color;
            Handles.color = CursorColor;
            Handles.DrawAAPolyLine(3f, new Vector3(center.x, center.y, 0), cursorEnd);
            Handles.color = prevColor;

            // ---- 中心小圆点 ----
            EditorGUI.DrawRect(new Rect(center.x - 3, center.y - 3, 6, 6), CursorColor);
        }

        // 用三角扇绘制实心扇形
        private static void DrawFilledArc(Vector2 center, float radius, float startDeg, float endDeg, Color color)
        {
            float sweepDeg = endDeg - startDeg;
            int segments = Mathf.Max(3, Mathf.CeilToInt(ArcSegments * sweepDeg / 360f));
            float stepDeg = sweepDeg / segments;

            var prevColor = Handles.color;
            Handles.color = color;

            Vector3 c = new Vector3(center.x, center.y, 0);
            for (int s = 0; s < segments; s++)
            {
                float a0 = (startDeg + stepDeg * s) * Mathf.Deg2Rad;
                float a1 = (startDeg + stepDeg * (s + 1)) * Mathf.Deg2Rad;

                Vector3 p0 = new Vector3(center.x + Mathf.Cos(a0) * radius, center.y + Mathf.Sin(a0) * radius, 0);
                Vector3 p1 = new Vector3(center.x + Mathf.Cos(a1) * radius, center.y + Mathf.Sin(a1) * radius, 0);

                Handles.DrawAAConvexPolygon(c, p0, p1);
            }

            Handles.color = prevColor;
        }

        private static void DrawRadiusLine(Vector2 center, float radius, float angleDeg, Color color)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector3 from = new Vector3(center.x, center.y, 0);
            Vector3 to = new Vector3(center.x + Mathf.Cos(rad) * radius, center.y + Mathf.Sin(rad) * radius, 0);
            var prevColor = Handles.color;
            Handles.color = color;
            Handles.DrawAAPolyLine(1.5f, from, to);
            Handles.color = prevColor;
        }

        private static void DrawCircleOutline(Vector2 center, float radius, Color color, float width)
        {
            const int circleSegs = 64;
            var prevColor = Handles.color;
            Handles.color = color;
            for (int i = 0; i < circleSegs; i++)
            {
                float a0 = (i * 360f / circleSegs) * Mathf.Deg2Rad;
                float a1 = ((i + 1) * 360f / circleSegs) * Mathf.Deg2Rad;
                Vector3 p0 = new Vector3(center.x + Mathf.Cos(a0) * radius, center.y + Mathf.Sin(a0) * radius, 0);
                Vector3 p1 = new Vector3(center.x + Mathf.Cos(a1) * radius, center.y + Mathf.Sin(a1) * radius, 0);
                Handles.DrawAAPolyLine(width, p0, p1);
            }
            Handles.color = prevColor;
        }

        private static Color DimColor(Color c, float factor)
        {
            return new Color(c.r * factor, c.g * factor, c.b * factor, c.a);
        }
    }
}
