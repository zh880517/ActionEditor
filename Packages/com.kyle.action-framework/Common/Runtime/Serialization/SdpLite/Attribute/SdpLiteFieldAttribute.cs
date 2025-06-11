using System;
/*索引从1开始，0留给基类用
*  class{ 
*      0 base{ 
*          0 base{ 
*              1, 
*          }, 
*          1, 
*          2
*      }, 
*      1, 
*      2 
*  }
*  因为主要是用来序列化到文件的，所以用过的索引即便是删掉了也不要重用
*/
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class SdpLiteFieldAttribute : Attribute
{
    public uint FieldIndex { get; private set; }
    public SdpLiteFieldAttribute(uint index)
    {
        FieldIndex = index;
    }
}