using UnityEngine;
using UnityEngine.UIElements;

public class PlayButtonsView : VisualElement
{
    public enum PlayEventType
    {
        FirstKey,
        LastKey,
        Play,
        Pause,
        PreKey,
        NextKey,
    }

    private readonly IconButton firstKey = new IconButton();
    private readonly IconButton lastKey = new IconButton();
    private readonly IconButton play = new IconButton();
    private readonly IconButton preKey = new IconButton();
    private readonly IconButton nextKey = new IconButton();
    private readonly IntegerField frameField = new IntegerField();
    private bool isPlaying;

    public System.Action<PlayEventType> OnPlayEvent;
    public System.Action<int> OnFrameChanged;
    public Vector2Int FrameRange = new Vector2Int(0, int.MaxValue);
    public PlayButtonsView()
    {
        style.flexDirection = FlexDirection.Row;


        firstKey.tooltip = "第一帧";
        SetButtonStyle(firstKey);
        firstKey.SetBuildinIcon("Animation.FirstKey");
        firstKey.clicked += () => OnPlayEvent?.Invoke(PlayEventType.FirstKey);
        Add(firstKey);

        preKey.tooltip = "上一帧";
        SetButtonStyle(preKey);
        preKey.SetBuildinIcon("Animation.PrevKey");
        preKey.clicked += () => OnPlayEvent?.Invoke(PlayEventType.PreKey);
        Add(preKey);

        play.tooltip = "播放/暂停";
        SetButtonStyle(play);
        play.SetBuildinIcon("Animation.Play");
        play.clicked += () => OnPlayEvent?.Invoke(isPlaying ? PlayEventType.Pause : PlayEventType.Play);
        Add(play);

        nextKey.tooltip = "下一帧";
        SetButtonStyle(nextKey);
        nextKey.SetBuildinIcon("Animation.NextKey");
        nextKey.clicked += () => OnPlayEvent?.Invoke(PlayEventType.NextKey);
        Add(nextKey);

        lastKey.tooltip = "最后一帧";
        SetButtonStyle(lastKey);
        lastKey.SetBuildinIcon("Animation.LastKey");
        lastKey.clicked += () => OnPlayEvent?.Invoke(PlayEventType.LastKey);
        Add(lastKey);

        frameField.tooltip = "当前帧";
        frameField.style.width = 50;
        frameField.RegisterValueChangedCallback(OnFrameFieldChanged);
        Add(frameField);
    }

    private void SetButtonStyle(IconButton button)
    {
        button.style.marginLeft = 2;
        button.style.width = 30;
    }

    public void SetPlayState(bool isPlaying)
    {
        this.isPlaying = isPlaying;
        play.SetBuildinIcon(isPlaying ? "PauseButton" : "Animation.Play");
    }

    public void SetFrame(int frame)
    {
        if (frame < FrameRange.x || frame > FrameRange.y)
        {
            return;
        }
        frameField.SetValueWithoutNotify(frame);
    }

    private void OnFrameFieldChanged(ChangeEvent<int> evt)
    {
        if (evt.newValue < FrameRange.x || evt.newValue > FrameRange.y)
        {
            frameField.SetValueWithoutNotify(evt.previousValue);
            return;
        }
        OnFrameChanged?.Invoke(evt.newValue);
    }
}
