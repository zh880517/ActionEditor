using System;
namespace ECSLite
{
    public struct EntityIdentify : IEquatable<EntityIdentify>
    {
        public int Index;
        public int Version;

        public readonly bool Valid => Version > 0;

        public readonly bool Equals(EntityIdentify other)
        {
            return Index == other.Index && Version == other.Version;
        }

        public override readonly bool Equals(object obj)
        {
            return obj is EntityIdentify other && Equals(other);
        }

        public override readonly int GetHashCode()
        {
            return Index.GetHashCode();
        }

        public override readonly string ToString()
        {
            return $"(Index:{Index} Version:{Version})";
        }

        public static bool operator ==(EntityIdentify left, EntityIdentify right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EntityIdentify left, EntityIdentify right)
        {
            return !left.Equals(right);
        }
    }
}
