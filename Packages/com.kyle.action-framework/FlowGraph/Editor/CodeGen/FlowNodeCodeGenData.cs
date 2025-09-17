using CodeGen;
using System;
using System.Linq;
using System.Reflection;

namespace Flow.EditorView
{
    public class FlowNodeCodeGenData
    {
        public Type DataType;
        public string DataTypeName;//包含命名空间
        public string RuntimeDataTypeName;
        public string NodeTypeName;//不包含命名空间
        public string ExecutorTypeName;//不包含命名空间
        public bool IsUpdateable;
        public bool IsConditional;
        public bool IsDynamicOutput;
        public bool IsPureDataNode;
        public FieldInfo[] InputFields;


        public FlowNodeCodeGenData(Type type)
        {
            DataType = type;
            DataTypeName = GeneratorUtils.TypeToName(type);
            RuntimeDataTypeName = FlowCodeGenHelper.DataTypeToRuntimrDataTypeName(type);
            NodeTypeName = FlowCodeGenHelper.DataTypeToNodeTypeName(type);
            ExecutorTypeName = FlowCodeGenHelper.DataTypeToExecutorTypeName(type);
            IsUpdateable = typeof(IFlowUpdateable).IsAssignableFrom(type);
            IsConditional = typeof(IFlowConditionable).IsAssignableFrom(type);
            IsDynamicOutput = typeof(IFlowDynamicOutputable).IsAssignableFrom(type);
            IsPureDataNode = typeof(IFlowDataProvider).IsAssignableFrom(type) && !typeof(IFlowInputable).IsAssignableFrom(type);

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            InputFields = fields.Where((f) => f.IsDefined(typeof(InputableAttribute), true)).ToArray();
        }
    }
}
