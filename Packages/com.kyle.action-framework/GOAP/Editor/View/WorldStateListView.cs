using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace GOAP.EditorView
{
    public enum WorldStateListMode
    {
        Condition,
        Effect
    }

    // WorldState 键值对列表编辑器
    // Condition 模式：显示比较运算符下拉（用于 Preconditions / DesiredState）
    // Effect 模式：显示效果模式下拉（用于 Effects）
    // 数据变更时向上发送 DataChangedEvent
    public class WorldStateListView : VisualElement
    {
        private static readonly string[] OpLabels = { "==", "!=", ">", "<", ">=", "<=" };
        private static readonly string[] BoolOpLabels = { "==", "!=" };
        private static readonly string[] ModeLabels = { "Assign", "Add" };

        private readonly List<WorldStateEntry> _entries;
        private readonly Type _boolKeyType;
        private readonly Type _intKeyType;
        private readonly WorldStateListMode _mode;
        private readonly VisualElement _listContainer;

        public WorldStateListView(List<WorldStateEntry> entries, Type boolKeyType, Type intKeyType,
            WorldStateListMode mode = WorldStateListMode.Condition)
        {
            _entries = entries;
            _boolKeyType = boolKeyType;
            _intKeyType = intKeyType;
            _mode = mode;

            style.paddingLeft = 4;

            _listContainer = new VisualElement();
            Add(_listContainer);

            var addButton = new Button(AddEntry) { text = "+ 新增条件" };
            addButton.style.marginTop = 4;
            Add(addButton);

            Refresh();
        }

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

            // Key 枚举下拉容器
            var keyContainer = new VisualElement();
            keyContainer.style.width = 130;

            // 运算符 / 模式下拉容器
            var opContainer = new VisualElement();
            opContainer.style.width = 55;

            // Value 控件容器
            var valueContainer = new VisualElement();
            valueContainer.style.flexGrow = 1;

            BuildKeyControl(keyContainer, index);
            BuildOpOrModeControl(opContainer, index);
            BuildValueControl(valueContainer, index);

            typeDropdown.RegisterValueChangedCallback(e =>
            {
                var newType = e.newValue == "Bool" ? WorldStateValueType.Bool : WorldStateValueType.Int;
                _entries[index] = new WorldStateEntry
                {
                    ValueType = newType,
                    KeyIndex = 0,
                    Value = 0,
                    Operator = CompareOp.Equal,
                    Mode = EffectMode.Assign
                };
                SendEvent(DataChangedEvent.GetPooled());
                BuildKeyControl(keyContainer, index);
                BuildOpOrModeControl(opContainer, index);
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
            row.Add(opContainer);
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
                    _entries[index] = new WorldStateEntry
                    {
                        ValueType = cur.ValueType, KeyIndex = e.newValue, Value = cur.Value,
                        Operator = cur.Operator, Mode = cur.Mode
                    };
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
                _entries[index] = new WorldStateEntry
                {
                    ValueType = cur.ValueType, KeyIndex = enumIntValue, Value = cur.Value,
                    Operator = cur.Operator, Mode = cur.Mode
                };
                SendEvent(DataChangedEvent.GetPooled());
            });
            container.Add(dropdown);
        }

        // Condition 模式：比较运算符下拉；Effect 模式：效果模式下拉
        private void BuildOpOrModeControl(VisualElement container, int index)
        {
            container.Clear();
            var entry = _entries[index];
            bool isBool = entry.ValueType == WorldStateValueType.Bool;

            if (_mode == WorldStateListMode.Condition)
            {
                var labels = isBool ? BoolOpLabels : OpLabels;
                var choices = new List<string>(labels);
                int currentIndex = isBool
                    ? ClampOpForBool(entry.Operator)
                    : (int)entry.Operator;

                var dropdown = new DropdownField(choices, currentIndex);
                dropdown.style.flexGrow = 1;
                dropdown.RegisterValueChangedCallback(e =>
                {
                    int sel = choices.IndexOf(e.newValue);
                    CompareOp newOp;
                    if (isBool)
                        newOp = sel == 1 ? CompareOp.NotEqual : CompareOp.Equal;
                    else
                        newOp = (CompareOp)sel;

                    var cur = _entries[index];
                    _entries[index] = new WorldStateEntry
                    {
                        ValueType = cur.ValueType, KeyIndex = cur.KeyIndex, Value = cur.Value,
                        Operator = newOp, Mode = cur.Mode
                    };
                    SendEvent(DataChangedEvent.GetPooled());
                });
                container.Add(dropdown);
            }
            else // Effect
            {
                if (isBool)
                {
                    // Bool 强制 Assign，显示只读标签
                    var label = new Label("Assign");
                    label.style.unityTextAlign = UnityEngine.TextAnchor.MiddleLeft;
                    container.Add(label);
                    return;
                }

                var choices = new List<string>(ModeLabels);
                int currentIndex = (int)entry.Mode;
                var dropdown = new DropdownField(choices, currentIndex);
                dropdown.style.flexGrow = 1;
                dropdown.RegisterValueChangedCallback(e =>
                {
                    int sel = choices.IndexOf(e.newValue);
                    var cur = _entries[index];
                    _entries[index] = new WorldStateEntry
                    {
                        ValueType = cur.ValueType, KeyIndex = cur.KeyIndex, Value = cur.Value,
                        Operator = cur.Operator, Mode = (EffectMode)sel
                    };
                    SendEvent(DataChangedEvent.GetPooled());
                });
                container.Add(dropdown);
            }
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
                    _entries[index] = new WorldStateEntry
                    {
                        ValueType = cur.ValueType, KeyIndex = cur.KeyIndex, Value = e.newValue ? 1 : 0,
                        Operator = cur.Operator, Mode = cur.Mode
                    };
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
                    _entries[index] = new WorldStateEntry
                    {
                        ValueType = cur.ValueType, KeyIndex = cur.KeyIndex, Value = e.newValue,
                        Operator = cur.Operator, Mode = cur.Mode
                    };
                    SendEvent(DataChangedEvent.GetPooled());
                });
                container.Add(field);
            }
        }

        private void AddEntry()
        {
            _entries.Add(new WorldStateEntry
            {
                ValueType = WorldStateValueType.Bool,
                KeyIndex = 0,
                Value = 0,
                Operator = CompareOp.Equal,
                Mode = EffectMode.Assign
            });
            SendEvent(DataChangedEvent.GetPooled());
            Refresh();
        }

        // Bool 键仅支持 == 和 !=，将其他运算符映射到 dropdown index
        private static int ClampOpForBool(CompareOp op)
            => op == CompareOp.NotEqual ? 1 : 0;
    }
}
