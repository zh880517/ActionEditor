using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

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

            foreach (var catalog in catalogs)
            {
                DataVisitCodeGenerator.GenerateCatalog(catalog);
            }

            AssetDatabase.Refresh();
        }
        public static void GenerateCatalog(CatalogData catalog)
        {
            var writer = new CSharpCodeWriter(true);
            var nameSpace = catalog.CatalogAttribute.NameSpace;
            var catalogName = catalog.CatalogAttribute.CatalogName;
            
            writer.WriteLine("using System.Collections.Generic;");
            writer.WriteLine("using DataVisit;");
            
            using (new CSharpCodeWriter.NameSpaceScop(writer, nameSpace))
            {
                GenerateVisitClass(writer, catalog, catalogName);
                writer.NewLine();
                
                var dynamicTypes = DataVisitTypeCollector.CollectDynamicTypes(catalog);
                if (dynamicTypes.Count > 0)
                {
                    GenerateDynamicTypeRegister(writer, dynamicTypes, catalogName, nameSpace);
                    writer.NewLine();
                }
                
                GenerateInitClass(writer, catalog, catalogName, nameSpace, dynamicTypes);
            }
            
            string filePath = Path.Combine(catalog.CatalogAttribute.GeneratePath, $"{catalogName}Visit.cs");
            GeneratorUtils.WriteToFile(filePath, writer.ToString());
            
            Debug.Log($"Generated: {filePath}");
        }

        private static void GenerateVisitClass(CSharpCodeWriter writer, CatalogData catalog, string catalogName)
        {
            using (new CSharpCodeWriter.Scop(writer, $"public static class {catalogName}Visit"))
            {
                foreach (var typeInfo in catalog.Types)
                {
                    GenerateVisitMethod(writer, typeInfo, catalog.CatalogAttribute.NameSpace);
                    writer.NewLine();
                }
            }
        }

        private static void GenerateVisitMethod(CSharpCodeWriter writer, TypeData typeInfo, string nameSpace)
        {
            string typeName = GeneratorUtils.TypeToName(typeInfo.Type, nameSpace);
            string methodName = $"Visit{typeInfo.TypeName}";
            string signature = $"public static void {methodName}(IVisitier visitier, uint tag, string name, uint flag, ref {typeName} value)";
            
            using (new CSharpCodeWriter.Scop(writer, signature))
            {
                if(typeInfo.Base != null)
                {
                    string baseTypeName = GeneratorUtils.TypeToName(typeInfo.Base.Type, nameSpace);
                    writer.WriteLine($"var _base = ({baseTypeName})value");
                    writer.WriteLine($"TypeVisit<{baseTypeName}>.Visit(visitier, 0, \"\", 0, ref _base);");
                }
                foreach (var field in typeInfo.Fields)
                {
                    GenerateFieldVisit(writer, field, nameSpace);
                }
            }
        }

        private static void GenerateFieldVisit(CSharpCodeWriter writer, FieldData field, string nameSpace)
        {
            string fieldAccess = $"value.{field.FieldName}";
            uint tag = field.Tag;
            string visitMethod = GetVisitMethodName(field.FieldType, field.IsDynamic);
            
            writer.WriteLine($"visitier.{visitMethod}({tag}, nameof({field.FieldName}), {field.Tag}, ref {fieldAccess});");
        }

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

        private static void GenerateDynamicTypeRegister(CSharpCodeWriter writer, List<DynamicTypeInfo> dynamicTypes, string catalogName, string nameSpace)
        {
            using (new CSharpCodeWriter.Scop(writer, $"public static class {catalogName}DynamicTypeRegister"))
            {
                foreach (var dynamicType in dynamicTypes)
                {
                    GenerateDynamicTypeEnum(writer, dynamicType, nameSpace);
                    writer.NewLine();
                    GenerateDynamicTypeRegisterMethod(writer, dynamicType, nameSpace);
                    writer.NewLine();
                }
            }
        }

        private static void GenerateDynamicTypeEnum(CSharpCodeWriter writer, DynamicTypeInfo dynamicType, string nameSpace)
        {
            string baseTypeName = GeneratorUtils.TypeToName(dynamicType.BaseType, nameSpace);
            string enumName = $"{dynamicType.BaseType.Name}TypeID";
            
            using (new CSharpCodeWriter.Scop(writer, $"public enum {enumName}"))
            {
                writer.WriteLine($"[VisitTypeTag(typeof({baseTypeName}))]");
                writer.WriteLine($"{dynamicType.BaseType.Name} = 0,");
                
                foreach (var child in dynamicType.ChildTypes)
                {
                    string childTypeName = GeneratorUtils.TypeToName(child.Type, nameSpace);
                    writer.WriteLine($"[VisitTypeTag(typeof({childTypeName}))]");
                    writer.WriteLine($"{child.Type.Name} = {child.TypeId},");
                }
            }
        }

        private static void GenerateDynamicTypeRegisterMethod(CSharpCodeWriter writer, DynamicTypeInfo dynamicType, string nameSpace)
        {
            string baseTypeName = GeneratorUtils.TypeToName(dynamicType.BaseType, nameSpace);
            string methodName = $"Register{dynamicType.BaseType.Name}Types";
            
            using (new CSharpCodeWriter.Scop(writer, $"public static void {methodName}()"))
            {
                foreach (var child in dynamicType.ChildTypes)
                {
                    string childTypeName = GeneratorUtils.TypeToName(child.Type, nameSpace);
                    writer.WriteLine($"DynamicTypeVisit<{baseTypeName}>.RegisterType<{childTypeName}>({child.TypeId});");
                }
            }
        }

        private static void GenerateInitClass(CSharpCodeWriter writer, CatalogData catalog, string catalogName, string nameSpace, List<DynamicTypeInfo> dynamicTypes)
        {
            using (new CSharpCodeWriter.Scop(writer, $"public static class {catalogName}VisitInit"))
            {
                writer.WriteLine("private static bool _isInit = false;");
                writer.NewLine();
                
                using (new CSharpCodeWriter.Scop(writer, "public static void Init()"))
                {
                    writer.WriteLine("if (_isInit) return;");
                    writer.WriteLine("_isInit = true;");
                    writer.NewLine();
                    
                    foreach (var typeInfo in catalog.Types)
                    {
                        string typeName = GeneratorUtils.TypeToName(typeInfo.Type, nameSpace);
                        string methodName = $"{catalogName}Visit.Visit{typeInfo.TypeName}";
                        
                        writer.WriteLine($"TypeVisit<{typeName}>.VisitFunc = {methodName};");
                        writer.WriteLine($"TypeVisit<{typeName}>.IsCustomStruct = {(typeInfo.IsStruct ? "true" : "false")};");
                        
                        if (!typeInfo.IsStruct)
                        {
                            writer.WriteLine($"TypeVisit<{typeName}>.New = () => new {typeName}();");
                        }
                        
                        writer.NewLine();
                    }
                    
                    if (dynamicTypes.Count > 0)
                    {
                        foreach (var dynamicType in dynamicTypes)
                        {
                            string methodName = $"{catalogName}DynamicTypeRegister.Register{dynamicType.BaseType.Name}Types";
                            writer.WriteLine($"{methodName}();");
                        }
                    }
                }
            }
        }
    }
}
