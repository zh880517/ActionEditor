using System;
/*
 * 标记需要多态支持的字段
 * 需要类型基类标记 SpdLitePolymorphismAttribute
 */

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class SdpLiteDynamicFieldAttribute : Attribute
{
}
