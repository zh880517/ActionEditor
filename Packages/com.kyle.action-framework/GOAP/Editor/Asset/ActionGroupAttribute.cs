using System;

namespace GOAP.EditorView
{
    // 分组基类 Attribute，继承后以子类类名（去掉末尾 "Group"）作为分组名
    // 标注在 ActionData 子类上，声明该类型属于哪个分组
    // 使用示例：
    //   public class MeleeGroup : ActionGroupAttribute { }   // Name = "Melee"
    //   [MeleeGroup] public class MeleeActionData : ActionData { ... }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ActionGroupAttribute : Attribute
    {
        public string Name { get; }
        public int Order { get; }

        public ActionGroupAttribute() : this(0) { }
        public ActionGroupAttribute(int order)
        {
            var name = GetType().Name;
            if (name.EndsWith("Group", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - 5);
            Name = name;
            Order = order;
        }
    }

    // 标注在 ConfigAsset 子类上，声明允许添加的 ActionData 分组类型
    // 使用示例：
    //   [GOAPTag(typeof(MeleeGroup), typeof(RangedGroup))]
    //   [CreateAssetMenu(menuName = "GOAP/WarriorConfig")]
    //   public class WarriorConfig : ConfigAsset { }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class GOAPTagAttribute : Attribute
    {
        public Type[] GroupTypes { get; }
        public GOAPTagAttribute(params Type[] groupTypes) { GroupTypes = groupTypes; }
    }
}
