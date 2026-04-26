namespace GOAP
{
    // 泛型行动执行器基类，对应 FlowGraph 的 TFlowNodeExecutor<T, ...>
    // 负责将非泛型 IActionExecutor 接口方法拆包为强类型的 T
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
    //   public class MeleeAttackExecutor : TActionExecutor<MeleeAttackData>
    //   {
    //       public static readonly MeleeAttackExecutor Instance = new MeleeAttackExecutor();
    //
    //       protected override bool OnIsAchievable(WorldState current, MeleeAttackData data)
    //           => current.TryGet((int)MyKey.EnemyInRange, out var v) && v != 0;
    //
    //       protected override ActionStatus OnUpdate(Agent agent, MeleeAttackData data)
    //       {
    //           /* 攻击逻辑 */
    //           return ActionStatus.Completed;
    //       }
    //   }
    //
    //   // 启动时注册：
    //   ActionExecutor<MeleeAttackData>.Executor = MeleeAttackExecutor.Instance;
    //
    //   // 创建行动实例：
    //   agent.AvailableActions.Add(new Action<MeleeAttackData>(new MeleeAttackData { AttackRange = 2f, ... }));
    public abstract class TActionExecutor<T> : IActionExecutor where T : struct, IActionData
    {
        // --- IActionExecutor 实现（拆包到强类型方法）---

        public bool IsAchievable(WorldState current, IActionData data)
            => OnIsAchievable(current, (T)data);

        public void OnEnter(Agent agent, IActionData data)
            => OnEnter(agent, (T)data);

        public ActionStatus OnUpdate(Agent agent, IActionData data)
            => OnUpdate(agent, (T)data);

        public void OnExit(Agent agent, IActionData data)
            => OnExit(agent, (T)data);

        public void OnAbort(Agent agent, IActionData data)
            => OnAbort(agent, (T)data);

        // --- 子类重写点 ---

        // 运行时可行性检查，默认始终可行
        protected virtual bool OnIsAchievable(WorldState current, T data) => true;

        // 行动开始时调用一次，可重写以播放动画、初始化状态等
        protected virtual void OnEnter(Agent agent, T data) { }

        // 每帧驱动行动逻辑，必须实现
        protected abstract ActionStatus OnUpdate(Agent agent, T data);

        // 行动正常结束时调用（Completed 或 Failed 后）
        protected virtual void OnExit(Agent agent, T data) { }

        // 行动被中断时调用（重规划、目标切换）
        protected virtual void OnAbort(Agent agent, T data) { }
    }
}
