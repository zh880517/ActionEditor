using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace GOAP.EditorView
{
    // 运行时调试面板，显示当前 Agent 的状态
    // 通过 DebugBridge 接收数据，仅在 Play 模式下有意义
    public class DebugView : VisualElement
    {
        private readonly Label _statusLabel;
        private readonly Label _goalLabel;
        private readonly Label _planLabel;
        private readonly Label _worldStateLabel;
        private readonly Label _timingLabel;

        public DebugView()
        {
            style.paddingTop = style.paddingBottom = style.paddingLeft = style.paddingRight = 6;
            style.backgroundColor = new StyleColor(new Color(0.12f, 0.12f, 0.12f));

            var title = new Label("调试面板");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 4;
            Add(title);

            _statusLabel = AddRow("状态");
            _goalLabel = AddRow("当前目标");
            _planLabel = AddRow("当前计划");
            _worldStateLabel = AddRow("WorldState");
            _timingLabel = AddRow("规划耗时");

            // 订阅桥接器
            DebugBridge.OnAgentUpdated += OnAgentUpdated;
        }

        ~DebugView()
        {
            DebugBridge.OnAgentUpdated -= OnAgentUpdated;
        }

        private Label AddRow(string labelText)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 2;

            var lbl = new Label(labelText + ":");
            lbl.style.width = 80;
            lbl.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f));

            var value = new Label("-");
            value.style.flexGrow = 1;
            value.style.color = new StyleColor(new Color(0.9f, 0.9f, 0.9f));

            row.Add(lbl);
            row.Add(value);
            Add(row);
            return value;
        }

        private void OnAgentUpdated(AgentSnapshot snapshot)
        {
            _statusLabel.text = snapshot.Status.ToString();
            _goalLabel.text = snapshot.CurrentGoalId != null
                ? $"{snapshot.CurrentGoalId} (优先级: {snapshot.CurrentGoalPriority:F1})"
                : "无";

            if (snapshot.PlanActionIds != null && snapshot.PlanActionIds.Length > 0)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < snapshot.PlanActionIds.Length; i++)
                {
                    if (i == snapshot.CurrentPlanIndex)
                        sb.Append($"[{snapshot.PlanActionIds[i]}]");
                    else
                        sb.Append(snapshot.PlanActionIds[i]);
                    if (i < snapshot.PlanActionIds.Length - 1)
                        sb.Append(" → ");
                }
                _planLabel.text = sb.ToString();
            }
            else
            {
                _planLabel.text = "无计划";
            }

            _worldStateLabel.text = snapshot.WorldStateSnapshot ?? "-";
            _timingLabel.text = $"{snapshot.LastPlanTimeMs:F2} ms";
        }
    }
}
