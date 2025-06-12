using UnityEditor;
using UnityEngine;

public static class HandlesUtil
{
    private static readonly Vector3[] s_TimelineHandlePts = new Vector3[5];

    private static void SetTimelineHandlePoints(Vector2 center, float width, float height)
    {
        s_TimelineHandlePts[0] = new Vector3(center.x - width*0.5f, 0);
        s_TimelineHandlePts[1] = new Vector3(center.x - width * 0.5f, height * 0.7f);
        s_TimelineHandlePts[2] = new Vector3(center.x, height);
        s_TimelineHandlePts[3] = new Vector3(center.x + width * 0.5f, height * 0.7f);
        s_TimelineHandlePts[4] = new Vector3(center.x + width * 0.5f, 0);
    }

    public static void DrawTimelineHandle(Vector2 center, float width, float height)
    {
        SetTimelineHandlePoints(center, width, height);
        Handles.DrawAAConvexPolygon(s_TimelineHandlePts);
    }
}
