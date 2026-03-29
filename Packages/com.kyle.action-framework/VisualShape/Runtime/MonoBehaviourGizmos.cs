using UnityEngine;

namespace VisualShape
{
    /// <summary>
    /// Inherit from this class to draw gizmos.
    /// </summary>
    public abstract class MonoBehaviourGizmos : MonoBehaviour, IDrawGizmos
    {
        public MonoBehaviourGizmos()
        {
#if UNITY_EDITOR
            ShapeManager.Register(this);
#endif
        }

        /// <summary>
        /// An empty OnDrawGizmosSelected method.
        /// Only objects with an OnDrawGizmos/OnDrawGizmosSelected method will show up in Unity's menu for enabling/disabling
        /// the gizmos per object type (upper right corner of the scene view).
        /// </summary>
        void OnDrawGizmosSelected()
        {
        }

        public virtual void DrawGizmos()
        {
        }
    }
}
