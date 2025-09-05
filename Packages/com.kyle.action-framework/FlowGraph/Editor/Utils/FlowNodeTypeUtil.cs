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
        public string OldName;
        public AliasAttribute Alias;
    }

    public class FlowNodeInputPortField
    {
        public FlowNodeInputPortField Parent;
        public FieldInfo Field;
        public Type FieldType => Field.FieldType;
        public string Name => Field.Name;
        public string OldName;
        public AliasAttribute Alias;

        public FlowNodeInputPortField(FieldInfo field)
        {
            Field = field;
            var attr = field.GetCustomAttribute<FormerlySerializedAsAttribute>();
            if (attr != null)
                OldName = attr.oldName;
            Alias = field.GetCustomAttribute<AliasAttribute>();
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
        public bool HasInput;
        public NodeOutputType OutputType = NodeOutputType.None;
        public List<FlowNodeInputPortField> InputFields = new List<FlowNodeInputPortField>();
        public List<FlowNodeOutputPortField> OutputFields = new List<FlowNodeOutputPortField>();
        public Type DynamicPortType;
        public PropertyInfo DynamicProperty;
    }
    public static class FlowNodeTypeUtil
    {
        private readonly static Dictionary<Type, FlowNodeTypeInfo> nodeTypeInfos = new Dictionary<Type, FlowNodeTypeInfo>();

        private static void CollectDataPortFields(Type type, FlowNodeInputPortField parent, FlowNodeTypeInfo typeInfo)
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
                var f = new FlowNodeInputPortField(field) { Parent = parent };
                if(typeof(IOutputData).IsAssignableFrom(field.FieldType))
                {
                    if(field.FieldType.IsGenericType)
                    {
                        var t = field.FieldType.GetGenericArguments()[0];
                        FlowNodeOutputPortField output = new FlowNodeOutputPortField { DataType = t };
                        var attr = field.GetCustomAttribute<FormerlySerializedAsAttribute>();
                        if (attr != null)
                            output.OldName = attr.oldName;
                        output.Alias = field.GetCustomAttribute<AliasAttribute>();
                        typeInfo.OutputFields.Add(output);
                    }
                }
                else if (field.GetCustomAttribute<InputableAttribute>() != null)
                {
                    typeInfo.InputFields.Add(f);
                }
                else if(parent == null && field.GetCustomAttribute<ExpandedInParentAttribute>() != null) //仅支持一层
                {
                    CollectDataPortFields(field.FieldType, f, typeInfo);
                }
            }
        }

        private static FlowNodeTypeInfo BuildInfo(Type nodeType)
        {
            if (!nodeType.IsSubclassOf(typeof(FlowNode)))
                return null;
            FlowNodeTypeInfo typeInfo = new FlowNodeTypeInfo();
            var interfaces = nodeType.GetInterfaces();

            typeInfo.HasInput = interfaces.Contains(typeof(IFlowInputable));
            var outputInterfaces = interfaces.Where(it => typeof(IFlowOutputable).IsAssignableFrom(it));
            var dyanmicInterface = outputInterfaces.FirstOrDefault(it => it.IsGenericType && it.GetGenericTypeDefinition() == typeof(IFlowDynamicOutputable<>));
            if (dyanmicInterface != null)
            {
                typeInfo.OutputType = NodeOutputType.Dynamic;
                typeInfo.DynamicPortType = dyanmicInterface.GetGenericArguments()[0];
                typeInfo.DynamicProperty = dyanmicInterface.GetProperty("Ports");
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

        private static readonly Dictionary<FlowNodeInputPortField, StructedFieldEditor> s_cache = new Dictionary<FlowNodeInputPortField, StructedFieldEditor>();

        public static List<FieldEditor> BuildEditor(FlowNode node)
        {
            var typeInfo = GetNodeTypeInfo(node.GetType());
            if (typeInfo.InputFields.Count == 0)
                return null;
            List<FieldEditor> result = new List<FieldEditor>();
            StructedFieldEditor root = new StructedFieldEditor 
            {
                Value = node
            };
            s_cache.Clear();
            foreach (var item in typeInfo.InputFields)
            {
                FieldEditor editor = new FieldEditor { Field = item.Field };
                if(item.Parent == null)
                {
                    editor.Parent = root;
                }
                else
                {
                    editor.Parent = GetEditor(item.Parent, root, s_cache);
                }
                result.Add(editor);
            }
            s_cache.Clear();
            return result;
        }

        private static StructedFieldEditor GetEditor(FlowNodeInputPortField field, StructedFieldEditor root, Dictionary<FlowNodeInputPortField, StructedFieldEditor> cache)
        {
            if(!cache.TryGetValue(field, out var result))
            {
                result = new StructedFieldEditor { Field = field.Field };
                cache.Add(field, result);
                if(field.Parent == null)
                {
                    result.Parent = root;
                    result.RefreshValue();
                }
                else
                {
                    result.Parent = GetEditor(field.Parent, root, s_cache);
                }
            }
            return result;
        }
    }
}
