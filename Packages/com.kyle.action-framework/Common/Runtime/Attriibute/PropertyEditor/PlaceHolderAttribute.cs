using System;
/// <summary>
/// 占位符属性,用于在Flow编辑器中做不可编辑的数据输入端口用
/// </summary>
[AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = true)]
public class PlaceHolderAttribute : Attribute
{
}