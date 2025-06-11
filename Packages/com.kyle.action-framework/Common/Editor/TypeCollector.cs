using System;
using System.Collections.Generic;

public static class TypeCollector<TBaseType> where TBaseType : class
{
    private static List<Type> types;
    public static List<Type> Types
    {
        get
        {
            if (types == null)
            {
                types = new List<Type>();
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    var assemblyTypes = assembly.GetTypes();
                    foreach (var type in assemblyTypes)
                    {
                        if (!type.IsAbstract && !type.IsInterface && typeof(TBaseType).IsAssignableFrom(type))
                        {
                            types.Add(type);
                        }
                    }
                }
            }
            return types;
        }
    }
}
