using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace GOAP.EditorView
{
    // WorldState 键值对列表编辑器
    // 每行：ValueType 下拉（Bool/Int）+ Key 枚举下拉 + Value 控件 + 删除按钮
    // boolKeyType / intKeyType 由 ConfigAsset 子类提供，用于枚举 Key 选项
    // 数据变更时向上发送 DataChangedEvent
    public class WorldStateListView : VisualElement
    {
        private readonly List<WorldStateEntry> _entries;
        private readonly Type _boolKeyType;
        private readonly Type _intKeyType;
        private readonly VisualElement _listContainer;

        public WorldStateListView(List<WorldStateEntry> entries, Type boolKeyType, Type intKeyType)
        {
            _entries = entries;
            _boolKeyType = boolKeyType;
            _intKeyType = intKeyType;

            style.paddingLeft = 4;

            _listContainer = new VisualElement();
            Add(_listContainer);

            var addButton = new Button(AddEntry) { text = "+ 新增条件" };
            addButton.style.marginTop = 4;
            Add(addButton);

            Refresh();
        }

        // 重新绘制列表（数据变更后调用）
        public void Refresh()
        {
            _listContainer.Clear();
            for (int i = 0; i < _entries.Count; i++)
                _listContainer.Add(CreateEntryRow(i));
        }

        private VisualElement CreateEntryRow(int index)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 2;

            var entry = _entries[index];

            // ValueType 下拉（Bool/Int）
            var typeChoices = new List<string> { "Bool", "Int" };
            int currentTypeIndex = entry.ValueType == WorldStateValueType.Bool ? 0 : 1;
            var typeDropdown = new DropdownField(typeChoices, currentTypeIndex);
            typeDropdown.style.width = 50;

            // Key 枚举下拉容器（根据 ValueType 填充对应枚举）
            var keyContainer = new VisualElement();
            keyContainer.style.width = 130;

            // Value 控件容器
            var valueContainer = new VisualElement();
            valueContainer.style.flexGrow = 1;

            BuildKeyControl(keyContainer, index);
            BuildValueControl(valueContainer, index);

            typeDropdown.RegisterValueChangedCallback(e =>
            {
                var newType = e.newValue == "Bool" ? WorldStateValueType.Bool : WorldStateValueType.Int;
                _entries[index] = new WorldStateEntry
                {
                    ValueType = newType,
                    KeyIndex = 0,
                    Value = 0
                };
                SendEvent(DataChangedEvent.GetPooled());
                BuildKeyControl(keyContainer, index);
                BuildValueControl(valueContainer, index);
            });

            // 删除按钮
            var deleteButton = new Button(() =>
            {
                _entries.RemoveAt(index);
                SendEvent(DataChangedEvent.GetPooled());
                Refresh();
            }) { text = "×" };
            deleteButton.style.width = 24;
            deleteButton.style.color = new UnityEngine.Color(1f, 0.4f, 0.4f);

            row.Add(typeDropdown);
            row.Add(keyContainer);
            row.Add(valueContainer);
            row.Add(deleteButton);
            return row;
        }

        // 根据 ValueType 构建 Key 枚举下拉（枚举为 null 时降级为整数输入框）
        private void BuildKeyControl(VisualElement container, int index)
        {
            container.Clear();
            var entry = _entries[index];
            var keyType = entry.ValueType == WorldStateValueType.Bool ? _boolKeyType : _intKeyType;

            if (keyType == null || !keyType.IsEnum)
            {
                var field = new IntegerField { value = entry.KeyIndex };
                field.style.flexGrow = 1;
                field.RegisterValueChangedCallback(e =>
                {
                    var cur = _entries[index];
                    _entries[index] = new WorldStateEntry { ValueType = cur.ValueType, KeyIndex = e.newValue, Value = cur.Value };
                    SendEvent(DataChangedEvent.GetPooled());
                });
                container.Add(field);
                return;
            }

            var names = Enum.GetNames(keyType);
            var enumValues = Enum.GetValues(keyType);
            var choices = new List<string>(names);

            int currentIndex = 0;
            for (int i = 0; i < enumValues.Length; i++)
            {
                if ((int)enumValues.GetValue(i) == entry.KeyIndex)
                {
                    currentIndex = i;
                    break;
                }
            }

            var dropdown = new DropdownField(choices, currentIndex);
            dropdown.style.flexGrow = 1;
            dropdown.RegisterValueChangedCallback(e =>
            {
                int selectedIndex = choices.IndexOf(e.newValue);
                int enumIntValue = selectedIndex >= 0 ? (int)enumValues.GetValue(selectedIndex) : 0;
                var cur = _entries[index];
                _entries[index] = new WorldStateEntry { ValueType = cur.ValueType, KeyIndex = enumIntValue, Value = cur.Value };
                SendEvent(DataChangedEvent.GetPooled());
            });
            container.Add(dropdown);
        }

        // 根据 ValueType 构建值输入控件（Bool → Toggle，Int → IntegerField）
        private void BuildValueControl(VisualElement container, int index)
        {
            container.Clear();
            var entry = _entries[index];

            if (entry.ValueType == WorldStateValueType.Bool)
            {
                var toggle = new Toggle { value = entry.Value != 0 };
                toggle.RegisterValueChangedCallback(e =>
                {
                    var cur = _entries[index];
                    _entries[index] = new WorldStateEntry { ValueType = cur.ValueType, KeyIndex = cur.KeyIndex, Value = e.newValue ? 1 : 0 };
                    SendEvent(DataChangedEvent.GetPooled());
                });
                container.Add(toggle);
            }
            else
            {
                var field = new IntegerField { value = entry.Value };
                field.style.flexGrow = 1;
                field.RegisterValueChangedCallback(e =>
                {
                    var cur = _entries[index];
                    _entries[index] = new WorldStateEntry { ValueType = cur.ValueType, KeyIndex = cur.KeyIndex, Value = e.newValue };
                    SendEvent(DataChangedEvent.GetPooled());
                });
                container.Add(field);
            }
        }

        private void AddEntry()
        {
            _entries.Add(new WorldStateEntry { ValueType = WorldStateValueType.Bool, KeyIndex = 0, Value = 0 });
            SendEvent(DataChangedEvent.GetPooled());
            Refresh();
        }
    }
}
