using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Serialization;

namespace Flow.EditorView
{
    public class FlowNodeOutputPortField
    {
        public Type DataType;
        public string Name;
        public string OldName;
        public string ShowName;
    }

    public class FlowNodeInputPortField
    {
        public string[] Path;
        public Type FieldType;
        public string Name;
        public string OldName;

        public FlowNodeInputPortField(FieldInfo field)
        {
            var attr = field.GetCustomAttribute<FormerlySerializedAsAttribute>();
            if (attr != null)
                OldName = attr.oldName;
            FieldType = field.FieldType;
            Name = field.Name;
        }

    }

    public enum NodeOutputType
    {
        None,
        Normal,
        Condition,
        Dynamic,
    }

    public class FlowNodeTypeInfo
    {
        public Type NodeType;
        public string ShowName;
        public bool HasInput;
        public NodeOutputType OutputType = NodeOutputType.None;
        public List<FlowNodeInputPortField> InputFields = new List<FlowNodeInputPortField>();
        public List<FlowNodeOutputPortField> OutputFields = new List<FlowNodeOutputPortField>();
        public Type DynamicPortType;
        public FieldInfo DynamicPortField;
    }
    public static class FlowNodeTypeUtil
    {
        private readonly static Dictionary<Type, FlowNodeTypeInfo> nodeTypeInfos = new Dictionary<Type, FlowNodeTypeInfo>();

        private static void CollectDataPortFields(Type type, string path, FlowNodeTypeInfo typeInfo)
        {
            List<FieldInfo> fields = new List<FieldInfo>();
            var currentType = type;
            while (currentType != null)
            {
                if (currentType == typeof(object))
                    break;
                var typeFields = currentType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                fields.InsertRange(0, typeFields);
                currentType = currentType.BaseType;
            }
            foreach (var field in fields)
            {
                if(field.IsDefined(typeof(DynamicOutputAttribute)))
                {
                    if(typeInfo.OutputType != NodeOutputType.Dynamic)
                    {
                        UnityEngine.Debug.LogError($"节点类型标记 [DynamicOutputAttribute] 必须继承自 IFlowDynamicOutputable 接口 : {type.FullName}, {field.Name}");
                    }
                    else if (field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        if (typeInfo.DynamicPortField != null)
                        {
                            UnityEngine.Debug.LogError($"节点类型有重复标记 [DynamicOutputAttribute],仅支持一个字段 : {type.FullName}, {field.Name}");
                        }
                        else
                        {
                            typeInfo.DynamicPortType = field.FieldType.GetGenericArguments()[0];
                            typeInfo.DynamicPortField = field;
                        }
                        continue;
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"节点类型标记 [DynamicOutputAttribute] 字段必须为 List<T> 类型 : {type.FullName}, {field.Name}");
                    }
                }
                if(typeof(IOutputData).IsAssignableFrom(field.FieldType))
                {
                    if(field.FieldType.IsGenericType)
                    {
                        var t = field.FieldType.GetGenericArguments()[0];
                        FlowNodeOutputPortField output = new FlowNodeOutputPortField { DataType = t};
                        var attr = field.GetCustomAttribute<FormerlySerializedAsAttribute>();
                        if (attr != null)
                            output.OldName = attr.oldName;
                        output.Name = field.Name;
                        var alias = field.GetCustomAttribute<AliasAttribute>();
                        if (alias != null)
                            output.ShowName = alias.Name;
                        else
                            output.ShowName = field.Name;
                        typeInfo.OutputFields.Add(output);
                    }
                }
                else if (field.IsDefined(typeof(InputableAttribute)))
                {
                    var f = new FlowNodeInputPortField(field);
                    List<string> paths = new List<string>();
                    if (path != null)
                        paths.AddRange(path.Split('.'));
                    paths.Add(field.Name);
                    f.Path = paths.ToArray();
                    typeInfo.InputFields.Add(f);
                }
                else if(path == null && field.IsDefined(typeof(ExpandedInParentAttribute))) //仅支持一层
                {
                    if(path == null)
                        CollectDataPortFields(field.FieldType, field.Name, typeInfo);
                    else
                        CollectDataPortFields(field.FieldType, path + "." + field.Name, typeInfo);
                }
            }
        }

        private static FlowNodeTypeInfo BuildInfo(Type nodeType)
        {
            if (!nodeType.IsSubclassOf(typeof(FlowNode)))
                return null;
            FlowNodeTypeInfo typeInfo = new FlowNodeTypeInfo();
            typeInfo.NodeType = nodeType;
            var alias = nodeType.GetCustomAttribute<AliasAttribute>();
            typeInfo.ShowName = alias != null ? alias.Name : nodeType.Name;

            var interfaces = nodeType.GetInterfaces();

            typeInfo.HasInput = interfaces.Contains(typeof(IFlowInputable));
            var outputInterfaces = interfaces.Where(it => typeof(IFlowOutputable).IsAssignableFrom(it));
            if(outputInterfaces.Contains(typeof(IFlowDynamicOutputable)))
            {
                typeInfo.OutputType = NodeOutputType.Dynamic;
            }
            else if (outputInterfaces.Contains(typeof(IFlowConditionable)))
            {
                typeInfo.OutputType = NodeOutputType.Condition;
            }
            else if (outputInterfaces.Contains(typeof(IFlowOutputable)))
            {
                typeInfo.OutputType = NodeOutputType.Normal;
            }
            else
            {
                typeInfo.OutputType = NodeOutputType.None;
            }
            CollectDataPortFields(nodeType, null, typeInfo);
            return typeInfo;
        }

        public static FlowNodeTypeInfo GetNodeTypeInfo(Type nodeType)
        {
            if (!nodeTypeInfos.TryGetValue(nodeType, out var typeInfo))
            {
                typeInfo = BuildInfo(nodeType);
                nodeTypeInfos[nodeType] = typeInfo;
            }
            return typeInfo;
        }
    }
}
