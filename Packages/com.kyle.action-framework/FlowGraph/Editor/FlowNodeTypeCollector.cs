using System;
using System.Collections.Generic;
using System.Reflection;

namespace Flow.EditorView
{
    public static class FlowNodeTypeCollector
    {
        private static Dictionary<string, List<Type>> nodeTypes;

        public static IReadOnlyList<Type> GetNodeTypes(string tag)
        {
            if (nodeTypes == null)
            {
                nodeTypes = new Dictionary<string, List<Type>>();
                foreach (var type in TypeCollector<FlowNode>.Types)
                {
                    var tagAttr = type.GetCustomAttribute<FlowTagAttribute>();
                    if (tagAttr != null)
                    {
                        if (!nodeTypes.TryGetValue(tagAttr.Tag, out var list))
                        {
                            list = new List<Type>();
                            nodeTypes.Add(tagAttr.Tag, list);
                        }
                        list.Add(type);
                    }
                }
            }
            if (nodeTypes.ContainsKey(tag))
            {
                return nodeTypes[tag];
            }
            return null;
        }

    }
}
