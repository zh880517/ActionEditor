using System;
[System.Diagnostics.Conditional("UNITY_EDITOR")]
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
public class CombinableProertyAttribute : Attribute
{
}