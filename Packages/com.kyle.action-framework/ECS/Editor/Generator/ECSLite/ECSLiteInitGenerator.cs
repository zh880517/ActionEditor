using CodeGen;

namespace ECSEditor
{
    public static class ECSLiteInitGenerator
    {
        public static void Gen(string path, ComponentCollector collector)
        {
            var writer = new CSharpCodeWriter();
            writer.WriteLine("//工具生成，手动修改无效");
            using (new CSharpCodeWriter.NameSpaceScop(writer, collector.NameSpace))
            {
                using (new CSharpCodeWriter.Scop(writer, $"public class {collector.ContextName}ECS"))
                {
                    writer.WriteLine($"private static int s_ComponentCount = {collector.ValidCount - collector.UniqueCount};");
                    writer.WriteLine($"private static int s_UniqueComponentCount = {collector.UniqueCount};");
                    writer.WriteLine($"private static int s_StatcicComponentCount = {collector.StaticTypes.Count};");
                    writer.WriteLine("public static int ComponentCount => s_ComponentCount;");
                    writer.WriteLine("public static int StaticComponentCount => s_StatcicComponentCount;");

                    WriteId(writer, collector);
                    using (new CSharpCodeWriter.Scop(writer, $"public static {collector.ContextName}Context CreateContext()"))
                    {
                        writer.WriteLine($"{collector.ContextName}Context context = new {collector.ContextName}Context(s_ComponentCount, s_UniqueComponentCount, s_StatcicComponentCount);");
                        foreach (var ty in collector.Types)
                        {
                            if (ty.Type.IsAbstract)
                                continue;
                            if (ty.IsUnique)
                                writer.WriteLine($"context.InitUniqueComponentCollector<{ty.Type.Name}>();");
                            else if (ty.IsFlag)
                                writer.WriteLine($"context.InitFlagComponentCollector<{ty.Type.Name}>();");
                            else
                                writer.WriteLine($"context.InitComponentCollector<{ty.Type.Name}>();");
                        }
                        writer.WriteLine("return context;");
                    }
                }
            }

            GeneratorUtils.WriteToFile(path, writer.ToString());
        }

        private static void WriteId(CSharpCodeWriter writer, ComponentCollector collector)
        {
            using (new CSharpCodeWriter.Scop(writer, $"static {collector.ContextName}ECS()"))
            {

                for (int i = 0; i < collector.StaticTypes.Count; ++i)
                {
                    var type = collector.StaticTypes[i];
                    writer.WriteLine($"ECSLite.StaticComponentIdentity<{type.Name}>.Id = {i};");
                }
                int id = 0;
                foreach (var ty in collector.Types)
                {
                    if (ty.Type.IsAbstract)
                        continue;
                    writer.WriteLine($"ECSLite.ComponentIdentity<{ty.Type.Name}>.Id = {id++};");
                    if (ty.IsUnique)
                        writer.WriteLine($"ECSLite.ComponentIdentity<{ty.Type.Name}>.Unique = true;");
                }
                writer.WriteLine($"{collector.ContextName}ComponentReset.Init();");
            }
        }
    }
}
