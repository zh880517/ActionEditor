using CodeGen;
using System;
using System.Collections.Generic;

namespace Flow.EditorView
{
    public static class FlowCodeGenHelper
    {
        public static CSharpCodeWriter WriteRuntimeDataDefine(IReadOnlyList<Type> types, string nameSpace)
        {
            CSharpCodeWriter writer = new CSharpCodeWriter();

            return writer;
        }

        public static CSharpCodeWriter WriteExecutorDefine(IReadOnlyList<Type> types, string nameSpace)
        {
            CSharpCodeWriter writer = new CSharpCodeWriter();

            return writer;
        }

        public static CSharpCodeWriter WriteNodeScript(Type type, string nameSpace)
        {
            CSharpCodeWriter writer = new CSharpCodeWriter();

            return writer;
        }

        public static CSharpCodeWriter WriteExecutor(Type type, string nameSpace)
        {
            CSharpCodeWriter writer = new CSharpCodeWriter();
            return writer;
        }
    }
}
