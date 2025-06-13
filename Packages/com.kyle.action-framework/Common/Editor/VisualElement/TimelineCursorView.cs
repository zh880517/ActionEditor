using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class TimelineCursorView : ImmediateModeElement
{
    private float startOffset = 10;
    private float frameWidth = 10;
    private int currentFrame = 0;
    private int frameCount = 0;
    private float titleHeight = 20;
    private float handlerWidth = 10;

    private bool showFrameRange = false;
    private int startFrame = 0;
    private int length = 0;
    private bool isDragging = false;

    public float StartOffset
    {
        get { return startOffset; }
        set
        {
            if (startOffset != value)
            {
                startOffset = value;
                MarkDirtyRepaint();
            }
        }
    }

    public float FrameWidth
    {
        get { return frameWidth; }
        set
        {
            if (frameWidth != value)
            {
                frameWidth = value;
                MarkDirtyRepaint();
            }
        }
    }

    public int CurrentFrame
    {
        get { return currentFrame; }
        set
        {
            if (currentFrame != value)
            {
                currentFrame = value;
                MarkDirtyRepaint();
            }
        }
    }
    public float TitleHeight
    {
        get { return titleHeight; }
        set
        {
            if (titleHeight != value)
            {
                titleHeight = value;
                MarkDirtyRepaint();
            }
        }
    }

    public int FrameCount
    {
        get { return frameCount; }
        set
        {
            if (frameCount != value)
            {
                frameCount = value;
                MarkDirtyRepaint();
            }
        }
    }

    public float HandlerWidth
    {
        get { return handlerWidth; }
        set
        {
            if (handlerWidth != value)
            {
                handlerWidth = value;
                MarkDirtyRepaint();
            }
        }
    }
    public System.Action<int> OnFrameSelected;
    public System.Action<int> OnDragFrame;
    public TimelineCursorView()
    {
        RegisterCallback<MouseDownEvent>(OnMouseDown);
        RegisterCallback<MouseMoveEvent>(OnMouseMove);
        RegisterCallback<MouseUpEvent>(OnMouseUp);
    }

    public void ShowFrameRange(int start, int length)
    {
        if (startFrame != start || this.length != length)
        {
            startFrame = start;
            this.length = length;
            showFrameRange = true;
            MarkDirtyRepaint();
        }
    }

    public void HideFrameRange()
    {
        if (showFrameRange)
        {
            showFrameRange = false;
            MarkDirtyRepaint();
        }
    }

    private void OnMouseDown(MouseDownEvent evt)
    {
        if (evt.button != 0)
            return;
        Vector2 localPos = evt.localMousePosition;
        localPos.x -= startOffset;
        if (localPos.y <= titleHeight)
        {
            int frame = Mathf.FloorToInt(localPos.x / frameWidth);
            if (frame >= 0 && (frameCount <= 0 || frame < frameCount))
            {
                isDragging = true;
                OnFrameSelect(frame);
            }
        }
    }
    private void OnMouseMove(MouseMoveEvent evt)
    {
        if (!isDragging || evt.button != 0)
            return;
        Vector2 localPos = evt.localMousePosition;
        localPos.x -= startOffset;
        int frame = Mathf.FloorToInt(localPos.x / frameWidth);
        if (frame >= 0)
        {
            if (isDragging)
            {
                if (frameCount <= 0 || frame < frameCount)
                    OnFrameSelect(frame);
            }
            else
            {
                OnDragFrame?.Invoke(frame);
            }
        }
    }
    private void OnFrameSelect(int frame)
    {
        currentFrame = frame;
        OnFrameSelected?.Invoke(frame);
        MarkDirtyRepaint();
    }
    private void OnMouseUp(MouseUpEvent evt)
    {
        isDragging = false;
    }

    protected override void ImmediateRepaint()
    {
        float x = currentFrame * frameWidth + startOffset;
        Vector2 size = contentRect.size;
        
        Handles.DrawLine(new Vector2(x, 0), new Vector2(x, size.y));
        HandlesUtil.DrawTimelineHandle(new Vector2(x, 0), handlerWidth, titleHeight);
        if (showFrameRange)
        {
            using (new Handles.DrawingScope(new Color(0, 0, 0, 0.5f)))
            {
                float startX = startFrame * frameWidth + startOffset;
                float endX = startX + length * frameWidth;
                Handles.DrawDottedLine(new Vector2(startX, 0), new Vector2(startX, size.y), 4);
                Handles.Label(new Vector2(startX, 4), startFrame.ToString());
                Handles.DrawDottedLine(new Vector2(endX, 0), new Vector2(endX, size.y), 4);
                Handles.Label(new Vector2(endX, 4), (startFrame + length).ToString());
            }
        }
    }
}