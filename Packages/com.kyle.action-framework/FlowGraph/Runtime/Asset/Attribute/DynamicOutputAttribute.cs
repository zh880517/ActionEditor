using System;

namespace Flow
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class DynamicOutputAttribute : Attribute
    {
    }
}
