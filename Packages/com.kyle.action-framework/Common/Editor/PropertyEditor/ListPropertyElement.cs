using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace PropertyEditor
{
    public class ListPropertyElement : PropertyElement
    {
        private readonly System.Type elementType;
        private readonly ListView listView = new ListView();
        private readonly List<PropertyElement> children = new List<PropertyElement>();
        private readonly IList rawValue;
        private IList sourceList;
        private float labelWidth = LabelMinWidth;
        public override bool ReadOnly { get => listView.enabledSelf; set => listView.SetEnabled(!value); }

        public ListPropertyElement(System.Type elementType)
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
            sourceList = value as IList;
            rawValue.Clear();
            if (sourceList != null)
            {
                foreach (var item in sourceList)
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
            if (sourceList == null)
            {
                sourceList = System.Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType)) as IList;
                if(Field != null)
                {
                    using var e = PropertyValueChangedEvent.GetPooled(this, sourceList, Field, Index);
                    SendEvent(e);
                }
            }
            sourceList.Clear();
            foreach (var item in rawValue)
            {
                sourceList.Add(item);
            }
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if(i >= rawValue.Count)
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
            sourceList[evt.Index] = evt.Value;
        }

    }
}
