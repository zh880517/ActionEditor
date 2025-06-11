using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class TimelineBarView : IMGUIContainer
{
    private float frameWidth = 10;
    private float frameRate = 30;
    private bool frameMode = true;
    private int frameCount = 0;
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

    public TimelineBarView()
    {
        onGUIHandler = OnGUI;
    }

    private void OnGUI()
    {
        Vector2 size = localBound.size;
        if (frameMode)
        {
            int frameLength = Mathf.FloorToInt(size.x / frameWidth);
            for (int i = 0; i < frameLength; ++i)
            {
                float x = i * frameWidth;
                if (i % 5 == 0)
                {
                    Handles.DrawLine(new Vector2(x, size.y), new Vector2(x, 0));
                    GUIContent content = new GUIContent(i.ToString());
                    Vector2 lablesize = EditorStyles.label.CalcSize(content);
                    lablesize.x = frameWidth * 5;
                    GUI.Label(new Rect(new Vector2(x + 2, 0), lablesize), content);
                }
                else
                {
                    Handles.DrawLine(new Vector2(x, size.y), new Vector2(x, size.y - 2));
                }
            }
        }
        else
        {
            float stepWidth = frameWidth * frameRate * 0.1f;
            int steps = Mathf.FloorToInt(size.x / stepWidth);
            for (int i = 0; i < steps; ++i)
            {
                float x = i * stepWidth;
                if (i % 5 == 0)
                {
                    Handles.DrawLine(new Vector2(x, size.y), new Vector2(x, 0));
                    GUIContent content = new GUIContent(string.Format("{0:F2}", i * 0.5f));
                    Vector2 lablesize = EditorStyles.label.CalcSize(content);
                    lablesize.x = frameWidth * 5;
                    GUI.Label(new Rect(new Vector2(x + 2, 0), lablesize), content);
                }
                else
                {
                    Handles.DrawLine(new Vector2(x, size.y), new Vector2(x, size.y - 2));
                }
            }
        }
        if(frameCount > 0)
        {
            var rect = new Rect(0, size.y * 0.5f, frameCount * frameWidth, size.y * 0.5f);
            Color color = Color.green;
            color.a = 0.5f;
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0.0f, color, 0.0f, 0.0f);
        }
    }

}
