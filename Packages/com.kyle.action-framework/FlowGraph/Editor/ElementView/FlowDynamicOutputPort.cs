using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Flow.EditorView
{
    public class DynamicOutputPortElement : VisualElement
    {
        private readonly FlowPortView port = new FlowPortView(false);
        public VisualElement Field { get; private set; }
        public DynamicOutputPortElement(VisualElement fieldElement)
        {
            style.flexDirection = FlexDirection.Row;
            if(fieldElement != null)
            {
                Add(fieldElement);
            }
            Field = fieldElement;
            Add(port);
        }
    }
    public class FlowDynamicOutputPort : VisualElement
    {
        public FlowNode Node { get; private set; }
        private readonly ListView listView = new ListView();

        public FlowDynamicOutputPort(FlowNode node)
        {
            if((node is IFlowDynamicOutputable))
            {
                throw new System.Exception("node must implement IFlowDynamicOutputable");
            }
            Add(listView);

            Node = node;
            style.flexDirection = FlexDirection.Column;
            listView.showFoldoutHeader = false;
            listView.reorderable = true;
            listView.reorderMode = ListViewReorderMode.Animated;
            listView.showAddRemoveFooter = true;
            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listView.horizontalScrollingEnabled = false;
            listView.showBorder = false;
        }

        private void BindItem(VisualElement element, int index)
        {

        }

        private void OnAdded(IEnumerable<int> indexs)
        {

        }

        private void OnRemoved(IEnumerable<int> indexs)
        {

        }
        private void IndexChanged(int srcIndex, int dstIndex)
        {

        }
    }
}
