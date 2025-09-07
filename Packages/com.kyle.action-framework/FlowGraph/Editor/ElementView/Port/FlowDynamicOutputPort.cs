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
            Port = new FlowPort(true);
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
            this.nodeTypeInfo = nodeTypeInfo;
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

            if ((node is IFlowDynamicOutputable))
            {
                throw new System.Exception("node must implement IFlowDynamicOutputable");
            }
            Add(listView);
            Node = node;
            style.flexDirection = FlexDirection.Column;
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
            listView.itemsAdded += OnAdded;
            listView.itemsRemoved += OnRemoved;
            listView.itemIndexChanged += IndexChanged;

            rawValue = (IList)System.Activator.CreateInstance(typeof(List<>).MakeGenericType(nodeTypeInfo.DynamicPortType));
            sourceList = (IList)nodeTypeInfo.DynamicPortField.GetValue(node);
            if (sourceList != null)
            {
                foreach (var item in sourceList)
                {
                    rawValue.Add(item);
                }
            }

            RegisterCallback<PropertyValueChangedEvent>(OnPropertyValueChangedEvent);
        }

        public void Refresh()
        {
            var newList = (IList)nodeTypeInfo.DynamicPortField.GetValue(Node);
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
            return new DynamicOutputPortElement(element);
        }

        private void BindItem(VisualElement element, int index)
        {
            var portElement = element as DynamicOutputPortElement;
            portElement.Field.SetValue(rawValue[index]);
            portElement.Field.Index = index;
            portElement.Port.Index = index;
            portElement.Index = index;
            if (!children.Contains(portElement))
                children.Add(portElement);
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
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (i >= rawValue.Count)
                {
                    children.RemoveAt(i);
                    i--;
                    continue;
                }
                if (child.Index != i)
                {
                    child.Index = i;
                    child.Port.Index = i;
                    child.Field.Index = i;
                    child.Field.SetValue(rawValue[i]);
                }
            }
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
