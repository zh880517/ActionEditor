using CodeGen;

namespace ECSEditor
{
    public static class ViewECSInitGenerator
    {
        public static void Gen(string path, ComponentCollector collector)
        {
            var writer = new CSharpCodeWriter();
            writer.WriteLine("//工具生成，手动修改无效");
            using (new CSharpCodeWriter.NameSpaceScop(writer, collector.NameSpace))
            {
                using (new CSharpCodeWriter.Scop(writer, "public class ViewECS"))
                {
                    writer.WriteLine($"private static int s_ComponentCount = {collector.ValidCount - collector.UniqueCount};");
                    writer.WriteLine($"private static int s_UniqueComponentCount = {collector.UniqueCount};");
                    writer.WriteLine($"private static int s_StatcicComponentCount = {collector.StaticTypes.Count};");
                    writer.WriteLine("public static int ComponentCount => s_ComponentCount;");
                    writer.WriteLine("public static int StaticComponentCount => s_StatcicComponentCount;");

                    WriteId(writer, collector);
                    using (new CSharpCodeWriter.Scop(writer, "public static VECS.ViewContext CreateContext()"))
                    {
                        writer.WriteLine("VECS.ViewContext context = new VECS.ViewContext(s_ComponentCount, s_UniqueComponentCount, s_StatcicComponentCount);");
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
            using (new CSharpCodeWriter.Scop(writer, "static ViewECS()"))
            {

                for (int i = 0; i < collector.StaticTypes.Count; ++i)
                {
                    var type = collector.StaticTypes[i];
                    writer.WriteLine($"VECS.ViewStaticComponentIdentity<{type.Name}>.Id = {i};");
                }
                int id = 0;
                foreach (var ty in collector.Types)
                {
                    if (ty.Type.IsAbstract)
                        continue;
                    writer.WriteLine($"VECS.ViewComponentIdentity<{ty.Type.Name}>.Id = {id++};");
                    if (ty.IsUnique)
                        writer.WriteLine($"VECS.ViewComponentIdentity<{ty.Type.Name}>.Unique = true;");
                }
                writer.WriteLine("ViewComponentClearup.Init();");
            }
        }
    }
}
