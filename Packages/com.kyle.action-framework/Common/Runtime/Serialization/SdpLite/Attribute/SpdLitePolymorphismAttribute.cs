using System;
/*
 * 序列化多态支持
 * 在基类标记即可
 * 
 */

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class SpdLitePolymorphismAttribute : Attribute
{
}
