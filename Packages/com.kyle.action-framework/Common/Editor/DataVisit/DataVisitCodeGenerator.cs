using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace CodeGen.DataVisit
{
    public static class DataVisitCodeGenerator
    {
        [MenuItem("Tools/DataVisit/Generate All", priority = 100)]
        public static void GenerateAll()
        {
            var catalogs = DataVisitTypeCollector.CollectAllCatalogs();

            if (catalogs.Count == 0)
            {
                return;
            }
            HashSet<string> modifyFiles = new HashSet<string>();
            foreach (var catalog in catalogs)
            {
                if(catalog.Types.Count == 0)
                    continue;
                GenerateCatalog(catalog, modifyFiles);
            }

            if(modifyFiles.Count > 0) 
                AssetDatabase.Refresh();
        }
        private static void GenerateCatalog(CatalogData catalog, HashSet<string> modifyFils)
        {
            string funcFilePath = GenerateVisitClass(catalog);
            if (funcFilePath != null)
                modifyFils?.Add(funcFilePath);
            string registerFilePath = GenerateVisistRegisterClass(catalog);
            if (registerFilePath != null)
                modifyFils?.Add(registerFilePath);
            
        }

        //生成Visit函数，单独一个文件
        private static string GenerateVisitClass(CatalogData catalog)
        {
            CSharpCodeWriter writer = new CSharpCodeWriter();
            writer.WriteLine("using DataVisit;");
            writer.WriteLine("//Generate by tools : DataVisitCodeGenerator.GenerateAll()");
            using (new CSharpCodeWriter.NameSpaceScop(writer, catalog.NameSpace))
            {
                using (new CSharpCodeWriter.Scop(writer, $"public partial class {catalog.GenTypeName}"))
                {
                    foreach (var typeInfo in catalog.Types)
                    {
                        GenerateVisitMethod(writer, typeInfo, catalog.NameSpace);
                        writer.NewLine();
                    }
                }
            }
            string filePath = Path.Combine(catalog.GeneratePath, $"{catalog.GenTypeName}_Func.cs");
            if (GeneratorUtils.WriteToFile(filePath, writer.ToString()))
                return filePath;
            return null;
        }

        //生成Visit方法
        private static void GenerateVisitMethod(CSharpCodeWriter writer, TypeData typeInfo, string nameSpace)
        {
            string typeName = GeneratorUtils.TypeToName(typeInfo.Type, nameSpace);
            string signature = $"private static void Visit{typeInfo.TypeName}(IVisitier visitier, uint tag, string name, uint flag, ref {typeName} value)";
            
            using (new CSharpCodeWriter.Scop(writer, signature))
            {
                if(typeInfo.Base != null)
                {
                    string baseTypeName = GeneratorUtils.TypeToName(typeInfo.Base.Type, nameSpace);
                    writer.WriteLine($"var _base = ({baseTypeName})value;");
                    writer.WriteLine($"visitier.VisitClass(0, \"\", 0, ref _base);");
                }
                foreach (var field in typeInfo.Fields)
                {
                    GenerateFieldVisit(writer, field);
                }
            }
        }
        //生成字段访问代码
        private static void GenerateFieldVisit(CSharpCodeWriter writer, FieldData field)
        {
            string visitMethod = GetVisitMethodName(field.FieldType, field.IsDynamic);   
            writer.WriteLine($"visitier.{visitMethod}({field.FieldIndex}, nameof(value.{field.FieldName}), {field.Tag}, ref value.{field.FieldName});");
        }
        //根据字段类型获取访问方法名
        private static string GetVisitMethodName(Type fieldType, bool isDynamic)
        {
            if (fieldType == typeof(bool)) return "Visit";
            if (fieldType == typeof(byte)) return "Visit";
            if (fieldType == typeof(sbyte)) return "Visit";
            if (fieldType == typeof(short)) return "Visit";
            if (fieldType == typeof(ushort)) return "Visit";
            if (fieldType == typeof(int)) return "Visit";
            if (fieldType == typeof(uint)) return "Visit";
            if (fieldType == typeof(long)) return "Visit";
            if (fieldType == typeof(ulong)) return "Visit";
            if (fieldType == typeof(float)) return "Visit";
            if (fieldType == typeof(double)) return "Visit";
            if (fieldType == typeof(string)) return "Visit";
            if (fieldType == typeof(bool[])) return "Visit";
            if (fieldType == typeof(byte[])) return "Visit";
            if (fieldType == typeof(sbyte[])) return "Visit";
            if (fieldType.IsEnum) return "VisitEnum";
            
            if (fieldType.IsGenericType)
            {
                var genericDef = fieldType.GetGenericTypeDefinition();
                if (genericDef == typeof(List<>)) return isDynamic ? "VisitDynamicList" : "VisitList";
                if (genericDef == typeof(Dictionary<,>)) return isDynamic ? "VisitDynamicDictionary" : "VisitDictionary";
                if (genericDef == typeof(HashSet<>)) return "VisitHashSet";
            }
            
            if (fieldType.IsArray) return isDynamic ? "VisitDynamicArray" : "VisitArray";
            if (fieldType.IsValueType) return "VisitStruct";
            if (fieldType.IsClass) return isDynamic ? "VisitDynamicClass" : "VisitClass";
            
            return "Visit";
        }

        //生成访问注册类，单独一个文件
        private static string GenerateVisistRegisterClass(CatalogData catalog)
        {
            CSharpCodeWriter writer = new CSharpCodeWriter();
            writer.WriteLine("using DataVisit;");
            writer.WriteLine("//Generate by tools : DataVisitCodeGenerator.GenerateAll()");
            using (new CSharpCodeWriter.NameSpaceScop(writer, catalog.NameSpace))
            {
                using (new CSharpCodeWriter.Scop(writer, $"public partial class {catalog.GenTypeName}"))
                {
                    //生成TypeID枚举
                    //生成VisitTypeIDCatalog特性,方便再次生成是查找
                    writer.WriteLine($"[VisitTypeIDCatalog(typeof({GeneratorUtils.TypeToName(catalog.AttributeType, catalog.NameSpace)}))]");
                    using (new CSharpCodeWriter.Scop(writer, "public enum TypeID"))
                    {
                        //按值排序，防止生成时顺序变化造成代码差异
                        var sorted = catalog.TypeIDs.OrderBy(it => it.Value);
                        foreach (var kv in sorted)
                        {
                            string typeName = GeneratorUtils.TypeToName(kv.Key, catalog.NameSpace);
                            writer.WriteLine($"[VisitTypeTag(typeof({typeName}))]");
                            writer.WriteLine($"{kv.Key.Name} = 0x{kv.Value:x},");//十六进制表示,方便查看
                        }
                    }
                    writer.WriteLine($"private static bool isInit = false;");
                    using (new CSharpCodeWriter.Scop(writer, "public static void Init()"))
                    {
                        writer.WriteLine($"if(isInit)return;");
                        writer.WriteLine($"isInit = true;");
                        foreach (var typeInfo in catalog.Types)
                        {
                            string typeName = GeneratorUtils.TypeToName(typeInfo.Type, catalog.NameSpace);
                            if (typeInfo.IsStruct)
                            {
                                writer.WriteLine($"TypeVisitT<{typeName}>.RegusterStruct(Visit({typeInfo.TypeName}));");
                            }
                            else
                            {
                                writer.WriteLine($"TypeVisitClassT<{typeName}>.Register((int)TypeID.{typeInfo.TypeName}, Visit{typeInfo.TypeName});");
                            }
                        }
                    }
                }
            }
            string filePath = Path.Combine(catalog.GeneratePath, $"{catalog.GenTypeName}.cs");
            if (GeneratorUtils.WriteToFile(filePath, writer.ToString()))
                return filePath;
            return null;
        }
    }
}
