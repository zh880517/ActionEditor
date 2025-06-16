using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class TimelineCursorView : ImmediateModeElement
{
    private float headerInterval = 0;
    private float horizontalOffset = 0;
    private float frameWidth = 10;
    private float scale = 1.0f;
    private int currentFrame = 0;
    private int frameCount = 0;
    private float titleHeight = 20;
    private float handlerWidth = 10;

    private bool showFrameRange = false;
    private int startFrame = 0;
    private int length = 0;
    public float HeaderInterval
    {
        get { return headerInterval; }
        set
        {
            if (headerInterval != value)
            {
                headerInterval = value;
                MarkDirtyRepaint();
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
        pickingMode = PickingMode.Ignore;
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

    protected override void ImmediateRepaint()
    {
        float finalWidth = frameWidth * scale;
        float x = currentFrame * finalWidth + headerInterval - (horizontalOffset * scale);
        Vector2 size = contentRect.size;
        
        Handles.DrawLine(new Vector2(x, 0), new Vector2(x, size.y));
        HandlesUtil.DrawTimelineHandle(new Vector2(x, 0), handlerWidth, titleHeight);
        if (showFrameRange)
        {
            using (new Handles.DrawingScope(new Color(0, 0, 0, 0.5f)))
            {
                float startX = startFrame * finalWidth + headerInterval - (horizontalOffset * scale); ;
                float endX = startX + length * finalWidth;
                Handles.DrawDottedLine(new Vector2(startX, 0), new Vector2(startX, size.y), 4);
                Handles.Label(new Vector2(startX, 4), startFrame.ToString());
                Handles.DrawDottedLine(new Vector2(endX, 0), new Vector2(endX, size.y), 4);
                Handles.Label(new Vector2(endX, 4), (startFrame + length).ToString());
            }
        }
    }
}