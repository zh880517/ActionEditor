using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NamedAsset
{
    public interface ITickable
    {
        bool OnTick();
    }
    internal class AssetUpdateLoop : MonoBehaviour
    {
        private static AssetUpdateLoop _instance;
        public static AssetUpdateLoop Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("_AssetLoadCoroutine_");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<AssetUpdateLoop>();
                }
                return _instance;
            }
        }

        private List<ITickable> tickables = new List<ITickable>();

        public void AddLoadTick(ITickable tick)
        {
            if (tickables.Contains(tick))
                return;
            tickables.Add(tick);
            if (tickables.Count == 1)
                StartCoroutine(DoLoadTick());
        }

        public static void RemoveTick(ITickable tickable)
        {
            if (_instance == null)
                return;
            _instance.tickables.Remove(tickable);
        }

        private IEnumerator DoLoadTick()
        {
            for (int i = 0; i < tickables.Count; ++i)
            {
                var loader = tickables[i];
                if (loader.OnTick())
                {
                    tickables.RemoveAt(i);
                    --i;
                }
            }
            if (tickables.Count > 0)
                yield break;
            else
                yield return null;
        }

        private void OnDestroy()
        {
            tickables.Clear();
            _instance = null;
        }
    }
}
