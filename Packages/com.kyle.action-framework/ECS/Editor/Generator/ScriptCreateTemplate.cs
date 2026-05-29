using CodeGen;
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace ECSEditor
{
    public class ScriptCreateTemplate
    {
        class CreateStateScriptAction : EndNameEditAction
        {
            public Func<string, string> GenFileContent;
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                string fileNameWithOutExtension = Path.GetFileNameWithoutExtension(pathName);
                File.WriteAllText(pathName, GenFileContent(fileNameWithOutExtension), Encoding.UTF8);
                AssetDatabase.ImportAsset(pathName);
                var o = AssetDatabase.LoadAssetAtPath(pathName, typeof(UnityEngine.Object));
                ProjectWindowUtil.ShowCreatedAsset(o);
            }
        }

        public static string GetSelectedPathOrFallBack()
        {
            string path = "Assets";
            foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    path = Path.GetDirectoryName(path);
                    break;
                }
            }
            return path;
        }

        public static void CreateTypeClass(System.Func<string, string> genContentFunc, string newFileName)
        {
            var action = ScriptableObject.CreateInstance<CreateStateScriptAction>();
            action.GenFileContent = genContentFunc;

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                action,
                $"{GetSelectedPathOrFallBack()}/{newFileName}.cs",
                EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D,
                ""
                );
        }

        public static string GenECSLiteComponentClass(string name, string nameSpece, string contextName)
        {
            CSharpCodeWriter writer = new CSharpCodeWriter(true);
            using (new CSharpCodeWriter.NameSpaceScop(writer, nameSpece))
            {
                writer.WriteLine($"public class {name} : I{contextName}Component");
                writer.BeginScope();
                writer.EndScope();
            }
            return writer.ToString();
        }

        public static string GenViewComponentClass(string name, string nameSpece)
        {
            CSharpCodeWriter writer = new CSharpCodeWriter(true);
            using (new CSharpCodeWriter.NameSpaceScop(writer, nameSpece))
            {
                writer.WriteLine($"public class {name} : VECS.IViewComponent");
                writer.BeginScope();
                writer.EndScope();
            }
            return writer.ToString();
        }
        public static string GenStaticViewComponentClass(string name, string nameSpece)
        {
            CSharpCodeWriter writer = new CSharpCodeWriter(true);
            using (new CSharpCodeWriter.NameSpaceScop(writer, nameSpece))
            {
                writer.WriteLine($"public class {name} : VECS.IViewStaticComponent");
                writer.BeginScope();
                writer.EndScope();
            }
            return writer.ToString();
        }
        public static string GenUniqueViewComponentClass(string name, string nameSpece)
        {
            CSharpCodeWriter writer = new CSharpCodeWriter(true);
            using (new CSharpCodeWriter.NameSpaceScop(writer, nameSpece))
            {
                writer.WriteLine($"public class {name} : VECS.IViewUniqueComponent");
                writer.BeginScope();
                writer.EndScope();
            }
            return writer.ToString();
        }

        public static string GenECSLiteSystem(string name, string nameSpace, string contextType, string baseClass, string funcName)
        {
            CSharpCodeWriter writer = new CSharpCodeWriter(true);
            using (new CSharpCodeWriter.NameSpaceScop(writer, nameSpace))
            {
                using (new CSharpCodeWriter.Scop(writer, $"public class {name} : ECSLite.{baseClass}"))
                {
                    writer.WriteLine($"private readonly {contextType} mContext;");
                    using (new CSharpCodeWriter.Scop(writer, $"public {name}({contextType} context)"))
                    {
                        writer.WriteLine("mContext = context;");
                    }
                    using (new CSharpCodeWriter.Scop(writer, $"public void {funcName}()"))
                    {
                        writer.WriteLine("/*");
                        writer.WriteLine("var group = mContext.CreatGroup<ECSLite.IComponent>();");
                        writer.WriteLine("while (group.MoveNext())");
                        using (new CSharpCodeWriter.Scop(writer))
                        {
                            writer.WriteLine("//TODO: group.Entity, group.Component");
                        }
                        writer.WriteLine("*/");
                    }
                }
            }
            return writer.ToString();
        }

        public static string GenViewSystem(string name, string nameSpace, string baseClass, string funcName)
        {
            CSharpCodeWriter writer = new CSharpCodeWriter(true);
            using (new CSharpCodeWriter.NameSpaceScop(writer, nameSpace))
            {
                using (new CSharpCodeWriter.Scop(writer, $"public class {name} : {baseClass}"))
                {
                    writer.WriteLine("private readonly VECS.ViewContext mContext;");
                    using (new CSharpCodeWriter.Scop(writer, $"public {name}(VECS.ViewContext context)"))
                    {
                        writer.WriteLine("mContext = context;");
                    }
                    using (new CSharpCodeWriter.Scop(writer, $"public void {funcName}()"))
                    {
                        writer.WriteLine("/*");
                        writer.WriteLine("var group = mContext.CreatGroup<IViewComponent>();");
                        writer.WriteLine("while (group.MoveNext())");
                        using (new CSharpCodeWriter.Scop(writer))
                        {
                            writer.WriteLine("//TODO: group.Entity, group.Component");
                        }
                        writer.WriteLine("*/");
                    }
                }
            }
            return writer.ToString();
        }

        public static string GenECSLiteGroupSystem<T>(string name, string nameSpace)
        {
            Type t = typeof(T);
            string contextName = t.Name;
            if (contextName.StartsWith('I'))
                contextName = contextName.Substring(1);

            CSharpCodeWriter writer = new CSharpCodeWriter(true);
            using (new CSharpCodeWriter.NameSpaceScop(writer, nameSpace))
            {
                writer.WriteLine($"using TComponent = I{contextName}Component;");
                using (new CSharpCodeWriter.Scop(writer, $"public class {name} : ECSLite.GroupUpdateSystemT<{contextName}Context, {t.Name}, TComponent>"))
                {
                    writer.WriteLine($"public {name}({contextName}Context context) : base(context){{}}");
                    using (new CSharpCodeWriter.Scop(writer, $"protected override void OnExecuteEntity(Entity<{t.Name}> entity, TComponent component)"))
                    {
                    }
                }
            }
            return writer.ToString();
        }

        public static string GenViewGroupSystem(string name, string nameSpace, bool isReactive, bool isLateUpdate)
        {
            CSharpCodeWriter writer = new CSharpCodeWriter(true);
            using (new CSharpCodeWriter.NameSpaceScop(writer, nameSpace))
            {
                string baseClass = "GroupUpdateSystem";
                if (isReactive)
                {
                    baseClass = isLateUpdate ? "ReactiveLateUpdateSystem" : "ReactiveUpdateSystem";
                }
                else if (isLateUpdate)
                {
                    baseClass = "GroupLateUpdateSystem";
                }
                using (new CSharpCodeWriter.Scop(writer, $"public class {name} : VECS.{baseClass}<IViewComponent>"))
                {
                    writer.WriteLine($"public {name}(VECS.ViewContext context) : base(context){{}}");
                    using (new CSharpCodeWriter.Scop(writer, "protected override void OnExecuteEntity(VECS.ViewEntity entity, VECS.IViewComponent component)"))
                    {
                    }
                }
            }
            return writer.ToString();
        }
    }
}
