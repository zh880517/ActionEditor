namespace GOAP
{
    // 泛型目标基类，与 Action<T> 对称
    // T 是描述目标数据的 struct（实现 IGoalData）
    // 子类重写 OnGetPriority / OnIsValid 注入动态逻辑
    //
    // 用法示例：
    //   public struct KillEnemyData : IGoalData
    //   {
    //       public string Id => "KillEnemy";
    //       public float BasePriority => 80f;
    //       public WorldState DesiredState { get; set; }
    //   }
    //
    //   public class KillEnemyGoal : Goal<KillEnemyData>
    //   {
    //       public KillEnemyGoal(KillEnemyData data) : base(data) { }
    //       protected override float OnGetPriority(WorldState current)
    //           => current.TryGet((int)MyKey.EnemyVisible, out var v) && v != 0 ? Data.BasePriority : 0f;
    //   }
    public abstract class Goal<T> : IGoal where T : IGoalData
    {
        // 目标数据实例，子类可通过 Data 访问自定义字段
        protected T Data { get; }

        protected Goal(T data)
        {
            Data = data;
        }

        // --- IGoal 实现 ---

        public string Id => Data.Id;
        public WorldState GetDesiredState() => Data.DesiredState;
        public float GetPriority(WorldState current) => OnGetPriority(current);
        public bool IsValid(WorldState current) => OnIsValid(current);

        public float InsistenceBias => OnGetInsistenceBias();

        // --- 子类重写点 ---

        // 动态优先级，默认返回 BasePriority
        protected virtual float OnGetPriority(WorldState current) => Data.BasePriority;

        // 是否有效，默认始终有效
        protected virtual bool OnIsValid(WorldState current) => true;

        // 滘后切换閘值，默认 0（无滘后效果）
        protected virtual float OnGetInsistenceBias() => 0f;
    }
}
