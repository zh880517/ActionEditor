using System;

namespace EasyConfig
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public abstract class ConfigGroupAttribute : Attribute
    {
        public abstract string GeneratePath { get; }
        public abstract string Namespace { get; }
        public abstract string ClassName { get; }
        public abstract string ExportSubFolder { get; }
    }
}
