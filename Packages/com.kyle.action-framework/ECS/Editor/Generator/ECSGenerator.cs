using System.IO;
using UnityEditor;

namespace ECSEditor
{
    public static class ECSGenerator
    {

        public static void ECSLiteGen<TContext>(string nameSpace, string path, System.Func<System.Type, string, string> customReset)
        {
            ComponentCollector collector = new ComponentCollector();
            string contextName = typeof(TContext).Name;
            if (contextName.StartsWith('I'))
                contextName = contextName.Substring(1);
            string componentName = $"I{contextName}Component";
            string uniqueComponentName = $"I{contextName}UniqueComponent";
            string staticComponentName = $"I{contextName}StaticComponent";
            collector.Collector(nameSpace, typeof(TContext), 
                FindComponentType(typeof(TContext), componentName),
                FindComponentType(typeof(TContext), uniqueComponentName),
                FindComponentType(typeof(TContext), staticComponentName)
                );
            ECSLiteContextGenerator.Gen(Path.Combine(path, $"{collector.ContextName}Context.cs"), collector);
            ECSLiteInitGenerator.Gen(Path.Combine(path, $"{collector.ContextName}ECS.cs"), collector);
            ECSLiteResetGenerator.Gen(Path.Combine(path, $"{collector.ContextName}ComponentReset.cs"), collector, customReset);
            ECSLiteClearupGenerator.Gen(Path.Combine(path, $"{collector.ContextName}ComponentReset_Init.cs"), collector);

            AssetDatabase.Refresh();
        }

        public static void ViewECSGen(string nameSpace, string path, System.Func<System.Type, string, string> customReset)
        {
            ComponentCollector collector = new ComponentCollector();
            collector.Collector(nameSpace, typeof(VECS.IView), typeof(VECS.IViewComponent), typeof(VECS.IViewUniqueComponent), typeof(VECS.IViewStaticComponent));
            ViewECSInitGenerator.Gen(Path.Combine(path, $"{collector.ContextName}ECS.cs"), collector);
            ViewECSResetGenerator.Gen(Path.Combine(path, $"{collector.ContextName}ComponentClearup_Reset.cs"), collector, customReset);
            ViewECSClearupGenerator.Gen(Path.Combine(path, $"{collector.ContextName}ComponentClearup_Init.cs"), collector);
            AssetDatabase.Refresh();
        }

        public static System.Type FindComponentType(System.Type contextType, string name)
        {
            string nameSpace = contextType.Namespace;
            if (nameSpace != null)
            {
                name = $"{nameSpace}.{name}";
            }
            var t = contextType.Assembly.GetType(name);
            if (t == null)
            {
                foreach (var assemble in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    t = assemble.GetType(name);
                    if (t != null)
                    {
                        if (!contextType.IsAssignableFrom(t))
                        {
                            t = null;
                            continue;
                        }
                        break;
                    }
                }
            }
            return t;
        }
    }

}