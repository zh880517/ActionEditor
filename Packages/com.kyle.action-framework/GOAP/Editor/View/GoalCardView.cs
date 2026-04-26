using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace GOAP.EditorView
{
    // 单个 Goal 的卡片 UI
    // 显示：Id、DisplayName、BasePriority、期望终态列表
    // 数据变更时向上冒泡 DataChangedEvent，点击删除时向上冒泡 DeleteRequestEvent
    public class GoalCardView : VisualElement
    {
        private readonly GoalData _data;
        private readonly Type _boolKeyType;
        private readonly Type _intKeyType;

        public GoalCardView(GoalData data, Type boolKeyType, Type intKeyType)
        {
            _data = data;
            _boolKeyType = boolKeyType;
            _intKeyType = intKeyType;
            Build();
        }

        private void Build()
        {
            // 卡片样式
            style.borderTopWidth = style.borderBottomWidth = style.borderLeftWidth = style.borderRightWidth = 1;
            style.borderTopColor = style.borderBottomColor = style.borderLeftColor = style.borderRightColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));
            style.borderTopLeftRadius = style.borderTopRightRadius = style.borderBottomLeftRadius = style.borderBottomRightRadius = 4;
            style.paddingTop = style.paddingBottom = style.paddingLeft = style.paddingRight = 8;
            style.marginBottom = 6;
            style.backgroundColor = new StyleColor(new Color(0.18f, 0.22f, 0.22f));

            // 标题行
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;

            var titleLabel = new Label("🎯 Goal");
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.color = new StyleColor(new Color(0.4f, 0.9f, 0.6f));

            var deleteBtn = new Button(() => SendEvent(DeleteRequestEvent.GetPooled())) { text = "×" };
            deleteBtn.style.color = new StyleColor(new Color(1f, 0.4f, 0.4f));
            deleteBtn.style.width = 24;

            header.Add(titleLabel);
            header.Add(deleteBtn);
            Add(header);

            // 名称字段（编辑 ScriptableObject.name）
            AddLabeledField("名称", new TextField { value = _data.name }, f =>
            {
                _data.name = ((TextField)f).value;
                SendEvent(DataChangedEvent.GetPooled());
            });

            // Id 标签（只读，来自 Config.Id）
            var idRow = new VisualElement();
            idRow.style.flexDirection = FlexDirection.Row;
            idRow.style.marginTop = 4;
            idRow.style.alignItems = Align.Center;
            var idLabel = new Label("Id");
            idLabel.style.width = 60;
            idLabel.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
            var idValue = new Label(_data.Id);
            idValue.style.color = new StyleColor(new Color(0.5f, 0.8f, 0.5f));
            idRow.Add(idLabel);
            idRow.Add(idValue);
            Add(idRow);

            // BasePriority 字段
            AddLabeledField("优先级", new FloatField { value = _data.BasePriority }, f =>
            {
                _data.BasePriority = ((FloatField)f).value;
                SendEvent(DataChangedEvent.GetPooled());
            });

            // 期望终态折叠组
            var foldout = new Foldout { text = "▼ 期望终态", value = _data.FoldoutDesiredState };
            foldout.Add(new WorldStateListView(_data.DesiredState, _boolKeyType, _intKeyType));
            foldout.RegisterValueChangedCallback(e =>
            {
                _data.FoldoutDesiredState = e.newValue;
                SendEvent(DataChangedEvent.GetPooled());
            });
            Add(foldout);
        }

        private void AddLabeledField(string label, VisualElement field, System.Action<VisualElement> onValueChange)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginTop = 4;
            row.style.alignItems = Align.Center;

            var lbl = new Label(label);
            lbl.style.width = 60;
            lbl.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));

            field.style.flexGrow = 1;
            if (field is TextField tf) tf.RegisterValueChangedCallback(_ => onValueChange(field));
            else if (field is FloatField ff) ff.RegisterValueChangedCallback(_ => onValueChange(field));

            row.Add(lbl);
            row.Add(field);
            Add(row);
        }
    }
}
