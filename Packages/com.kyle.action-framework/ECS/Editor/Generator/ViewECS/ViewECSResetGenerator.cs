using CodeGen;

namespace ECSEditor
{
    public static class ViewECSResetGenerator
    {
        public static void Gen(string path, ComponentCollector collector, System.Func<System.Type, string, string> customReset)
        {
            var writer = new CSharpCodeWriter();
            writer.WriteLine("//工具生成，手动修改无效");
            using (new CSharpCodeWriter.NameSpaceScop(writer, collector.NameSpace))
            {
                using (new CSharpCodeWriter.Scop(writer, "public partial class ViewComponentClearup"))
                {
                    foreach (var ty in collector.Types)
                    {
                        if (ty.ResetType == ComponentCollector.ResetType.None)
                            continue;
                        writer.WriteLine($"public static void OnReset({ty.Type.Name} value)");
                        using (new CSharpCodeWriter.Scop(writer))
                        {
                            var baseTy = ty.Base;
                            while (baseTy != null)
                            {
                                if (ty.ResetType != ComponentCollector.ResetType.None)
                                {
                                    writer.WriteLine($"OnReset(({baseTy.Type.Name})value);");
                                    break;
                                }
                                baseTy = baseTy.Base;
                            }
                            GeneratorUtils.WriteReset(writer, ty.Fields, customReset);
                        }
                    }
                }
            }
            GeneratorUtils.WriteToFile(path, writer.ToString());
        }
    }
}
