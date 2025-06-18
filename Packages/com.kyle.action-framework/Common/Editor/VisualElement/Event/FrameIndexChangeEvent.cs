using UnityEngine.UIElements;

public class FrameIndexChangeEvent : EventBase<FrameIndexChangeEvent>
{
    public int Frame { get; private set; }
    protected override void Init()
    {
        base.Init();
        Frame = 0;
    }
    public static FrameIndexChangeEvent GetPooled(int frame)
    {
        var evt = GetPooled();
        evt.Frame = frame;
        evt.bubbles = true;
        return evt;
    }
}