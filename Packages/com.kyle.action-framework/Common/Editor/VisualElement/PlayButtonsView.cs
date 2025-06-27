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

    public int MaxFrame = int.MaxValue;
    private static readonly Color borderColor = new Color32(25, 25, 25, 255);
    public PlayButtonsView()
    {
        style.flexDirection = FlexDirection.Row;
        style.borderBottomColor = borderColor;
        style.borderBottomWidth = 1;
        style.borderBottomColor = borderColor;
        style.borderLeftWidth = 1;
        style.borderLeftColor = borderColor;
        style.borderRightWidth = 2;
        style.borderRightColor = borderColor;

        firstKey.tooltip = "第一帧";
        SetButtonStyle(firstKey);
        firstKey.SetBuildinIcon("Animation.FirstKey");
        firstKey.clicked += () => OnPlayEvent(PlayEventType.FirstKey);
        Add(firstKey);

        preKey.tooltip = "上一帧";
        SetButtonStyle(preKey);
        preKey.SetBuildinIcon("Animation.PrevKey");
        preKey.clicked += () => OnPlayEvent(PlayEventType.PreKey);
        Add(preKey);

        play.tooltip = "播放/暂停";
        SetButtonStyle(play);
        play.SetBuildinIcon("Animation.Play");
        play.clicked += () => OnPlayEvent(isPlaying ? PlayEventType.Pause : PlayEventType.Play);
        Add(play);

        nextKey.tooltip = "下一帧";
        SetButtonStyle(nextKey);
        nextKey.SetBuildinIcon("Animation.NextKey");
        nextKey.clicked += () => OnPlayEvent(PlayEventType.NextKey);
        Add(nextKey);

        lastKey.tooltip = "最后一帧";
        SetButtonStyle(lastKey);
        lastKey.SetBuildinIcon("Animation.LastKey");
        lastKey.clicked += () => OnPlayEvent(PlayEventType.LastKey);
        Add(lastKey);

        frameField.tooltip = "当前帧";
        frameField.style.width = 50;
        frameField.RegisterValueChangedCallback(OnFrameFieldChanged);
        Add(frameField);
    }

    private void OnPlayEvent(PlayEventType type)
    {
        using (var evt = PlayButtonChangeEvent.GetPooled(type))
        {
            SendEvent(evt);
        }
    }


    private void SetButtonStyle(IconButton button)
    {
        //button.style.marginLeft = 2;
        button.style.width = 30;
        button.style.borderRightColor = borderColor;
        button.style.borderRightWidth = 1;
    }

    public void SetPlayState(bool isPlaying)
    {
        this.isPlaying = isPlaying;
        play.SetBuildinIcon(isPlaying ? "PauseButton" : "Animation.Play");
    }

    public void SetMaxFrame(int frame)
    {
        if (frame > MaxFrame)
        {
            frame = MaxFrame;
        }
        frameField.SetValueWithoutNotify(frame);
    }

    public void SetFrame(int frameIndex)
    {
        frameIndex = Mathf.Clamp(frameIndex, 0, MaxFrame);
        if (frameIndex != frameField.value)
        {
            frameField.SetValueWithoutNotify(frameIndex);
        }
    }

    private void OnFrameFieldChanged(ChangeEvent<int> evt)
    {
        int frame = evt.newValue;
        if (evt.newValue > MaxFrame)
        {
            frame = MaxFrame;
            frameField.SetValueWithoutNotify(frame);
        }
        using (var newEvt = FrameIndexChangeEvent.GetPooled(frame))
        {
            SendEvent(newEvt);
        }
    }
}
