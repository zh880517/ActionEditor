using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class TimelineBarView : IMGUIContainer
{
    private float frameWidth = 10;
    private float scale = 1.0f;
    private float frameRate = 30;
    private bool frameMode = true;
    private int frameCount = 0;
    private float titleHeight = 20;
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

    public TimelineBarView()
    {
        onGUIHandler = OnGUI;
    }


    private void OnGUI()
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
                var rect = new Rect(0, titleHeight * 0.5f, frameCount * finalFrameWidth, titleHeight * 0.5f);
                Color color = new Color32(65, 105, 255, 150);
                Handles.DrawSolidRectangleWithOutline(rect, color, Color.clear);
                using (new Handles.DrawingScope(color))
                {
                    float endX = frameCount * finalFrameWidth;
                    Handles.DrawLine(new Vector2(endX, 0), new Vector2(endX, size.y));
                }
            }
            if (frameMode)
            {
                int frameLength = Mathf.FloorToInt(size.x / finalFrameWidth);
                for (int i = 0; i < frameLength; ++i)
                {
                    float x = i * finalFrameWidth;
                    if (i % step == 0)
                    {
                        using (new Handles.DrawingScope(Color.white))
                        {
                            Handles.DrawLine(new Vector2(x, 0), new Vector2(x, titleHeight));
                        }
                        Handles.DrawLine(new Vector2(x, size.y), new Vector2(x, titleHeight));
                        GUIContent content = new GUIContent( i.ToString());
                        Vector2 lablesize = EditorStyles.label.CalcSize(content); 
                        lablesize.x += 2; // Add some padding
                        GUI.Label(new Rect(new Vector2(x + 2, 0), lablesize), content);
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
                int steps = Mathf.FloorToInt(size.x / stepWidth);
                for (int i = 0; i < steps; ++i)
                {
                    float x = i * stepWidth;
                    if (i % step == 0)
                    {
                        using (new Handles.DrawingScope(Color.white))
                        {
                            Handles.DrawLine(new Vector2(x, 0), new Vector2(x, titleHeight));
                        }
                        Handles.DrawLine(new Vector2(x, size.y), new Vector2(x, titleHeight));
                        GUIContent content = new GUIContent(string.Format("{0:F2}", i * 0.5f));
                        Vector2 lablesize = EditorStyles.label.CalcSize(content);
                        lablesize.x += 2; // Add some padding
                        GUI.Label(new Rect(new Vector2(x + 2, 0), lablesize), content);
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
