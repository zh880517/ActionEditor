using CodeGen;

namespace ECSEditor
{
    public class ECSLiteClearupGenerator
    {
        public static void Gen(string path, ComponentCollector collector)
        {
            var writer = new CSharpCodeWriter();
            writer.WriteLine("//工具生成，手动修改无效");
            using (new CSharpCodeWriter.NameSpaceScop(writer, collector.NameSpace))
            {
                using (new CSharpCodeWriter.Scop(writer, $"public partial class {collector.ContextName}ComponentReset"))
                {
                    using (new CSharpCodeWriter.Scop(writer, "public static void Init()"))
                    {
                        foreach (var ty in collector.Types)
                        {
                            if (ty.ResetType == ComponentCollector.ResetType.None)
                                continue;
                            writer.WriteLine($"ECSLite.ComponentReset<{ty.Type.Name}>.OnReset = OnReset;");
                        }
                    }
                }
            }

            GeneratorUtils.WriteToFile(path, writer.ToString());
        }

    }
}
