using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace GOAP.EditorView
{
    // 单个 Action 的卡片 UI
    // 显示：Id、DisplayName、Cost、前置条件列表、效果列表
    // 数据变更时向上冒泡 DataChangedEvent，点击删除时向上冒泡 DeleteRequestEvent
    public class ActionCardView : VisualElement
    {
        private readonly ActionData _data;
        private readonly Type _boolKeyType;
        private readonly Type _intKeyType;

        public ActionCardView(ActionData data, Type boolKeyType, Type intKeyType)
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
            style.backgroundColor = new StyleColor(new Color(0.22f, 0.22f, 0.22f));

            // 标题行
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;

            var titleLabel = new Label("⚔ Action");
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.color = new StyleColor(new Color(0.9f, 0.7f, 0.3f));

            var deleteBtn = new Button(() => SendEvent(DeleteRequestEvent.GetPooled())) { text = "×" };
            deleteBtn.style.color = new StyleColor(new Color(1f, 0.4f, 0.4f));
            deleteBtn.style.width = 24;

            header.Add(titleLabel);
            header.Add(deleteBtn);
            Add(header);

            // DisplayName 字段
            AddLabeledField("显示名", new TextField { value = _data.name }, f =>
            {
                _data.name = ((TextField)f).value;
                SendEvent(DataChangedEvent.GetPooled());
            });

            // Cost 字段
            AddLabeledField("代价", new FloatField { value = _data.Cost }, f =>
            {
                _data.Cost = ((FloatField)f).value;
                SendEvent(DataChangedEvent.GetPooled());
            });

            // 前置条件折叠组
            var precondFoldout = new Foldout { text = "▼ 前置条件", value = _data.FoldoutPreconditions };
            precondFoldout.Add(new WorldStateListView(_data.Preconditions, _boolKeyType, _intKeyType, WorldStateListMode.Condition));
            precondFoldout.RegisterValueChangedCallback(e =>
            {
                _data.FoldoutPreconditions = e.newValue;
                SendEvent(DataChangedEvent.GetPooled());
            });
            Add(precondFoldout);

            // 效果折叠组
            var effectFoldout = new Foldout { text = "▼ 效果", value = _data.FoldoutEffects };
            effectFoldout.Add(new WorldStateListView(_data.Effects, _boolKeyType, _intKeyType, WorldStateListMode.Effect));
            effectFoldout.RegisterValueChangedCallback(e =>
            {
                _data.FoldoutEffects = e.newValue;
                SendEvent(DataChangedEvent.GetPooled());
            });
            Add(effectFoldout);
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
