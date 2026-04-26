namespace GOAP
{
    // 每个行动数据类型 T 对应一个静态 Executor 注册槽，对应 FlowGraph 的 FlowNodeExecutor<T>
    // 用法：ActionExecutor<MyData>.Executor = new MyDataExecutor();
    public static class ActionExecutor<T> where T : struct, IActionData
    {
        public static IActionExecutor Executor { get; set; }
    }
}
