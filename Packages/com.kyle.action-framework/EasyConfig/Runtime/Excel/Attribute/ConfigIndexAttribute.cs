using System;
namespace EasyConfig
{
    [AttributeUsage(AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
    public class ConfigIndexAttribute : Attribute
    {
    }
}