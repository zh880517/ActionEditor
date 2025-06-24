namespace ActionLine.EditorView
{
    public interface IClipSimulator
    {
        void Start(ActionLineSimulate simulate, ActionLineClip clip);
        void Update(ActionLineSimulate simulate, ActionLineClip clip, int frameOffset);
        void End(ActionLineSimulate simulate, ActionLineClip clip);
        //frameOffset 为-1则表示当前帧不在 Clip 的范围内
        void DrawGizmos(ActionLineSimulate simulate, ActionLineClip clip, int frameOffset, bool readOnly);
        void Destroy(ActionLineSimulate simulate, ActionLineClip clip);
    }
}
