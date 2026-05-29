using System;
using CodeGen;

namespace ECSEditor
{
    public static class ViewECSClearupGenerator
    {
        public static void Gen(string path, ComponentCollector collector)
        {
            var writer = new CSharpCodeWriter();
            writer.WriteLine("//工具生成，手动修改无效");
            using (new CSharpCodeWriter.NameSpaceScop(writer, collector.NameSpace))
            {
                using (new CSharpCodeWriter.Scop(writer, "public partial class ViewComponentClearup"))
                {
                    using (new CSharpCodeWriter.Scop(writer, "public static void Init()"))
                    {
                        var type = Type.GetType("VECS.ViewComponentClearup");
                        if (type != null)
                        {
                            writer.WriteLine("//OnRemove");
                            foreach (var method in type.GetMethods())
                            {
                                if (method.Name != "OnRemove")
                                    continue;
                                var paramList = method.GetParameters();
                                if (paramList.Length != 2)
                                    continue;
                                if (paramList[0].ParameterType != typeof(VECS.ViewEntity))
                                    continue;
                                var componentType = paramList[1].ParameterType;
                                if (typeof(VECS.IViewComponent).IsAssignableFrom(componentType))
                                {
                                    writer.WriteLine($"VECS.ViewComponentClear<{componentType.Name}>.OnRemove = OnRemove;");
                                }
                            }
                        }
                        writer.WriteLine("//OnReset");
                        foreach (var ty in collector.Types)
                        {
                            if (ty.ResetType == ComponentCollector.ResetType.None)
                                continue;
                            writer.WriteLine($"VECS.ViewComponentClear<{ty.Type.Name}>.OnReset = OnReset;");
                        }
                    }
                }
            }

            GeneratorUtils.WriteToFile(path, writer.ToString());
        }

    }
}
