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
    }
}
