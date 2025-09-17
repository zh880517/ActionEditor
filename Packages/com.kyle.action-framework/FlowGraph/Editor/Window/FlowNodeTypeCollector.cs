using System;
using System.Collections.Generic;
using System.Reflection;

namespace Flow.EditorView
{
    public static class FlowNodeTypeCollector
    {
        private static Dictionary<string, List<Type>> nodeTypes;
        private static Dictionary<string, List<Type>> dataTypes;
        private static readonly Dictionary<Type, string> typeTags = new Dictionary<Type, string>();

        private static void InitNodeTypes()
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
                    if (dataTypeTag != null)
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
        }

        private static void InitDataTypes()
        {
            if (dataTypes == null)
            {
                dataTypes = new Dictionary<string, List<Type>>();
                foreach (var type in TypeCollector<IFlowNode>.Types)
                {
                    string dataTypeTag = GetTypeTag(type);
                    if (dataTypeTag != null)
                    {
                        if (!dataTypes.TryGetValue(dataTypeTag, out var list))
                        {
                            list = new List<Type>();
                            dataTypes.Add(dataTypeTag, list);
                        }
                        list.Add(type);
                    }
                }
            }
        }

        public static IReadOnlyList<Type> GetNodeTypes(string tag)
        {
            InitNodeTypes();
            if (nodeTypes.TryGetValue(tag, out var result))
            {
                return result;
            }
            return null;
        }

        public static Type DataTypeToNodeType(Type dataType, string tag)
        {
            var types = GetNodeTypes(tag);
            if (types != null)
            {
                foreach (var type in types)
                {
                    var dt = type.GetGenericParam(typeof(TFlowNode<>));
                    if (dt == dataType)
                    {
                        return type;
                    }
                }
            }
            return null;
        }

        public static IReadOnlyList<Type> GetDataTypes(string tag)
        {
            InitDataTypes();
            if (dataTypes.TryGetValue(tag, out var result))
            {
                return result;
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
