using System.Collections.Generic;

namespace ActionLine.EditorView
{
    /// <summary>
    /// ActionLine预览系统，用于编辑器中模拟ActionLine的行为。不同的ActionLine系统可以自定义
    /// 该系统不管理资源的加载和卸载，只负责模拟和更新
    /// </summary>
    public class ActionLinePreviewContext
    {
        public class ClipPreview
        {
            public bool InSimulate;
            public IClipPreview Preview;
            public ActionLineClip Clip;
        }

        protected List<ClipPreview> previews = new List<ClipPreview>();
        private List<ActionClipData> clipDatas;
        public int FrameIndex { get; private set; }

        public ActionLineAsset Target { get; internal set; }
        public PreviewResourceContext ResourceContext { get; internal set; }

        public void Resfresh(List<ActionClipData> clips)
        {
            clipDatas = clips;
            //TODO:刷新simulators列表
            for (int i = 0; i < previews.Count; i++)
            {
                var simulator = previews[i];
                if (!simulator.Clip || !Target.ContainsClip(simulator.Clip))
                {
                    simulator.Preview?.Destroy(this);
                    previews.RemoveAt(i);
                    i--;
                }
            }
            for (int i = 0; i < clips.Count; i++)
            {
                var data = clips[i];
                bool found = false;
                for (int j = 0; j < previews.Count; j++)
                {
                    if (previews[j].Clip == data.Clip)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    ClipPreview unit = new ClipPreview
                    {
                        InSimulate = false,
                        Preview = CreateSimulator( data.Clip),
                        Clip = data.Clip
                    };
                    previews.Add(unit);
                }
            }
        }

        public void SetFrame(int index)
        {
            FrameIndex = index;
            for (int i = 0; i < previews.Count; i++)
            {
                var unit = previews[i];
                if (unit.Preview == null || unit.Clip == null)
                    continue;
                var data = clipDatas[i];
                int endFrame = data.Clip.StartFrame + data.Clip.Length;
                bool isIn = index >= data.Clip.StartFrame && index < endFrame;
                if (isIn == unit.InSimulate)
                {
                    if(isIn)
                    {
                        unit.Preview.Start(this, data.Clip);
                    }
                    else
                    {
                        unit.Preview.End(this, data.Clip);
                    }
                    unit.InSimulate = isIn;
                }
                if(isIn)
                {
                    int frameOffset = index - data.Clip.StartFrame;
                    unit.Preview.Update(this, data.Clip, frameOffset);
                }
            }
        }

        public void Clear()
        {
            foreach (var item in previews)
            {
                item.Preview?.Destroy(this);
            }
            previews.Clear();
        }

        public void DrawGizmos()
        {
            if (clipDatas == null || clipDatas.Count != previews.Count)
                return;
            for (int i = 0; i < previews.Count; i++)
            {
                var unit = previews[i];
                if (unit.Preview == null || unit.Clip == null)
                    continue;
                var data = clipDatas[i];
                int endFrame = data.Clip.StartFrame + data.Clip.Length;
                bool isIn = FrameIndex >= data.Clip.StartFrame && FrameIndex < endFrame;
                int frameOffset = isIn ? FrameIndex - data.Clip.StartFrame : -1;
                unit.Preview.DrawGizmos(this, data.Clip, frameOffset, data.IsInherit);
            }
        }

        public virtual void Enable()
        {

        }

        public virtual void Disable()
        {
            foreach (var item in previews)
            {
                if (item.Preview == null || item.Clip == null)
                    continue;
                item.Preview.End(this, item.Clip);
            }
        }


        public virtual void Destroy()
        {
            Clear();
        }

        protected virtual IClipPreview CreateSimulator(ActionLineClip clip)
        {
            var types = TypeWithAttributeCollector<IClipPreview, CustomClipPreviewAttribute>.Types;
            foreach (var kv in types)
            {
                if (kv.Value.ClipType == clip.GetType())
                {
                    return (IClipPreview)System.Activator.CreateInstance(kv.Key);
                }
            }
            return null;
        }
    }
}
