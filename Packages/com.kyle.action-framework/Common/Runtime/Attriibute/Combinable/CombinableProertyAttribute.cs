using System;
[System.Diagnostics.Conditional("UNITY_EDITOR")]
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
public class CombinableProertyAttribute : Attribute
{
}