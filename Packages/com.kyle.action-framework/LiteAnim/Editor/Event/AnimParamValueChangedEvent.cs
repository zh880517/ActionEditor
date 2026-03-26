using UnityEngine.UIElements;

namespace LiteAnim.EditorView
{
    public class AnimParamValueChangedEvent : EventBase<AnimParamValueChangedEvent>
    {
        public float Value { get; private set; }

        public static AnimParamValueChangedEvent GetPooled(float value)
        {
            var evt = EventBase<AnimParamValueChangedEvent>.GetPooled();
            evt.Value = value;
            evt.bubbles = true;
            return evt;
        }
    }
}
