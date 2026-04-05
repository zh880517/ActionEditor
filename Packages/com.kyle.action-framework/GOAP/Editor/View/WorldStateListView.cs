using System.Collections.Generic;
using GOAP;
using UnityEditor;
using UnityEngine.UIElements;

namespace GOAP.EditorView
{
    // WorldState 键值对列表编辑器
    // 每行：Key 文本框 + ValueType 下拉（bool/int/float/string）+ Value 控件 + 删除按钮
    // 数据变更时向上发送 DataChangedEvent
    public class WorldStateListView : VisualElement
    {
        private readonly List<WorldStateEntry> _entries;
        private readonly VisualElement _listContainer;

        public WorldStateListView(List<WorldStateEntry> entries)
        {
            _entries = entries;

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

            // Key 输入框
            var keyField = new TextField { value = entry.Key };
            keyField.style.width = 120;
            keyField.style.flexShrink = 1;
            keyField.RegisterValueChangedCallback(e =>
            {
                var current = _entries[index];
                _entries[index] = new WorldStateEntry { Key = e.newValue, ValueType = current.ValueType, ValueJson = current.ValueJson };
                SendEvent(DataChangedEvent.GetPooled());
            });

            // ValueType 下拉
            var typeChoices = new List<string> { "bool", "int", "float", "string" };
            var typeDropdown = new DropdownField(typeChoices, typeChoices.IndexOf(entry.ValueType) >= 0 ? typeChoices.IndexOf(entry.ValueType) : 0);
            typeDropdown.style.width = 70;

            // Value 控件（根据类型动态切换）
            var valueContainer = new VisualElement();
            valueContainer.style.flexGrow = 1;
            BuildValueControl(valueContainer, index);

            typeDropdown.RegisterValueChangedCallback(e =>
            {
                var current = _entries[index];
                _entries[index] = new WorldStateEntry { Key = current.Key, ValueType = e.newValue, ValueJson = GetDefaultJson(e.newValue) };
                SendEvent(DataChangedEvent.GetPooled());
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

            row.Add(keyField);
            row.Add(typeDropdown);
            row.Add(valueContainer);
            row.Add(deleteButton);
            return row;
        }

        // 根据当前 ValueType 构建值输入控件
        private void BuildValueControl(VisualElement container, int index)
        {
            container.Clear();
            var entry = _entries[index];
            switch (entry.ValueType)
            {
                case "bool":
                {
                    bool.TryParse(entry.ValueJson, out bool val);
                    var toggle = new Toggle { value = val };
                    toggle.RegisterValueChangedCallback(e =>
                    {
                        var cur = _entries[index];
                        _entries[index] = new WorldStateEntry { Key = cur.Key, ValueType = cur.ValueType, ValueJson = e.newValue.ToString().ToLower() };
                        SendEvent(DataChangedEvent.GetPooled());
                    });
                    container.Add(toggle);
                    break;
                }
                case "int":
                {
                    int.TryParse(entry.ValueJson, out int val);
                    var field = new IntegerField { value = val };
                    field.style.flexGrow = 1;
                    field.RegisterValueChangedCallback(e =>
                    {
                        var cur = _entries[index];
                        _entries[index] = new WorldStateEntry { Key = cur.Key, ValueType = cur.ValueType, ValueJson = e.newValue.ToString() };
                        SendEvent(DataChangedEvent.GetPooled());
                    });
                    container.Add(field);
                    break;
                }
                case "float":
                {
                    float.TryParse(entry.ValueJson, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float val);
                    var field = new FloatField { value = val };
                    field.style.flexGrow = 1;
                    field.RegisterValueChangedCallback(e =>
                    {
                        var cur = _entries[index];
                        _entries[index] = new WorldStateEntry { Key = cur.Key, ValueType = cur.ValueType, ValueJson = e.newValue.ToString(System.Globalization.CultureInfo.InvariantCulture) };
                        SendEvent(DataChangedEvent.GetPooled());
                    });
                    container.Add(field);
                    break;
                }
                case "string":
                default:
                {
                    var field = new TextField { value = entry.ValueJson ?? "" };
                    field.style.flexGrow = 1;
                    field.RegisterValueChangedCallback(e =>
                    {
                        var cur = _entries[index];
                        _entries[index] = new WorldStateEntry { Key = cur.Key, ValueType = cur.ValueType, ValueJson = e.newValue };
                        SendEvent(DataChangedEvent.GetPooled());
                    });
                    container.Add(field);
                    break;
                }
            }
        }

        private void AddEntry()
        {
            _entries.Add(new WorldStateEntry { Key = "newKey", ValueType = "bool", ValueJson = "false" });
            SendEvent(DataChangedEvent.GetPooled());
            Refresh();
        }

        private string GetDefaultJson(string valueType)
        {
            switch (valueType)
            {
                case "bool":   return "false";
                case "int":    return "0";
                case "float":  return "0";
                case "string": return "";
                default:       return "";
            }
        }
    }
}
