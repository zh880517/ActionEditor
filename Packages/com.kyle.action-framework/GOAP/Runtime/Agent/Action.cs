namespace GOAP
{
    // 泛型行动基类，类似 FlowGraph 的 TFlowNode<T> 模式
    // T 是描述行动数据的 struct（实现 IActionData），包含 Id、Cost、Preconditions、Effects
    // 子类只需重写行为方法，无需重复实现数据属性
    //
    // 用法示例：
    //   public struct MeleeAttackData : IActionData
    //   {
    //       public string Id => "MeleeAttack";
    //       public float Cost => 1f;
    //       public WorldState Preconditions { get; set; }
    //       public WorldState Effects { get; set; }
    //       public float AttackRange;
    //   }
    //
    //   public class MeleeAttackAction : Action<MeleeAttackData>
    //   {
    //       public MeleeAttackAction(MeleeAttackData data) : base(data) { }
    //       protected override bool OnIsAchievable(WorldState current) => current.TryGet<bool>("enemyInRange", out var v) && v;
    //       protected override ActionStatus OnPerform(Agent agent) { /* 攻击逻辑 */ return ActionStatus.Completed; }
    //   }
    public abstract class Action<T> : IAction where T : IActionData
    {
        // 行动数据实例，子类可通过 Data 访问自定义字段
        protected T Data { get; }

        protected Action(T data)
        {
            Data = data;
        }

        // --- IAction 实现（代理到 Data）---

        public string Id => Data.Id;
        public float Cost => Data.Cost;
        public WorldState Preconditions => Data.Preconditions;
        public WorldState Effects => Data.Effects;

        public bool IsAchievable(WorldState current) => OnIsAchievable(current);
        public ActionStatus Perform(Agent agent) => OnPerform(agent);
        public void Abort(Agent agent) => OnAbort(agent);

        // --- 子类重写点 ---

        // 运行时额外可行性检查（如距离、冷却等动态条件）
        // 默认实现：始终可行，子类可增加动态检查
        protected virtual bool OnIsAchievable(WorldState current) => true;

        // 执行行动逻辑，每帧调用直到返回 Completed 或 Failed
        protected abstract ActionStatus OnPerform(Agent agent);

        // 中断行动，清理内部状态
        protected virtual void OnAbort(Agent agent) { }
    }
}
