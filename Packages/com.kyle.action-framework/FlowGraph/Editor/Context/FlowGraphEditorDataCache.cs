using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Flow.EditorView
{
    public class FlowGraphEditorDataCache : ScriptableSingleton<FlowGraphEditorDataCache>
    {
        [System.Serializable]
        private struct EditorDataEntry
        {
            public EditorWindow Window;
            public FlowGraph Graph;
            public FlowGraphEditorData Data;
        }
        [SerializeField]
        private List<EditorDataEntry> entries = new List<EditorDataEntry>();

        public FlowGraphEditorData GetEditorData(EditorWindow window, FlowGraph graph)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if ((!entries[i].Window || entries[i].Window == window) && entries[i].Graph == graph)
                {
                    return entries[i].Data;
                }
            }
            var context = FlowGraphEditorContext.GetContext(graph.GetType());
            System.Type dataType = null;
            if (context != null)
            {
                dataType = context.GetEditorDataType();
            }
            if(dataType == null && !dataType.IsSubclassOf(typeof(FlowGraphEditorData)))
            {
                dataType = typeof(FlowGraphEditorData);
            }
            var newData = CreateInstance(dataType) as FlowGraphEditorData;
            newData.hideFlags = HideFlags.DontSave;
            var newEntry = new EditorDataEntry()
            {
                Window = window,
                Graph = graph,
                Data = newData
            };
            entries.Add(newEntry);
            return newData;
        }

        public void OnWindowDestroy(EditorWindow window)
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].Window == window)
                {
                    if (!entries[i].Graph)
                    {
                        DestroyImmediate(entries[i].Data);
                    }
                    entries.RemoveAt(i);
                }
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (!entries[i].Graph)
                {
                    DestroyImmediate(entries[i].Data);
                }
            }
        }

    }
}
