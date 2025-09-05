using System;

/// <summary>
/// 用于在父节点中展开显示该字段的属性，不显示折叠箭头
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public class ExpandedInParentAttribute : Attribute
{
}