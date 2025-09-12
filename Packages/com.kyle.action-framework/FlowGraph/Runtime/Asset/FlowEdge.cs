namespace Flow
{
    [System.Serializable]
    public struct FlowEdge : System.IEquatable<FlowEdge>
    {
        public FlowNode Output;//输出节点
        public FlowNode Input;//目标节点
        public int OutputIndex;

        public readonly bool Equals(FlowEdge other)
        {
            return Output == other.Output && Input == other.Input && OutputIndex == other.OutputIndex;
        }

        public readonly override bool Equals(object obj)
        {
            return obj is FlowEdge other && Equals(other);
        }

        public readonly override int GetHashCode()
        {
            return System.HashCode.Combine(Output, Input, OutputIndex);
        }

        public static bool operator ==(FlowEdge left, FlowEdge right) => left.Equals(right);
        public static bool operator !=(FlowEdge left, FlowEdge right) => !(left == right);

    }
}
