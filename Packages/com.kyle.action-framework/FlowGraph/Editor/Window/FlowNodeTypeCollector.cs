using System;
using System.Collections.Generic;
using System.Reflection;

namespace Flow.EditorView
{
    public static class FlowNodeTypeCollector
    {
        private static Dictionary<string, List<Type>> nodeTypes;
        private static Dictionary<Type, string> typeTags = new Dictionary<Type, string>();

        public static IReadOnlyList<Type> GetNodeTypes(string tag)
        {
            if (nodeTypes == null)
            {
                nodeTypes = new Dictionary<string, List<Type>>();
                foreach (var type in TypeCollector<FlowNode>.Types)
                {
                    var dataType = type.GetGenericParam(typeof(TFlowNode<>));
                    if (dataType == null)
                        continue;
                    string dataTypeTag = GetTypeTag(dataType);
                    if(dataTypeTag != null)
                    {

                        if (!nodeTypes.TryGetValue(dataTypeTag, out var list))
                        {
                            list = new List<Type>();
                            nodeTypes.Add(dataTypeTag, list);
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

        public static string GetTypeTag(Type type)
        {
            if (!typeTags.TryGetValue(type, out var tag))
            {
                var interfaces = type.GetInterfaces();
                foreach (var item in interfaces)
                {
                    var tagAttr = item.GetCustomAttribute<FlowTagAttribute>();
                    if (tagAttr != null)
                    {
                        tag = tagAttr.Tag;
                        break;
                    }
                }
                typeTags[type] = tag;
            }
            return tag;
        }

    }
}
