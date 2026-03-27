using System;
namespace EasyConfig
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public abstract class NameIndexAttribute : Attribute
    {
    }
}