using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace PropertyEditor
{
    public class ArrayPropertyElement : PropertyElement
    {
        private readonly System.Type elementType;
        private readonly ListView listView = new ListView();
        private readonly List<PropertyElement> children = new List<PropertyElement>();
        private readonly IList rawValue;
        private System.Array sourceArray;
        private float labelWidth = LabelMinWidth;
        public override bool ReadOnly { get => listView.enabledSelf; set => listView.SetEnabled(!value); }
        public ArrayPropertyElement(System.Type elementType)
        {
            this.elementType = elementType;
            rawValue = (IList)System.Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
            listView.makeItem = MakeItem;
            listView.bindItem = BindItem;
            listView.itemsAdded += OnAdded;
            listView.itemsRemoved += OnRemoved;
            listView.itemIndexChanged += IndexChanged;
            listView.selectionType = SelectionType.Multiple;
            listView.showFoldoutHeader = true;
            listView.reorderable = true;
            listView.reorderMode = ListViewReorderMode.Animated;
            listView.showAddRemoveFooter = true;
            listView.horizontalScrollingEnabled = false;
            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            Add(listView);
            RegisterCallback<PropertyValueChangedEvent>(OnPropertyValueChangedEvent);
        }

        public void SetResizeable(bool value)
        {
            listView.showAddRemoveFooter = value;
            listView.Query("unity-list-view__size-field").ForEach(e => e.SetEnabled(value));
        }

        public override void SetLable(string name, string tip)
        {
            if (string.IsNullOrEmpty(name))
            {
                listView.showFoldoutHeader = false;
            }
            else
            {
                listView.showFoldoutHeader = true;
                listView.headerTitle = name;
                listView.tooltip = tip;
            }
        }

        public override void SetLableWidth(float width)
        {
            if (labelWidth == width)
                return;
            labelWidth = width;
            foreach (var item in children)
            {
                item.SetLableWidth(labelWidth);
            }
        }

        public override void SetValue(object value)
        {
            sourceArray = (System.Array)value;
            rawValue.Clear();
            if(sourceArray != null)
            {
                children.Clear();
                foreach (var item in sourceArray)
                {
                    rawValue.Add(item);
                }
            }
            listView.itemsSource = rawValue;
            listView.Rebuild();
        }

        private VisualElement MakeItem()
        {
            var element = PropertyElementFactory.CreateByType(elementType, true);
            element.SetLableWidth(labelWidth);
            return element;
        }
        private void BindItem(VisualElement element, int index)
        {
            var propertyElement = element as PropertyElement;
            if (propertyElement != null && index >= 0 && index < rawValue.Count)
            {
                propertyElement.Index = index;
                propertyElement.SetValue(rawValue[index]);
                if (!children.Contains(propertyElement))
                    children.Add(propertyElement);
            }
        }
        private void OnAdded(IEnumerable<int> indices)
        {
            SyncFromListView();
        }
        private void OnRemoved(IEnumerable<int> indices)
        {
            SyncFromListView();
        }
        private void IndexChanged(int oldIndex, int newIndex)
        {
            SyncFromListView();
        }
        private void SyncFromListView()
        {
            using var e = RegisterUndoEvent.GetPooled(this, "Modify Property");
            SendEvent(e);
            CopyRawValueToList();
        }
        private void CopyRawValueToList()
        {
            if (sourceArray == null || sourceArray.Length != rawValue.Count)
            {
                sourceArray = System.Array.CreateInstance(elementType, rawValue.Count);
                if (Field != null)
                {
                    using var evt = PropertyValueChangedEvent.GetPooled(this, sourceArray, Field, Index);
                    SendEvent(evt);
                }
            }
            for (int i = 0; i < rawValue.Count; i++)
            {
                sourceArray.SetValue(rawValue[i], i);
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
                    child.SetValue(rawValue[i]);
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
            sourceArray.SetValue(evt.Value, evt.Index);
        }
    }
}
