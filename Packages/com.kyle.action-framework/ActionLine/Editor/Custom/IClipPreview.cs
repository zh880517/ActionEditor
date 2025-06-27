
namespace ActionLine.EditorView
{
    public interface IClipPreview
    {
        System.Type ClipType { get; }
        void Start(ActionLinePreviewContext context, ActionLineClip clip);
        void Update(ActionLinePreviewContext context, ActionLineClip clip, int frameOffset);
        void End(ActionLinePreviewContext context, ActionLineClip clip);
        //frameOffset 为-1则表示当前帧不在 Clip 的范围内
        void DrawGizmos(ActionLinePreviewContext context, ActionLineClip clip, int frameOffset, bool readOnly);
        void Destroy(ActionLinePreviewContext context);
    }

    public abstract class TClipSimulator<T> : IClipPreview where T : ActionLineClip
    {
        public System.Type ClipType => typeof(T);
        public void Start(ActionLinePreviewContext context, ActionLineClip clip)
        {
            OnStart(context, clip as T);
        }

        protected abstract void OnStart(ActionLinePreviewContext context, T clip);

        public void Update(ActionLinePreviewContext context, ActionLineClip clip, int frameOffset)
        {
            OnUpdate(context, clip as T, frameOffset);
        }

        protected abstract void OnUpdate(ActionLinePreviewContext context, T clip, int frameOffset);
        public void End(ActionLinePreviewContext context, ActionLineClip clip)
        {
            OnEnd(context, clip as T);
        }

        protected abstract void OnEnd(ActionLinePreviewContext context, T clip);
        public void DrawGizmos(ActionLinePreviewContext context, ActionLineClip clip, int frameOffset, bool readOnly)
        {
            OnDrawGizmos(context, clip as T, frameOffset, readOnly);
        }

        protected abstract void OnDrawGizmos(ActionLinePreviewContext context, T clip, int frameOffset, bool readOnly);

        public abstract void Destroy(ActionLinePreviewContext context);

    }
}
