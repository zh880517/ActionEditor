using System;

namespace Flow
{
    /// <summary>
    /// 标记字段的旧名称，用于在重命名字段时保持向后兼容性。
    /// 需要在重命名前添加此属性，以便在重命名后仍能正确序列化和反序列化。
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
