namespace ActionLine
{
    [System.Serializable]
    public struct ActionClipData : System.IEquatable<ActionClipData>
    {
        public ActionLineClip Clip;
        public bool IsInherit;
        public bool IsActive;

        public readonly bool Equals(ActionClipData other)
        {
            return Clip == other.Clip;
        }
        public override readonly bool Equals(object obj)
        {
            return obj is ActionClipData other && Equals(other);
        }

        public override readonly int GetHashCode()
        {
            return Clip != null ? Clip.GetHashCode() : 0;
        }

        public static bool operator ==(ActionClipData left, ActionClipData right)
        {
            return left.Clip == right.Clip;
        }

        public static bool operator !=(ActionClipData left, ActionClipData right)
        {
            return left.Clip != right.Clip;
        }
    }
}
