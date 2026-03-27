using UnityEngine;

namespace NamedAsset
{
    /// <summary>
    /// 资源管理器，提供基于Version的Handle机制防止重复Release
    /// Handle分配使用数组+FreeList，避免GC
    /// </summary>
    public static class AssetManager
    {
        private const int InitialHandleCapacity = 64;

        private struct HandleSlot
        {
            public int Version;   // 每次Release时+1使旧Handle失效，每次Alloc时+1
            public int Location;  // 有效时为Provider的资源定位Key; 空闲时为FreeList的下一个索引
        }

        private static HandleSlot[] s_Slots = new HandleSlot[InitialHandleCapacity];
        private static int s_SlotCount;   // 已使用的最大槽位索引
        private static int s_FreeHead = -1; // 空闲链表头，-1表示无空闲槽

#if UNITY_EDITOR
        public static IAssetPackageInfoProvider PackageInfo;
#endif
        private static IAssetProvider s_Provider;

        /// <summary>
        /// 检查Handle是否有效（未被Release）
        /// </summary>
        internal static bool IsHandleValid(int index, int version)
        {
            if (index <= 0 || index > s_SlotCount) return false;
            return s_Slots[index].Version == version;
        }

        /// <summary>
        /// 释放Handle，返回true表示成功释放，false表示Handle已失效（防止重复Release）
        /// </summary>
        internal static bool ReleaseHandle(int index, int version)
        {
            if (!IsHandleValid(index, version)) return false;

            int location = s_Slots[index].Location;

            // Version+1 使所有持有旧version的AssetRequest失效
            s_Slots[index].Version++;
            // 回收到空闲链表
            s_Slots[index].Location = s_FreeHead;
            s_FreeHead = index;

            // 通知Provider减少引用计数
            s_Provider?.Release(location);
            return true;
        }

        /// <summary>
        /// 分配Handle，返回(index, version)，从FreeList优先复用槽位避免数组扩容
        /// </summary>
        internal static (int index, int version) AllocHandle(int location)
        {
            int index;
            if (s_FreeHead >= 0)
            {
                index = s_FreeHead;
                s_FreeHead = s_Slots[index].Location;
            }
            else
            {
                s_SlotCount++;
                if (s_SlotCount >= s_Slots.Length)
                {
                    System.Array.Resize(ref s_Slots, s_Slots.Length * 2);
                }
                index = s_SlotCount;
            }

            s_Slots[index].Version++;
            s_Slots[index].Location = location;
            return (index, s_Slots[index].Version);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void SetEditorModeMaxLoadCount(int count)
        {
            AssetDatabaseProvider.MaxLoadAssetCount = count;
        }

        public static async Awaitable Initialize(IPathProvider pathProvider)
        {
#if UNITY_EDITOR
            if (s_Provider == null)
            {
                bool forceBundle = UnityEditor.EditorPrefs.GetBool("_forceBundle_");
                if (!forceBundle)
                {
                    s_Provider = new AssetDatabaseProvider();
                }
            }
#endif
            s_Provider ??= new AssetBundleProvider(pathProvider);
            await s_Provider.Initialize();
        }

        public static async Awaitable<AssetRequest<T>> LoadAsset<T>(string name) where T : Object
        {
            if (s_Provider == null)
            {
                return new AssetRequest<T> { Name = name, Result = AssetRequestResult.AssetLoadFailed };
            }

            var result = await s_Provider.LoadAsset<T>(name);
            if (result.Result != AssetRequestResult.Success)
            {
                return new AssetRequest<T> { Name = name, Result = result.Result };
            }

            var (handleIndex, handleVersion) = AllocHandle(result.Location);
            return new AssetRequest<T>
            {
                HandleIndex = handleIndex,
                HandleVersion = handleVersion,
                CachedAsset = result.Asset as T,
                Result = AssetRequestResult.Success,
                Name = name,
            };
        }

        /// <summary>
        /// 卸载所有引用计数为0的资源及其AssetBundle
        /// </summary>
        public static void ClearUnusedAssets()
        {
            s_Provider?.ClearUnusedAssets();
        }

        public static void Destroy()
        {
            s_Provider?.Destroy();
            s_Provider = null;
            System.Array.Clear(s_Slots, 0, s_Slots.Length);
            s_SlotCount = 0;
            s_FreeHead = -1;
        }
    }
}
