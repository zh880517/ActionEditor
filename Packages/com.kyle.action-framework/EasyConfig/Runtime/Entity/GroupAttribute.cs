using System;
using System.Text.RegularExpressions;

namespace EasyConfig
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class GroupAttribute : Attribute
    {
        public string Name { get; }
        public int Order { get; }

        public GroupAttribute() : this(0) { }

        public GroupAttribute(int order)
        {
            Name = DeriveName();
            Order = order;
        }

        private string DeriveName()
        {
            var type = GetType();
            var name = type.Name;
            if (name.EndsWith("Group", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - 5);
            return name;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class EntityTagAttribute : Attribute
    {
        public Type[] GroupTypes { get; }

        public EntityTagAttribute(params Type[] groupTypes)
        {
            GroupTypes = groupTypes;
        }
    }
}
