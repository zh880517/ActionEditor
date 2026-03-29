namespace Flow
{
    /// <summary>
    /// 通过名字获取FlowGraphRuntimeData的抽象提供者。
    /// 用户需要实现子类并赋值给Instance。
    /// </summary>
    public abstract class FlowSubGraphProvider
    {
        public static FlowSubGraphProvider Instance { get; set; }

        public abstract FlowGraphRuntimeData GetSubGraphData(string name);

        public static FlowGraphRuntimeData Get(string name)
        {
            return Instance?.GetSubGraphData(name);
        }
    }
}
