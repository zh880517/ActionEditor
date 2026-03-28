using UnityEngine;

namespace NamedAsset
{
    /// <summary>
    /// 挂载在池化GameObject上的内部组件，记录所属池Key
    /// 通过侵入式链表管理所有Resetable，回收时遍历调用OnReset
    /// </summary>
    internal class PoolableEntity : MonoBehaviour
    {
        internal string PoolKey;
        internal Resetable Head;

        internal void Register(Resetable node)
        {
            node.Next = Head;
            node.Prev = null;
            if (Head != null) Head.Prev = node;
            Head = node;
        }

        internal void Unregister(Resetable node)
        {
            if (node.Prev != null) node.Prev.Next = node.Next;
            else Head = node.Next;
            if (node.Next != null) node.Next.Prev = node.Prev;
            node.Prev = null;
            node.Next = null;
        }

        /// <summary>
        /// 回收时由GameObjectPool调用，遍历链表通知所有Resetable
        /// </summary>
        internal void Reset()
        {
            var current = Head;
            while (current != null)
            {
                current.OnReset();
                current = current.Next;
            }
        }
    }
}
