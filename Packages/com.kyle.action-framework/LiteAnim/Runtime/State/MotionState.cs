using UnityEngine.Playables;

namespace LiteAnim
{
    public abstract class MotionState
    {
        public LiteAnimMotion Motion { get; set; }
        public string Name { get; private set; }// 动画状态名称,这里缓存是为了优化访问时的GC问题
        public float Length { get; private set; }// 动画时长，实时计算的，初始化时缓存

        public ILiteAnimPlayer Player { get; set; }
        private int version = -1;// 动画版本号，用于检测动画资源是否被修改
        public bool IsChanged => Motion == null || version != Motion.Version;// 动画资源是否被修改
        public int LayerIndex => Motion != null ? Motion.LayerIndex : 0;
        public virtual void Init(LiteAnimMotion motion)
        {
            Motion = motion;
            Name = motion ? motion.name : string.Empty;
            Length = motion ? motion.GetLength() : 0;
            version = motion ? motion.Version : -1;
        }

        public abstract void Create(PlayableGraph graph);
        public abstract void Connect(IConnectable destination, int inputPort);
        public abstract void Connect<V>(V playable, int index) where V : struct, IPlayable;
        public abstract void Evaluate(double time);
        public abstract void Destroy();
    }
}
