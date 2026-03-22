using UnityEngine.UIElements;

public class PlayManipulator : Manipulator
{
    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PlayButtonChangeEvent>(OnPlayButtonChange);
        target.RegisterCallback<Timeline.FrameIndexChangeEvent>(OnFrameIndexChange);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PlayButtonChangeEvent>(OnPlayButtonChange);
        target.UnregisterCallback<Timeline.FrameIndexChangeEvent>(OnFrameIndexChange);
    }

    private void OnPlayButtonChange(PlayButtonChangeEvent evt)
    {
    }

    private void OnFrameIndexChange(Timeline.FrameIndexChangeEvent evt)
    {
    }
}
