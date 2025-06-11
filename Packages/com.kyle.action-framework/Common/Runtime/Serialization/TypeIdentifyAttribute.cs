using System;
//类型标识符：字符串，通过Guid.NewGuid().ToString()生成
//主要用来防止类重名后丢失已经序列化的类型信息
[System.Diagnostics.Conditional("UNITY_EDITOR")]
public class TypeIdentifyAttribute : Attribute
{
    public string GUID { get; private set; }
    public TypeIdentifyAttribute(string guid)
    {
        GUID = guid;
    }
}