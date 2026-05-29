using CodeGen;

namespace ECSEditor
{
    public static class ECSLiteContextGenerator
    {
        public static void Gen(string path, ComponentCollector collector)
        {
            var writer = new CSharpCodeWriter();
            writer.WriteLine("//工具生成，手动修改无效");
            using (new CSharpCodeWriter.NameSpaceScop(writer, collector.NameSpace))
            {
                writer.WriteLine($"public interface I{collector.ContextName}Component : {collector.ContextType.Name}, ECSLite.IComponent{{}}");
                writer.WriteLine($"public interface I{collector.ContextName}UniqueComponent : {collector.ContextType.Name}, I{collector.ContextName}Component, ECSLite.IUniqueComponent{{}}");
                writer.WriteLine($"public interface I{collector.ContextName}StaticComponent : {collector.ContextType.Name}, ECSLite.IStaticComponent{{}}");
                writer.NewLine();
                using (new CSharpCodeWriter.Scop(writer, $"public class {collector.ContextName}Context : ECSLite.ContextT<{collector.ContextType.Name}>"))
                {
                    using (new CSharpCodeWriter.Scop(writer, $"public {collector.ContextName}Context(int componentCount, int uniqueCount, int staticComponentCount) : base(componentCount, uniqueCount, staticComponentCount)"))
                    {
                    }
                }
            }
            GeneratorUtils.WriteToFile(path, writer.ToString());
        }
    }
}
