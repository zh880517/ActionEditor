using UnityEngine.UIElements;

public class PlayButtonChangeEvent : EventBase<PlayButtonChangeEvent>
{
    public PlayButtonsView.PlayEventType PlayEvent { get; private set; }
    protected override void Init()
    {
        base.Init();
        bubbles = true;
    }
    public static PlayButtonChangeEvent GetPooled(PlayButtonsView.PlayEventType playEvent)
    {
        var evt = GetPooled();
        evt.PlayEvent = playEvent;
        return evt;
    }
}
