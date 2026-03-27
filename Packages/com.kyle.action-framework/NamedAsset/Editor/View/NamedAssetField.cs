using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NamedAsset.Editor
{
    public class NamedAssetField : VisualElement, INotifyValueChanged<string>, IEventHandler
    {
        private static readonly string ussClassName = "unity-object-field";

        
        internal static readonly string labelUssClassName = ussClassName + "__label";

        public static readonly string selectorUssClassName = ussClassName + "__selector";
        public static readonly string objectUssClassName = ussClassName + "__object";
        private class NamedAssetElement : VisualElement
        {
            private static readonly string ussClassName = "unity-object-field-display";

            private static readonly string iconUssClassName = ussClassName + "__icon";

            private static readonly string labelUssClassName = ussClassName + "__label";

            private static readonly string acceptDropVariantUssClassName = ussClassName + "--accept-drop";
            private readonly Image iconField = new Image();
            private readonly Label labelField = new Label();
            private readonly VisualElement selectorField = new VisualElement();
            internal System.Type assetType = typeof(Object);
            internal Object value;
            internal NamedAssetField assetField;
            public NamedAssetElement()
            {
                //focusable = true;
                style.flexDirection = FlexDirection.Row;
                AddToClassList("unity-base-field__input");
                AddToClassList("unity-object-field__input");
                var element = new VisualElement();
                {
                    Add(element);
                    element.AddToClassList("unity-base-field__display");
                    element.AddToClassList("unity-object-field__object");
                    element.Add(iconField);
                    iconField.pickingMode = PickingMode.Ignore;
                    iconField.scaleMode = ScaleMode.ScaleAndCrop;
                    iconField.AddToClassList(iconUssClassName);
                    element.Add(labelField);
                    labelField.pickingMode = PickingMode.Ignore;
                    labelField.AddToClassList(labelUssClassName);
                }
                Add(selectorField);
                selectorField.AddToClassList("unity-object-field__selector");
                Update();
                selectorField.RegisterCallback<MouseDownEvent>(OnSelectorMouseDown);
            }
            internal void Update()
            {
                var content = EditorGUIUtility.ObjectContent(value, assetType);
                if (content != null)
                {
                    iconField.image = content.image;
                    labelField.text = content.text;
                }
            }
            [EventInterest(new System.Type[]
            {
                typeof(MouseDownEvent),
                typeof(DragUpdatedEvent),
                typeof(DragPerformEvent),
                typeof(DragLeaveEvent)
            })]
            protected override void HandleEventBubbleUp(EventBase evt)
            {
                base.HandleEventBubbleUp(evt);
                if (evt is MouseDownEvent mouseDown && mouseDown.button == 0)
                {
                    OnMouseDown(mouseDown);
                }
                if (enabledInHierarchy)
                {
                    if (evt is DragUpdatedEvent dragUpdated)
                    {
                        OnDragUpdated(dragUpdated);
                    }
                    else if (evt is DragPerformEvent dragPerform)
                    {
                        OnDragPerform(dragPerform);
                    }
                    else if (evt is DragLeaveEvent dragLeave)
                    {
                        OnDragLeave();
                    }
                }
            }
            private void OnMouseDown(MouseDownEvent evt)
            {
                if (evt.clickCount == 1)
                {
                    if (!evt.shiftKey && !evt.ctrlKey && value)
                    {
                        EditorGUIUtility.PingObject(value);
                    }
                    evt.StopPropagation();
                }
                else if (evt.clickCount == 2)
                {
                    if (value)
                    {
                        AssetDatabase.OpenAsset(value);
                        GUIUtility.ExitGUI();
                    }

                    evt.StopPropagation();
                }
            }
            private Object DNDValidateObject(out string key)
            {
                Object[] objectReferences = DragAndDrop.objectReferences;
                foreach (var obj in objectReferences)
                {
                    if (!EditorUtility.IsPersistent(obj))
                        continue;
                    if (assetType.IsAssignableFrom(obj.GetType()))
                    {
                        string path = AssetDatabase.GetAssetPath(obj);
                        key = AssetCollector.instance.AssetPathToKey(path);
                        if (key != null)
                            return obj;
                    }
                }
                key = null;
                return null;
            }
            private void OnDragUpdated(DragUpdatedEvent evt)
            {
                var obj = DNDValidateObject(out string _);
                if (obj)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                    EnableInClassList(acceptDropVariantUssClassName, enable: true);
                    evt.StopPropagation();
                }
            }
            private void OnDragPerform(DragPerformEvent evt)
            {
                var obj = DNDValidateObject(out string key);
                if (obj)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                    value = obj;
                    DragAndDrop.AcceptDrag();
                    RemoveFromClassList(acceptDropVariantUssClassName);
                    evt.StopPropagation();
                    Update();
                    assetField.OnValueChange(key);
                }
            }
            private void OnSelectorMouseDown(MouseDownEvent evt)
            {
                Vector2 offset = EditorWindow.focusedWindow.position.position;

                NamedAssetSelectWindow.Show(evt.mousePosition + offset, assetType, (key) =>
                {
                    value = AssetCollector.instance.LoadAsset<Object>(key);
                    Update();
                    assetField.OnValueChange(key);
                });
            }
            private void OnDragLeave()
            {
                EnableInClassList(acceptDropVariantUssClassName, enable: false);
            }
        }
        /*
        public new class UxmlFactory : UxmlFactory<NamedAssetField, UxmlTraits>
        {
        }
        */
        private readonly Label labelElement = new Label();
        private readonly NamedAssetElement namedAssetElement = new NamedAssetElement();
        private string _value;
        public string value 
        { 
            get => _value; 
            set => OnValueChange(value); 
        }
        public string Label
        {
            get => labelElement.text;
            set => labelElement.text = value;
        }
        public System.Type AssetType
        {
            get => namedAssetElement.assetType;
            set 
            {
                namedAssetElement.assetType = value;
                namedAssetElement.Update();
            }
        }

        public NamedAssetField():this(null)
        {
        }

        public NamedAssetField(string lable)
        {
            focusable = true;
            style.flexDirection = FlexDirection.Row;
            labelElement.style.minWidth = 123f;
            labelElement.text = lable;
            Add(labelElement);
            namedAssetElement.assetField = this;
            Add(namedAssetElement);
            AddToClassList("unity-base-field");
            AddToClassList(ussClassName);
            labelElement.AddToClassList("unity-base-field__label");
            labelElement.AddToClassList(labelUssClassName);
        }

        internal void OnValueChange(string key)
        {
            if (key == _value)
                return;
            string previousValue = _value;
            _value = key;
            namedAssetElement.value = AssetCollector.instance.LoadAsset<Object>(value);
            namedAssetElement.Update();
            using (ChangeEvent<string> changeEvent = ChangeEvent<string>.GetPooled(previousValue, value))
            {
                changeEvent.target = this;
                SendEvent(changeEvent);
            }
        }

        public void SetValueWithoutNotify(string newValue)
        {
            _value = newValue;
            namedAssetElement.value = AssetCollector.instance.LoadAsset<Object>(newValue);
            namedAssetElement.Update();
        }
        [EventInterest(new System.Type[]
        {
            typeof(KeyDownEvent),
        })]
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            if (evt is KeyDownEvent keyDown)
            {
                if (keyDown.keyCode == KeyCode.Delete || keyDown.keyCode == KeyCode.Backspace)
                {
                    OnValueChange(null);
                }
            }
        }
    }
}
