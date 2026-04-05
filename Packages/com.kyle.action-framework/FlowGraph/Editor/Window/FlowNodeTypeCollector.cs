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
                    if (type.IsAbstract) continue;

                    // 原有路径：TFlowNode<T> 派生类，Tag 来自 T 实现的接口上的 [FlowTagAttribute]
                    var dataType = type.GetGenericParam(typeof(TFlowNode<>));
                    if (dataType != null)
                    {
                        string dataTypeTag = GetTypeTag(dataType);
                        if (dataTypeTag != null)
                            AddToNodeTypes(nodeTypes, dataTypeTag, type);
                        continue;
                    }

                    // 新增路径：TSubGraphNode<TData, TGraph> 派生类，Tag 来自 TData 实现的接口上的 [FlowTagAttribute]
                    var tSubDataType = type.GetGenericParam(typeof(TSubGraphNode<,>), 0);
                    if (tSubDataType != null)
                    {
                        string tag = GetTypeTag(tSubDataType);
                        if (tag != null)
                            AddToNodeTypes(nodeTypes, tag, type);
                    }
                }
            }
        }

        private static void AddToNodeTypes(Dictionary<string, List<Type>> dict, string tag, Type type)
        {
            if (!dict.TryGetValue(tag, out var list))
            {
                list = new List<Type>();
                dict.Add(tag, list);
            }
            if (!list.Contains(type))
                list.Add(type);
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
