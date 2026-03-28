using UnityEngine;

namespace NamedAsset
{
    /// <summary>
    /// 可重置组件基类，挂载时自动注册到同GameObject的PoolableEntity
    /// 回收到池时收到OnReset通知，子类重写OnReset清理状态
    /// </summary>
    public abstract class Resetable : MonoBehaviour
    {
        internal Resetable Prev;
        internal Resetable Next;
        private PoolableEntity m_Entity;

        protected virtual void OnEnable()
        {
            m_Entity = GetComponent<PoolableEntity>();
            m_Entity?.Register(this);
        }

        protected virtual void OnDisable()
        {
            m_Entity?.Unregister(this);
            m_Entity = null;
        }

        /// <summary>
        /// GameObject被回收到池时调用，子类重写以清理状态
        /// </summary>
        public abstract void OnReset();
    }
}
