using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Timeline
{
    public enum OverlapSelection
    {
        None,
        PrevSelected,
        NextSelected
    }

    public struct OverlapZone
    {
        public float Left;
        public float Right;
        public OverlapSelection Selection;

        public OverlapZone(float left, float right, OverlapSelection selection = OverlapSelection.None)
        {
            Left = left;
            Right = right;
            Selection = selection;
        }
    }

    public class OverlapDrawElement : ImmediateModeElement
    {
        private static readonly Color NormalColor = new Color(1f, 0.5f, 0f, 0.9f);
        private static readonly Color NormalFill = new Color(1f, 0.5f, 0f, 0.25f);
        private static readonly Color SelectedColor = new Color(0.2f, 0.8f, 1f, 0.9f);
        private static readonly Color SelectedFill = new Color(0.2f, 0.8f, 1f, 0.25f);

        private readonly List<OverlapZone> zones = new List<OverlapZone>();

        public OverlapDrawElement()
        {
            pickingMode = PickingMode.Ignore;
            this.StretchToParentSize();
            style.display = DisplayStyle.None;
        }

        public void AddZone(OverlapZone zone)
        {
            zones.Add(zone);
            style.display = DisplayStyle.Flex;
            MarkDirtyRepaint();
        }

        public void ClearAll()
        {
            if (zones.Count == 0) return;
            zones.Clear();
            style.display = DisplayStyle.None;
            MarkDirtyRepaint();
        }

        protected override void ImmediateRepaint()
        {
            if (zones.Count == 0) return;
            float h = contentRect.height;
            foreach (var zone in zones)
            {
                bool selected = zone.Selection != OverlapSelection.None;
                var lineColor = selected ? SelectedColor : NormalColor;
                var fillColor = selected ? SelectedFill : NormalFill;

                // 对角线方向取决于哪个 Clip 被选中
                Vector3 lineStart, lineEnd;
                if (zone.Selection == OverlapSelection.NextSelected)
                {
                    // 后一个 Clip 被选中：对角线从左下到右上
                    lineStart = new Vector3(zone.Left, h);
                    lineEnd = new Vector3(zone.Right, 0);
                }
                else
                {
                    // 前一个 Clip 被选中或无选中：对角线从左上到右下
                    lineStart = new Vector3(zone.Left, 0);
                    lineEnd = new Vector3(zone.Right, h);
                }

                using (new Handles.DrawingScope(lineColor))
                {
                    Handles.DrawLine(lineStart, lineEnd);
                }

                // 半透明填充
                var rect = new Rect(zone.Left, 0, zone.Right - zone.Left, h);
                using (new Handles.DrawingScope(fillColor))
                {
                    Handles.DrawSolidRectangleWithOutline(rect, fillColor, Color.clear);
                }
            }
        }
    }
}
