using CodeGen;
using System.Collections.Generic;
using System.Reflection;

namespace ECSEditor
{
    public static class ECSLiteResetGenerator
    {
        public static void Gen(string path, ComponentCollector collector, System.Func<System.Type, string, string> customReset)
        {
            var writer = new CSharpCodeWriter();
            writer.WriteLine("//工具生成，手动修改无效");
            using (new CSharpCodeWriter.NameSpaceScop(writer, collector.NameSpace))
            {
                using (new CSharpCodeWriter.Scop(writer, $"public partial class {collector.ContextName}ComponentReset"))
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
                            WriteReset(writer, ty.Fields, customReset);
                        }
                    }
                }
            }
            GeneratorUtils.WriteToFile(path, writer.ToString());
        }

        private static void WriteReset(CSharpCodeWriter writer, List<FieldInfo> fields, System.Func<System.Type, string, string> customReset)
        {
            foreach (var field in fields)
            {
                if (customReset!= null)
                {
                    string v = customReset(field.FieldType, field.Name);
                    if (!string.IsNullOrEmpty(v))
                    {
                        writer.WriteLine(v);
                        continue;
                    }
                }
                if (WriteReset(writer, field, "Clear"))
                    continue;
                if (WriteReset(writer, field, "Reset()"))
                    continue;
                if (field.FieldType.Name.Contains("Quaternion") && field.FieldType.IsValueType)
                {
                    writer.WriteLine($"value.{field.Name} = {field.FieldType.FullName}.identity;");
                    continue;
                }
                writer.WriteLine($"value.{field.Name} = default;");
            }
        }

        private static bool WriteReset(CSharpCodeWriter writer, FieldInfo field, string funcName)
        {
            var method = field.FieldType.GetMethod(funcName);
            if (method != null && method.IsPublic && !method.IsStatic && method.GetParameters().Length == 0)
            {
                if (field.FieldType.IsValueType)
                {
                    writer.WriteLine($"value.{field.Name}.{funcName}();");
                }
                else
                {
                    writer.WriteLine($"value.{field.Name}?.{funcName}();");
                }
                return true;
            }
            return false;
        }

    }
}
