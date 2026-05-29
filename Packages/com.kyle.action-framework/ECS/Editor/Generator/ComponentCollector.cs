using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
namespace ECSEditor
{
    public class ComponentCollector
    {
        public static readonly string SUFFIX = "Component";
        public enum ResetType
        {
            None,
            Normal,
            Virtual,
            Override,
        }

        public class ComponentType
        {
            public Type Type;
            public Type BaseType;
            public ComponentType Base;
            public bool IsUnique;
            public bool IsFlag;
            public string Name;
            public ResetType ResetType;
            public List<FieldInfo> Fields;
            public List<ComponentType> SubClass = new List<ComponentType>();
        }

        private readonly List<ComponentType> types = new List<ComponentType>();
        private readonly List<Type> staticTypes = new List<Type>();
        public IReadOnlyList<ComponentType> Types => types;
        public IReadOnlyList<Type> StaticTypes => staticTypes;
        public int ValidCount { get; private set; }
        public int UniqueCount { get; private set; }

        public Type ContextType { get; private set; }
        public string ContextName {  get; private set; }
        public string NameSpace {  get; private set; }

        public void Collector(string nameSpcace, Type contextType, Type componentType, Type uniqueType, Type staticType)
        {
            NameSpace = nameSpcace;
            ContextType = contextType;
            if (contextType == null || componentType == null)
                return;
            ContextName = ContextType.Name;
            if (ContextName.StartsWith('I'))
            {
                ContextName = ContextName.Substring(1);
            }
            foreach (var assemble in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assemble.GetTypes())
                {
                    if (type.IsInterface || type.GetCustomAttribute<ObsoleteAttribute>() != null)
                        continue;
                    if (!ContextType.IsAssignableFrom(type))
                        continue;
                    if (staticType != null && staticType.IsAssignableFrom(type))
                    {
                        staticTypes.Add(type);
                        continue;
                    }
                    if (!componentType.IsAssignableFrom(type))
                        continue;
                    var fields = type.GetFields().Where(it => it.IsPublic && !it.IsStatic);
                    ComponentType c = new ComponentType
                    {
                        Type = type,
                        IsUnique = uniqueType != null && uniqueType.IsAssignableFrom(type),
                        Name = type.Name.EndsWith(SUFFIX) ? type.Name.Substring(0, type.Name.Length - SUFFIX.Length) : type.Name,
                        Fields = fields.Where(it => it.DeclaringType == type).ToList(),
                        IsFlag = fields.Count() == 0,
                        BaseType = componentType.IsAssignableFrom(type.BaseType) ? type.BaseType : null,
                    };
                    types.Add(c);
                }
            }
            foreach (var t in types)
            {
                if (t.BaseType != null)
                {
                    t.Base = types.Find(it => it.Type == t.BaseType);
                    t.Base?.SubClass.Add(t);
                }
            }
            foreach (var t in types)
            {
                if (t.Fields.Count > 0)
                {
                    t.ResetType = ResetType.Normal;
                    var bt = t.Base;
                    while (bt != null)
                    {
                        if (bt.Fields.Count > 0)
                        {
                            bt.ResetType = ResetType.Override;
                            break;
                        }
                        bt = bt.Base;
                    }
                    if (t.ResetType == ResetType.Normal)
                    {
                        if (SubClassHasField(t))
                        {
                            t.ResetType = ResetType.Virtual;
                        }
                    }
                }
            }
            types.Sort(Sort);
            ValidCount = types.Count(it => !it.Type.IsAbstract);
            UniqueCount = types.Count(it => !it.Type.IsAbstract && it.IsUnique);
        }

        private static int Sort(ComponentType a, ComponentType b)
        {
            if (a.IsUnique == b.IsUnique)
            {
                return a.Name.CompareTo(b.Name);
            }
            return a.IsUnique.CompareTo(b.IsUnique);
        }

        private bool SubClassHasField(ComponentType t)
        {
            if (t.SubClass.Count == 0)
                return false;
            foreach (var sub in t.SubClass)
            {
                if (sub.Fields.Count > 0)
                    return true;
                if (SubClassHasField(sub))
                    return true;
            }
            return false;
        }
    }
}