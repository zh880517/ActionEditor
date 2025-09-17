using CodeGen;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Flow.EditorView
{
    public static class FlowCodeGenHelper
    {
        public static string DataTypeToNodeTypeName(Type type)
        {
            return $"{type.Name}Node";
        }
        public static string DataTypeToExecutorTypeName(Type type)
        {
            return $"{type.Name}Executor";
        }
        public static string DataTypeToRuntimrDataTypeName(Type type)
        {
            return $"{type.Name}RuntimeData";
        }

        public static CSharpCodeWriter WriteRuntimeDataDefine(IReadOnlyList<Type> types, string nameSpace)
        {
            CSharpCodeWriter writer = new CSharpCodeWriter();
            using (new CSharpCodeWriter.NameSpaceScop(writer, nameSpace))
            {
                foreach (var item in types)
                {
                    writer.WriteLine($"public class {DataTypeToRuntimrDataTypeName(item)} : Flow.TFlowNodeRuntimeData<{GeneratorUtils.TypeToName(item, nameSpace)}>{{}}");
                }
            }
            return writer;
        }

        private static void WriteInputFieldFill(CSharpCodeWriter writer, FieldInfo field)
        {
            int hashId = UnityEngine.Animator.StringToHash(field.Name);
            writer.WriteLine($"context.TryGetInputValue(nodeId, {hashId}, ref data.{field.Name})");
        }

        public static CSharpCodeWriter WriteExecutorDefine(IReadOnlyList<FlowNodeCodeGenData> types, string contextTypeName, string nameSpace)
        {
            CSharpCodeWriter writer = new CSharpCodeWriter();
            using (new CSharpCodeWriter.NameSpaceScop(writer, nameSpace))
            {
                foreach (var item in types)
                {
                    string dataTypeName = GeneratorUtils.TypeToName(item.DataType, nameSpace);
                    string runtimeDataTypeName = item.NodeTypeName;
                    string className = item.ExecutorTypeName;
                    string baseClassName = "Flow.TNormalExecutor";
                    string functionFillInputs = $"protected override void FillInputs({contextTypeName} context, int nodeId, ref {dataTypeName} data)";
                    if (item.IsUpdateable)
                    {
                        if (item.IsDynamicOutput)
                        {
                            baseClassName = "Flow.TDynamicOutputUpdateableExecutor";
                        }
                        else if (item.IsConditional)
                        {
                            baseClassName = "Flow.TConditionUpdateableExecutor";
                        }
                        else
                        {
                            baseClassName = "Flow.TUpdateableExecutor";
                        }
                    }
                    else if(item.IsDynamicOutput)
                    {
                        baseClassName = "Flow.TDynamicOutputExecutor";
                    }
                    else if(item.IsConditional)
                    {
                        baseClassName = "Flow.TConditionExecutor";
                    }
                    string define = $"public partial class {className} : {baseClassName}<{dataTypeName}, {runtimeDataTypeName}, {contextTypeName}>";
                    if (item.InputFields.Length > 0)
                    {
                        using (new CSharpCodeWriter.Scop(writer, define))
                        {
                            writer.WriteLine("protected override bool HasInput => true;");
                            using (new CSharpCodeWriter.Scop(writer, functionFillInputs))
                            {
                                foreach (var field in item.InputFields)
                                {
                                    WriteInputFieldFill(writer, field);
                                }
                            }
                        }
                    }
                    else
                    {
                        writer.WriteLine(define + " {}");
                    }

                }
            }
            return writer;
        }

        public static CSharpCodeWriter WriteNodeScript(FlowNodeCodeGenData node, string nameSpace)
        {
            CSharpCodeWriter writer = new CSharpCodeWriter();
            writer.WriteLine("using Flow;");
            using (new CSharpCodeWriter.NameSpaceScop(writer, nameSpace))
            {
                using (new CSharpCodeWriter.Scop(writer, $"public partial class {node.NodeTypeName} : TFlowNode<{node.DataTypeName}>"))
                {
                    using (new CSharpCodeWriter.Scop(writer, $"protected override FlowNodeRuntimeData CreateExport()"))
                    {
                        writer.WriteLine($"return new {node.RuntimeDataTypeName}();");
                    }
                }
            }
            return writer;
        }

        public static CSharpCodeWriter WriteExecutor(FlowNodeCodeGenData node, string contextTypeName, string nameSpace)
        {
            CSharpCodeWriter writer = new CSharpCodeWriter(true);
            using (new CSharpCodeWriter.NameSpaceScop(writer, nameSpace))
            {
                using (new CSharpCodeWriter.Scop(writer, $"public partial class {node.ExecutorTypeName}"))
                {
                    if(node.IsUpdateable)
                    {
                        writer.WriteLine("//protected override UpdateNodeContext CreateNodeContext() => UpdateNodeContext.None;");
                        writer.WriteLine($"//protected override void OnEnter({contextTypeName} context, {node.DataTypeName} data, UpdateNodeContext nodeContext) {{}}");
                        if(node.IsDynamicOutput)
                        {
                            using (new CSharpCodeWriter.Scop(writer, $"protected override int OnUpdate({contextTypeName} context, {node.DataTypeName} data, UpdateNodeContext nodeContext)"))
                            {
                                writer.WriteLine($"throw new System.NotImplementedException(\"{node.ExecutorTypeName}.OnUpdate 未实现\");");
                            }
                        }
                        else if(node.IsConditional)
                        {
                            using (new CSharpCodeWriter.Scop(writer, $"protected override ResultType OnUpdate({contextTypeName} context, {node.DataTypeName} data, UpdateNodeContext nodeContext)"))
                            {
                                writer.WriteLine($"throw new System.NotImplementedException(\"{node.ExecutorTypeName}.OnUpdate 未实现\");");
                            }
                        }
                        else
                        {
                            using (new CSharpCodeWriter.Scop(writer, $"protected override bool OnUpdate({contextTypeName} context, {node.DataTypeName} data, UpdateNodeContext nodeContext)"))
                            {
                                writer.WriteLine($"throw new System.NotImplementedException(\"{node.ExecutorTypeName}.OnUpdate 未实现\");");
                            }
                        }
                        writer.WriteLine($"//protected override void OnExit({contextTypeName} context, {node.DataTypeName} data, UpdateNodeContext nodeContext) {{}}");
                    }
                    else if(node.IsDynamicOutput)
                    {
                        using (new CSharpCodeWriter.Scop(writer, $"protected override int Select({contextTypeName} context, {node.DataTypeName} data)"))
                        {
                            writer.WriteLine($"throw new System.NotImplementedException(\"{node.ExecutorTypeName}.Select 未实现\");");
                        }
                    }
                    else if (node.IsConditional)
                    {
                        using (new CSharpCodeWriter.Scop(writer, $"protected override bool OnCondition({contextTypeName} context, {node.DataTypeName} data)"))
                        {
                            writer.WriteLine($"throw new System.NotImplementedException(\"{node.ExecutorTypeName}.OnCondition 未实现\");");
                        }
                    }
                    else
                    {
                        using (new CSharpCodeWriter.Scop(writer, $"protected override void OnExecute({contextTypeName} context, {node.DataTypeName} data)"))
                        {
                            writer.WriteLine($"throw new System.NotImplementedException(\"{node.ExecutorTypeName}.OnExecute 未实现\");");
                        }
                    }
                }
            }
            return writer;
        }
    }
}
