using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Flow.EditorView
{
    public class FlowNodeDataPortField
    {
        public Type FieldType => Field.FieldType;
        public FieldInfo Field;
        public string Name => Field.Name;
        public string OldName;
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
        public List<FlowNodeDataPortField> DataPortFields = new List<FlowNodeDataPortField>();
        public Type DynamicPortType;
        public PropertyInfo DynamicProperty;
    }
    public static class FlowNodeTypeUtil
    {
        private readonly static Dictionary<Type, FlowNodeTypeInfo> nodeTypeInfos = new Dictionary<Type, FlowNodeTypeInfo>();

        private static void CollectDataPortFields(Type type, List<FlowNodeDataPortField> result)
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

            return typeInfo;
        }
    }
}
