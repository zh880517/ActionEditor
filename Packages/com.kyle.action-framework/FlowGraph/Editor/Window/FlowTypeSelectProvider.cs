using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Flow.EditorView
{
    public class FlowTypeSelectProvider : ScriptableSingleton<FlowTypeSelectProvider>
    {
        [System.Serializable]
        class TypeSelect
        {
            public MonoScript GraphScript;
            public FlowTypeSelectWindow TypeSelectWindow;
        }
        [SerializeField]
        private List<TypeSelect> _typeSelects = new List<TypeSelect>();

        public static FlowTypeSelectWindow GetTypeSelectWindow(FlowGraph graph)
        {
            var mono = MonoScript.FromScriptableObject(graph);
            var typeSelect = instance._typeSelects.Find(t => t.GraphScript == mono);
            if (typeSelect == null)
            {
                typeSelect = new TypeSelect();
                typeSelect.GraphScript = mono;
                var tagsAttr = graph.GetType().GetCustomAttribute<FlowGraphTagsAttrribute>(true);
                if (tagsAttr != null)
                {
                    typeSelect.TypeSelectWindow = TypeSelectWindow.Create<FlowTypeSelectWindow>();
                    typeSelect.TypeSelectWindow.SetTags(tagsAttr.Tags);
                }
                instance._typeSelects.Add(typeSelect);
            }
            return typeSelect.TypeSelectWindow;
        }

        private void OnEnable()
        {
            foreach (var item in _typeSelects)
            {
                var tagsAttr = item.GraphScript.GetClass().GetCustomAttribute<FlowGraphTagsAttrribute>(true);
                if (tagsAttr != null)
                {
                    if (item.TypeSelectWindow == null)
                    {
                        item.TypeSelectWindow = TypeSelectWindow.Create<FlowTypeSelectWindow>();
                    }
                    item.TypeSelectWindow.SetTags(tagsAttr.Tags);
                }
                else
                {
                    if (item.TypeSelectWindow != null)
                    {
                        DestroyImmediate(item.TypeSelectWindow);
                        item.TypeSelectWindow = null;
                    }
                }
            }
        }

        private void OnDisable()
        {
            FlowGraphEditorContext.Destroy();
        }
    }
}
