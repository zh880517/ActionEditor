using System;
/*
 * 标记是否生成序列化函数
 */
[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class SdpLiteSerializeFunctionAttribute : Attribute
{
}
