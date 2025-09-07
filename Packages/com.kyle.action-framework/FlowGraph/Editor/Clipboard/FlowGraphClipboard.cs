using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Flow.EditorView
{
    public class FlowGraphClipboard : ScriptableSingleton<FlowGraphClipboard>
    {
        [SerializeField]
        private readonly List<FlowGraphCopyData> copyDatas = new List<FlowGraphCopyData>();

        public static FlowGraphCopyData GetCopyData(FlowGraph graph)
        {
            var monoScript = MonoScript.FromScriptableObject(graph);
            foreach (var data in instance.copyDatas)
            {
                if (data.GraphScript == monoScript)
                {
                    return data;
                }
            }
            return null;
        }
        public static void SetCopy(FlowGraphCopyData data)
        {
            var monoScript = data.GraphScript;
            instance.copyDatas.RemoveAll(d => d.GraphScript == monoScript);
            instance.copyDatas.Add(data);
        }

        private void OnEnable()
        {
            copyDatas.RemoveAll(d => !d.GraphScript);
        }

    }
}
