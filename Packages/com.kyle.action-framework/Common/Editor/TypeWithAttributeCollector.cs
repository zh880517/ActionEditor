using System;
using System.Collections.Generic;
using System.Reflection;

public static class TypeWithAttributeCollector<TBaseType, TAttribute> where TBaseType : class where TAttribute : Attribute
{
    private static Dictionary<Type, TAttribute> types;
    public static Dictionary<Type, TAttribute> Types
    {
        get
        {
            if (types == null)
            {
                types = Collector();
            }
            return types;
        }
    }

    public static Dictionary<Type, TAttribute> Collector()
    {
        var types = new Dictionary<Type, TAttribute>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            var assemblyTypes = assembly.GetTypes();
            foreach (var type in assemblyTypes)
            {
                if (!type.IsAbstract && !type.IsInterface && typeof(TBaseType).IsAssignableFrom(type))
                {
                    var attribute = type.GetCustomAttribute<TAttribute>(false);
                    if (attribute != null)
                    {
                        types.Add(type, attribute);
                    }
                }
            }
        }
        return types;
    }
}
