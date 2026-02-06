using UnityEngine.Playables;

namespace LiteAnim
{
    public interface IConnectable
    {
        void Connect<V>(V playable, int index) where V : struct, IPlayable;
    }
}
