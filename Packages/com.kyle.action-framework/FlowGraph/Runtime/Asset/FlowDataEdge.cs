namespace Flow
{
    [System.Serializable]
    public struct FlowDataEdge : System.IEquatable<FlowDataEdge>
    {
        public FlowNode Output;
        public FlowNode Input;
        public string OutputSlot;
        public string InputSlot;
        public ulong EdgeID;

        public readonly bool Equals(FlowDataEdge other)
        {
            return EdgeID == other.EdgeID 
                && Output == other.Output 
                && Input == other.Input 
                && OutputSlot == other.OutputSlot 
                && InputSlot == other.InputSlot;
        }

        public override readonly bool Equals(object obj)
        {
            return obj is FlowDataEdge other && Equals(other);
        }

        public readonly override int GetHashCode()
        {
            return EdgeID.GetHashCode();
        }

        public static bool operator ==(FlowDataEdge left, FlowDataEdge right) => left.Equals(right);
        public static bool operator !=(FlowDataEdge left, FlowDataEdge right) => !left.Equals(right);
    }
}
