using System;

namespace Flow
{
    /// <summary>
    //使用 [UnityEngine.FormerlySerializedAs] 替代，用于重命名字段时保持序列化数据的兼容性
    //后续删除当前属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public class FlowFormerlyNameAttribute : Attribute
    {
        public string OldName { get; private set; }
        public FlowFormerlyNameAttribute(string oldName)
        {
            OldName = oldName;
        }
    }
}
