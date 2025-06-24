using System;
using System.Collections.Generic;
using static UnityEditor.Progress;

namespace ActionLine.EditorView
{
    public class ActionLineSimulate
    {
        public class ClipSimulator
        {
            public bool InSimulate;
            public IClipSimulator Simulator;
            public ActionLineClip clip;
        }

        protected List<ClipSimulator> simulators = new List<ClipSimulator>();
        private List<ActionClipData> clipDatas;
        public int FrameIndex { get; private set; }

        public ActionLineAsset Target { get; internal set; }

        public void Resfresh(List<ActionClipData> clips)
        {
            clipDatas = clips;
            //TODO:刷新simulators列表
        }

        public void SetFrame(int index)
        {
            FrameIndex = index;
            for (int i = 0; i < simulators.Count; i++)
            {
                var unit = simulators[i];
                var data = clipDatas[i];
                int endFrame = data.Clip.StartFrame + data.Clip.Length;
                bool isIn = index >= data.Clip.StartFrame && index < endFrame;
                if (isIn == unit.InSimulate)
                {
                    if(isIn)
                    {
                        unit.Simulator.Start(this, data.Clip);
                    }
                    else
                    {
                        unit.Simulator.End(this, data.Clip);
                    }
                    unit.InSimulate = isIn;
                }
                if(isIn)
                {
                    int frameOffset = index - data.Clip.StartFrame;
                    unit.Simulator.Update(this, data.Clip, frameOffset);
                }
            }
        }

        public void DrawGizmos()
        {
            if (clipDatas == null || clipDatas.Count != simulators.Count)
                return;
            for (int i = 0; i < simulators.Count; i++)
            {
                var unit = simulators[i];
                var data = clipDatas[i];
                int endFrame = data.Clip.StartFrame + data.Clip.Length;
                bool isIn = FrameIndex >= data.Clip.StartFrame && FrameIndex < endFrame;
                int frameOffset = isIn ? FrameIndex - data.Clip.StartFrame : -1;
                unit.Simulator.DrawGizmos(this, data.Clip, frameOffset, data.IsInherit);
            }
        }


        public virtual void Enable()
        {

        }

        public virtual void Disable()
        {
            foreach (var item in simulators)
            {
                item.Simulator.End(this, item.clip);
            }
        }

        public virtual void Destroy()
        {
            foreach (var item in simulators)
            {
                item.Simulator.Destroy(this, item.clip);
            }
        }
    }
}
