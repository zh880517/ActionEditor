using UnityEngine.Playables;

namespace Montage
{
    public abstract class MontageMotionState
    {
        public abstract MontageMotion Motion { get; }
        public string Name { get; private set; }// 动画状态名称,这里缓存是为了优化访问时的GC问题
        public float Length { get; private set; }// 动画时长，实时计算的，初始化时缓存

        public IMontagePlayer Player { get; set; }
        public double Time { get; set; }// 当前时间
        public float Weight { get; set; }// 混合权重
        private int version = -1;// 动画版本号，用于检测动画资源是否被修改
        public bool IsChanged => version != Motion.Version;// 动画资源是否被修改
        public MontageMotionState(MontageMotion motion)
        {
            Name = motion.name;
            Length = motion.Length;
            version = motion.Version;
        }

        public abstract void Init(PlayableGraph graph);
        public abstract void Connect(IConnectable destination, int inputPort);
        public abstract void Evaluate(double time);
        public abstract void Destroy();

    }
}
