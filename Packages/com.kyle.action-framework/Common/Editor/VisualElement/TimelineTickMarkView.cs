using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class TimelineTickMarkView : ImmediateModeElement
{
    private float startOffset = 10;
    private float frameWidth = 10;
    private float scale = 1.0f;
    private float frameRate = 30;
    private bool frameMode = true;
    private int frameCount = 0;
    private float titleHeight = 20;
    private TimelineCursorView cursorView;
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
                if (cursorView != null)
                    cursorView.StartOffset = startOffset;
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

    public float FrameWdith
    {
        get { return frameWidth; }
        set
        {
            if (frameWidth != value)
            {
                frameWidth = value;
                MarkDirtyRepaint();
                if (cursorView != null)
                    cursorView.FrameWidth = frameWidth * scale;
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
                    cursorView.FrameWidth = frameWidth * scale;
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

    public void SetCursorView(TimelineCursorView cursor)
    {
        if (cursorView == cursor)
            return;
        cursorView = cursor;
        if (cursorView != null)
        {
            cursorView.FrameWidth = frameWidth * scale;
            cursorView.TitleHeight = titleHeight;
            cursorView.StartOffset = startOffset ;
        }
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
        using (new Handles.DrawingScope(lineColore))
        {
            if (frameCount > 0)
            {
                var rect = new Rect(startOffset, titleHeight * 0.5f, frameCount * finalFrameWidth, titleHeight * 0.5f);
                Color color = new Color32(65, 105, 255, 150);
                Handles.DrawSolidRectangleWithOutline(rect, color, Color.clear);
                using (new Handles.DrawingScope(color))
                {
                    float endX = frameCount * finalFrameWidth + startOffset;
                    Handles.DrawLine(new Vector2(endX, 0), new Vector2(endX, size.y));
                }
            }
            if (frameMode)
            {
                int frameLength = Mathf.FloorToInt((size.x - startOffset) / finalFrameWidth);
                for (int i = 0; i < frameLength; ++i)
                {
                    float x = i * finalFrameWidth + startOffset;
                    if (i % step == 0)
                    {
                        using (new Handles.DrawingScope(Color.white))
                        {
                            Handles.DrawLine(new Vector2(x, 0), new Vector2(x, titleHeight));
                        }
                        Handles.DrawLine(new Vector2(x, size.y), new Vector2(x, titleHeight));
                        GUIContent content = new GUIContent( i.ToString());
                        Handles.Label(new Vector2(x + 2, 4), content, EditorStyles.label);
                    }
                    else if(i % minStep == 0)
                    {
                        if (i % halfStep == 0)
                            Handles.DrawLine(new Vector2(x, titleHeight), new Vector2(x, titleHeight * 0.5f));
                        else
                            Handles.DrawLine(new Vector2(x, titleHeight), new Vector2(x, titleHeight * 0.7f));
                    }
                }
            }
            else
            {
                float stepWidth = finalFrameWidth * frameRate * 0.1f;
                int steps = Mathf.FloorToInt((size.x - startOffset) / stepWidth);
                for (int i = 0; i < steps; ++i)
                {
                    float x = i * stepWidth + startOffset;
                    if (i % step == 0)
                    {
                        using (new Handles.DrawingScope(Color.white))
                        {
                            Handles.DrawLine(new Vector2(x, 0), new Vector2(x, titleHeight));
                        }
                        Handles.DrawLine(new Vector2(x, size.y), new Vector2(x, titleHeight));
                        GUIContent content = new GUIContent(string.Format("{0:F2}", i * 0.5f));
                        Handles.Label(new Vector2(x + 2, 4), content, EditorStyles.label);
                    }
                    else if (i % minStep == 0)
                    {
                        if (i % halfStep == 0)
                            Handles.DrawLine(new Vector2(x, titleHeight), new Vector2(x, titleHeight * 0.5f));
                        else
                            Handles.DrawLine(new Vector2(x, titleHeight), new Vector2(x, titleHeight * 0.7f));
                    }
                }
            }

        }
    }

}
