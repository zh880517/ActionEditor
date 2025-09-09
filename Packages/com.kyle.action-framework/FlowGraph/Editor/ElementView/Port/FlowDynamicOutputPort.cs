using PropertyEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine.UIElements;

namespace Flow.EditorView
{
    public class DynamicOutputPortElement : VisualElement
    {
        public FlowPort Port { get; private set; }
        public PropertyElement Field { get; private set; }
        public int Index;
        public DynamicOutputPortElement(PropertyElement fieldElement)
        {
            style.flexDirection = FlexDirection.Row;
            if(fieldElement != null)
            {
                Add(fieldElement);
            }
            Field = fieldElement;
            Port = new FlowPort(false);
            Port.portName = "";
            Field.style.minWidth = 100;
            Add(Port);
        }
    }
    public class FlowDynamicOutputPort : VisualElement
    {
        public FlowNode Node { get; private set; }
        private readonly Label titleLabel = new Label();
        private readonly ListView listView = new ListView();
        private readonly FlowNodeTypeInfo nodeTypeInfo;
        private readonly IList rawValue;
        private IList sourceList;

        private readonly List<DynamicOutputPortElement> children = new List<DynamicOutputPortElement>();

        public FlowDynamicOutputPort(FlowNode node, FlowNodeTypeInfo nodeTypeInfo)
        {
            Node = node;
            this.nodeTypeInfo = nodeTypeInfo;
            rawValue = (IList)System.Activator.CreateInstance(typeof(List<>).MakeGenericType(nodeTypeInfo.DynamicPortType));
            var value = nodeTypeInfo.ValueField.GetValue(node);
            sourceList = GetSourceList();
            if (sourceList != null)
            {
                foreach (var item in sourceList)
                {
                    rawValue.Add(item);
                }
            }
            else
            {
                sourceList = (IList)System.Activator.CreateInstance(typeof(List<>).MakeGenericType(nodeTypeInfo.DynamicPortType));
                nodeTypeInfo.DynamicPortField.SetValue(value, sourceList);
                nodeTypeInfo.ValueField.SetValue(node, value);
            }

            style.flexDirection = FlexDirection.Column;
            titleLabel.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
            var displayNameAttr = nodeTypeInfo.DynamicPortField.GetCustomAttribute<DisplayAttribute>();
            if (displayNameAttr != null)
            {
                titleLabel.text = displayNameAttr.Name;
                titleLabel.tooltip = displayNameAttr.Tooltip;
            }
            else
            {
                titleLabel.text = ObjectNames.NicifyVariableName(nodeTypeInfo.DynamicPortField.Name);
            }
            Add(titleLabel);

            listView.showFoldoutHeader = false;
            listView.reorderable = true;
            listView.reorderMode = ListViewReorderMode.Animated;
            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listView.showAddRemoveFooter = true;
            listView.showBorder = false;
            listView.horizontalScrollingEnabled = false;
            listView.itemsSource = rawValue;
            listView.makeItem = MakeItem;
            listView.bindItem = BindItem;
            listView.destroyItem = DestroyItem;
            listView.itemsAdded += OnAdded;
            listView.itemsRemoved += OnRemoved;
            listView.itemIndexChanged += IndexChanged;
            Add(listView);


            RegisterCallback<PropertyValueChangedEvent>(OnPropertyValueChangedEvent);
            RegisterCallback<MouseDownEvent>(evt =>
            {
                evt.StopImmediatePropagation();
            });
        }

        private IList GetSourceList()
        {
            var value = nodeTypeInfo.ValueField.GetValue(Node);
            return (IList)nodeTypeInfo.DynamicPortField.GetValue(value);
        }

        public void Refresh()
        {
            var newList = GetSourceList();
            if(newList != sourceList)
            {
                sourceList = newList;
            }
            rawValue.Clear();
            foreach (var item in sourceList)
            {
                rawValue.Add(item);
            }
            listView.RefreshItems();
        }

        public void DisconnectAll()
        {
            foreach (var item in children)
            {
                item.Port.DisconnectAll();
            }
        }

        public FlowNodePort GetPort(int index)
        {
            if (index < 0 || index >= children.Count)
                return null;
            return children[index].Port;
        }

        private VisualElement MakeItem()
        {
            var element = PropertyElementFactory.CreateByType(nodeTypeInfo.DynamicPortType, true);
            element.SetLableWidth(60);
            var e = new DynamicOutputPortElement(element);
            e.Port.Owner = Node;
            return e;
        }


        private void BindItem(VisualElement element, int index)
        {
            var portElement = element as DynamicOutputPortElement;
            portElement.Field.SetValue(rawValue[index]);
            portElement.Field.Index = index;
            portElement.Port.Index = index;
            portElement.Index = index;
            if (!children.Contains(portElement))
            {
                children.Add(portElement);
                using (var e = DynamicOuputPortCreateEvent.GetPooled(portElement.Port, Node, index))
                {
                    SendEvent(e);
                }
            }
            children.Sort((a, b) => a.Index.CompareTo(b.Index));
        }

        private void DestroyItem(VisualElement element)
        {
            var portElement = element as DynamicOutputPortElement;
            children.Remove(portElement);
        }

        private void OnAdded(IEnumerable<int> indexs)
        {
            SyncFromListView();
        }

        private void OnRemoved(IEnumerable<int> indexs)
        {
            //按照从后往前的顺序删除
            FlowPortOperateUtil.RemoveDynamicOutputPortWithUndo(Node, indexs);
            CopyRawValueToList();
        }
        private void IndexChanged(int srcIndex, int dstIndex)
        {
            //先从列表中移除srcIndex的数据，再插入到dstIndex位置
            FlowPortOperateUtil.DynamicOutputPortIndexChangedWithUndo(Node, srcIndex, dstIndex);
            CopyRawValueToList();
        }
        private void SyncFromListView()
        {
            using var e = RegisterUndoEvent.GetPooled(this, "Modify Property");
            SendEvent(e);
            CopyRawValueToList();
        }
        private void CopyRawValueToList()
        {
            sourceList.Clear();
            foreach (var item in rawValue)
            {
                sourceList.Add(item);
            }
            children.Sort((a, b) => a.Index.CompareTo(b.Index));
        }
        private void OnPropertyValueChangedEvent(PropertyValueChangedEvent evt)
        {
            if (evt.target == evt.currentTarget)
                return;
            evt.StopPropagation();

            using var e = RegisterUndoEvent.GetPooled(this, "Modify Property");
            SendEvent(e);

            rawValue[evt.Index] = evt.Value;
            sourceList[evt.Index] = evt.Value;
        }
    }
}
