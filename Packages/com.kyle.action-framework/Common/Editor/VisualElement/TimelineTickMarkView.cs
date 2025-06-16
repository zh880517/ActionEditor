using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class TimelineTickMarkView : ImmediateModeElement
{
    private float headerInterval = 0;
    private float horizontalOffset = 0;
    private float frameWidth = 10;
    private float scale = 1.0f;
    private float frameRate = 30;
    private bool frameMode = true;
    private int frameCount = 0;
    private float titleHeight = 20;
    private TimelineCursorView cursorView;
    private bool isDragging = false;
    public int FramePointByMouse { get; private set; } = -1; // 鼠标点击的帧点

    public float HeaderInterval
    {
        get { return headerInterval; }
        set
        {
            if (headerInterval != value)
            {
                headerInterval = value;
                MarkDirtyRepaint();
                if (cursorView != null)
                    cursorView.HeaderInterval = headerInterval;
            }
        }
    }

    public float HorizontalOffset
    {
        get { return horizontalOffset; }
        set
        {
            if (horizontalOffset != value)
            {
                horizontalOffset = value;
                MarkDirtyRepaint();
                if (cursorView != null)
                    cursorView.HorizontalOffset = horizontalOffset;
            }
        }
    }

    public bool IsFrameMode
    {
        get { return frameMode; }
        set
        {
            if (frameMode != value)
            {
                frameMode = value;
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
                if (cursorView != null)
                    cursorView.FrameWidth = frameWidth;
            }
        }
    }

    public float Scale
    {
        get { return scale; }
        set
        {
            if (scale != value)
            {
                scale = value;
                MarkDirtyRepaint();
                if (cursorView != null)
                    cursorView.Scale = scale;
            }
        }
    }

    public float FrameRate
    {
        get { return frameRate; }
        set
        {
            if (frameRate != value)
            {
                frameRate = value;
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
                if (cursorView != null)
                {
                    cursorView.FrameCount = frameCount;
                }
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
                if (cursorView != null)
                {
                    cursorView.TitleHeight = titleHeight;
                }
            }
        }
    }

    public System.Action<int> OnFrameSelected;
    public System.Action<int> OnDragFrame;

    public TimelineTickMarkView()
    {
        RegisterCallback<MouseDownEvent>(OnMouseDown);
        RegisterCallback<MouseMoveEvent>(OnMouseMove);
        RegisterCallback<MouseUpEvent>(OnMouseUp);
        RegisterCallback<MouseLeaveEvent>(evt => FramePointByMouse = -1);
    }

    public void SetCursorView(TimelineCursorView cursor)
    {
        if (cursorView == cursor)
            return;
        cursorView = cursor;
        if (cursorView != null)
        {
            cursorView.FrameWidth = frameWidth;
            cursorView.HorizontalOffset = horizontalOffset;
            cursorView.Scale = scale;
            cursorView.TitleHeight = titleHeight;
            cursorView.HeaderInterval = headerInterval ;
        }
    }
    private void OnMouseDown(MouseDownEvent evt)
    {
        Vector2 localPos = evt.localMousePosition;
        localPos.x -= headerInterval;
        localPos.x += (horizontalOffset * scale);
        FramePointByMouse = Mathf.FloorToInt(localPos.x / (frameWidth * scale));
        if (evt.button != 0)
            return;
        this.CaptureMouse();
        if (localPos.y <= titleHeight)
        {
            if (FramePointByMouse >= 0 && (frameCount <= 0 || FramePointByMouse < frameCount))
            {
                isDragging = true;
                OnFrameSelect(FramePointByMouse);
            }
        }
    }
    private void OnMouseMove(MouseMoveEvent evt)
    {
        Vector2 localPos = evt.localMousePosition;
        localPos.x -= headerInterval;
        localPos.x += (horizontalOffset * scale);
        FramePointByMouse = Mathf.FloorToInt(localPos.x / (frameWidth * scale));
        if (!isDragging || evt.button != 0)
            return;
        if ((evt.pressedButtons & 1) == 0)
        {
            //如果此时鼠标没有按下，则说明在区域外松开了鼠标
            isDragging = false;
            return;
        }
        if (FramePointByMouse >= 0)
        {
            if (isDragging)
            {
                if (frameCount <= 0 || FramePointByMouse < frameCount)
                    OnFrameSelect(FramePointByMouse);
            }
            else
            {
                OnDragFrame?.Invoke(FramePointByMouse);
            }
        }
    }

    private void OnMouseUp(MouseUpEvent evt)
    {
        if (evt.button != 0)
            return;
        if (isDragging)
        {
            isDragging = false;
            this.ReleaseMouse();
        }
    }

    private void OnFrameSelect(int frame)
    {
        if(cursorView != null)
            cursorView.CurrentFrame = frame;

        OnFrameSelected?.Invoke(frame);
        MarkDirtyRepaint();
    }
    protected override void ImmediateRepaint()
    {
        Vector2 size = localBound.size;
        Color lineColore = Color.gray;
        lineColore.a = 0.5f;
        int step = 10;
        step = Mathf.FloorToInt(step / scale);
        step -= step % 5;
        step = Mathf.Max(5, step);
        int halfStep = step / 2;
        int minStep = Mathf.FloorToInt(step / 10f);
        minStep = Mathf.Max(1, minStep);
        float finalFrameWidth = frameWidth * scale;
        float finalHeaderOffset = horizontalOffset * scale - headerInterval;
        using (new Handles.DrawingScope(lineColore))
        {
            if (frameCount > 0)
            {
                var rect = new Rect(finalHeaderOffset, titleHeight * 0.5f, frameCount * finalFrameWidth, titleHeight * 0.5f);
                Color color = new Color32(65, 105, 255, 150);
                Handles.DrawSolidRectangleWithOutline(rect, color, Color.clear);
                using (new Handles.DrawingScope(color))
                {
                    float endX = frameCount * finalFrameWidth + finalHeaderOffset;
                    Handles.DrawLine(new Vector2(endX, 0), new Vector2(endX, size.y));
                }
            }
            int frameLength = Mathf.CeilToInt(size.x / finalFrameWidth);
            int startFrame = Mathf.FloorToInt(horizontalOffset / frameWidth);
            for (int i = 0; i < frameLength; ++i)
            {
                int frame = i + startFrame;
                if (frame < 0)
                    continue;
                float x = frame * finalFrameWidth - finalHeaderOffset;
                if (frame % step == 0)
                {
                    using (new Handles.DrawingScope(Color.white))
                    {
                        Handles.DrawLine(new Vector2(x, 0), new Vector2(x, titleHeight));
                    }
                    Handles.DrawLine(new Vector2(x, size.y), new Vector2(x, titleHeight));
                    string showText = frameMode ? frame.ToString() : string.Format("{0:F2}", frame / frameRate);
                    Handles.Label(new Vector2(x + 2, 4), showText, EditorStyles.label);
                }
                else if(frame % minStep == 0)
                {
                    if (frame % halfStep == 0)
                        Handles.DrawLine(new Vector2(x, titleHeight), new Vector2(x, titleHeight * 0.5f));
                    else
                        Handles.DrawLine(new Vector2(x, titleHeight), new Vector2(x, titleHeight * 0.7f));
                }
            }
        }
    }

}
