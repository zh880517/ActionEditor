namespace Flow
{
    public struct FlowNodeResult
    {
        public bool IsRunning;
        public int OutputIndex;

        public readonly static FlowNodeResult Running = new FlowNodeResult { IsRunning = true, OutputIndex = -1 };
        public readonly static FlowNodeResult Next = new FlowNodeResult { IsRunning = false, OutputIndex = 0 };
        public readonly static FlowNodeResult True = new FlowNodeResult { IsRunning = false, OutputIndex = 0 };
        public readonly static FlowNodeResult False = new FlowNodeResult { IsRunning = false, OutputIndex = 1 };
    }
}
