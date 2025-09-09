using System;

namespace Flow
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class DynamicOutputAttribute : HiddenInPropertyEditor
    {
    }
}
