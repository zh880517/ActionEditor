using UnityEngine.Playables;

namespace Montage
{
    public interface IConnectable
    {
        void Connect<V>(V playable, int index) where V : struct, IPlayable;
    }
}
