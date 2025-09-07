using System;

namespace Flow
{
    public static class FlowGUIDGenertor
    {
        private static ulong key;

        public static ulong GenID(uint index)
        {
            if (key == 0)
            {
                key = (ulong)Guid.NewGuid().ToString().GetHashCode();
                key = (key << 32) & 0xFFFFFFFF00000000;
            }
            return key | index;
        }
    }
}
