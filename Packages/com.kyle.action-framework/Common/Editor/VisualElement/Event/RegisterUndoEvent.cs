using UnityEngine.UIElements;

public class RegisterUndoEvent : EventBase<RegisterUndoEvent>
{
    public string ActionName { get; private set; }
    protected override void Init()
    {
        base.Init();
        bubbles = true;
    }
    public static RegisterUndoEvent GetPooled(VisualElement target, string actionName)
    {
        var evt = GetPooled();
        evt.target = target;
        evt.ActionName = actionName;
        return evt;
    }
}