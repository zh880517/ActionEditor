using System;
using System.Collections.Generic;

namespace EasyConfig.Editor
{
    public static class EditorExcelConfigReloadDispatcher
    {
        private static readonly HashSet<Type> registeredTypes = new HashSet<Type>();

        public static IReadOnlyCollection<Type> RegisteredConfigTypes => registeredTypes;

        public static void RegisterConfigType(Type type)
        {
            if (type == null)
                return;
            registeredTypes.Add(type);
        }

        internal static void ClearForTests()
        {
            registeredTypes.Clear();
        }
    }
}
