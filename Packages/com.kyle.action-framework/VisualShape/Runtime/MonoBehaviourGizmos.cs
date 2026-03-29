using UnityEngine;

namespace VisualShape
{
    /// <summary>
    /// 继承此类以绘制 Gizmos。
    /// </summary>
    public abstract class MonoBehaviourGizmos : MonoBehaviour, IDrawGizmos
    {
        void Awake()
        {
#if UNITY_EDITOR
            ShapeManager.Register(this);
#endif
        }

        /// <summary>
        /// 空的 OnDrawGizmosSelected 方法。
        /// 只有拥有 OnDrawGizmos/OnDrawGizmosSelected 方法的对象才会在 Unity 的 Gizmos 启用/禁用菜单中显示
        /// （场景视图右上角）。
        /// </summary>
        void OnDrawGizmosSelected()
        {
        }

        public virtual void DrawGizmos()
        {
        }
    }
}
