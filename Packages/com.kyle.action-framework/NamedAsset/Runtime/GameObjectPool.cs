using System.Collections.Generic;
using UnityEngine;

namespace NamedAsset
{
    /// <summary>
    /// GameObject缓存池，按资源名分组管理实例
    /// 首次请求时异步加载Prefab，后续复用已回收的实例
    /// </summary>
    internal class GameObjectPool
    {
        private struct PoolEntry
        {
            public AssetRequest<GameObject> AssetRequest;
            public Stack<GameObject> Inactive;
            public int ActiveCount;
            public bool ClearWhenInactive;
        }

        private readonly Dictionary<string, PoolEntry> m_Pools = new();

        public async Awaitable<GameObject> GetAsync(string name, Transform parent)
        {
            if (!m_Pools.TryGetValue(name, out var entry))
            {
                var request = await AssetManager.LoadAsset<GameObject>(name);
                if (!request.IsValid)
                    return null;

                // 并发加载同一资源时，等待结束后可能已被缓存
                if (m_Pools.TryGetValue(name, out var existing))
                {
                    request.Release();
                    entry = existing;
                }
                else
                {
                    entry = new PoolEntry
                    {
                        AssetRequest = request,
                        Inactive = new Stack<GameObject>()
                    };
                    m_Pools[name] = entry;
                }
            }

            // 从池中取出可用实例（跳过已被外部销毁的）
            while (entry.Inactive.Count > 0)
            {
                var go = entry.Inactive.Pop();
                if (go != null)
                {
                    var entity = go.GetComponent<PoolableEntity>();
                    if (entity != null)
                        entity.InPool = false;
                    entry.ActiveCount++;
                    m_Pools[name] = entry;
                    go.transform.SetParent(parent, false);
                    go.SetActive(true);
                    return go;
                }
            }

            var newGo = Object.Instantiate(entry.AssetRequest.Asset, parent);
            newGo.SetActive(false);
            var newEntity = newGo.AddComponent<PoolableEntity>();
            newEntity.PoolKey = name;
            entry.ActiveCount++;
            m_Pools[name] = entry;
            newGo.SetActive(true);
            return newGo;
        }

        public void Release(GameObject go)
        {
            if (go == null) return;

            var entity = go.GetComponent<PoolableEntity>();
            if (entity == null || !m_Pools.TryGetValue(entity.PoolKey, out var entry))
            {
                Object.Destroy(go);
                return;
            }
            if (entity.InPool)
                return;

            entity.Reset();
            entity.InPool = true;
            if (entry.ActiveCount > 0)
                entry.ActiveCount--;
            if (entry.ClearWhenInactive)
            {
                Object.Destroy(go);
                if (entry.ActiveCount <= 0)
                {
                    entry.AssetRequest.Release();
                    m_Pools.Remove(entity.PoolKey);
                }
                else
                {
                    m_Pools[entity.PoolKey] = entry;
                }
                return;
            }
            go.SetActive(false);
            entry.Inactive.Push(go);
            m_Pools[entity.PoolKey] = entry;
        }

        public void OnEntityDestroyed(PoolableEntity entity)
        {
            if (entity == null || entity.InPool)
                return;
            if (!m_Pools.TryGetValue(entity.PoolKey, out var entry))
                return;

            if (entry.ActiveCount > 0)
                entry.ActiveCount--;
            if (entry.ClearWhenInactive && entry.ActiveCount <= 0)
            {
                entry.AssetRequest.Release();
                m_Pools.Remove(entity.PoolKey);
            }
            else
            {
                m_Pools[entity.PoolKey] = entry;
            }
        }

        public void Clear(bool forceReleaseActive = false)
        {
            var removeKeys = new List<string>();
            foreach (var kv in m_Pools)
            {
                var entry = kv.Value;
                while (entry.Inactive.Count > 0)
                {
                    var go = entry.Inactive.Pop();
                    if (go != null) Object.Destroy(go);
                }
                if (forceReleaseActive || entry.ActiveCount <= 0)
                {
                    entry.AssetRequest.Release();
                    removeKeys.Add(kv.Key);
                }
                else
                {
                    entry.ClearWhenInactive = true;
                    m_Pools[kv.Key] = entry;
                }
            }
            for (int i = 0; i < removeKeys.Count; i++)
                m_Pools.Remove(removeKeys[i]);
        }
    }
}
