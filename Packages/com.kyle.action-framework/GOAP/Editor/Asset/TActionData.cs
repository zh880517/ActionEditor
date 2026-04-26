namespace GOAP
{
    // 快速扩展 ActionData 额外字段的泛型模板（类比 TFlowNode<T>）
    // T 是 struct，包含该 Action 类型的特有字段
    // 使用示例：
    //   public struct MeleeConfig { public float AttackRange; }
    //   [MeleeGroup] public class MeleeActionData : TActionData<MeleeConfig> { }
    public abstract class TActionData<T> : ActionData where T : struct
    {
        public T Config;
    }
}
