using System.Collections.Generic;
using UnityEngine;

namespace NamedAsset
{
    public class AssetManager
    {
        private static int keyIndex = 0;
        private static readonly HashSet<int> unReleaseKey = new HashSet<int>();

        internal static bool ReleaseKey(int key)
        {
            if (unReleaseKey.Contains(key))
            {
                unReleaseKey.Remove(key);
                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        public static IAssetPackageInfoProvider PackageInfo;
#endif
        private static IAssetProvider assetProvider;

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void SetEditorModeMaxLoadCount(int count)
        {
            AssetDatabaseProvider.MaxLoadAssetCount = count;
        }

        public static async Awaitable Initialize(IPathProvider pathProvider)
        {

#if UNITY_EDITOR
            if (assetProvider == null)
            {
                bool forceBundle = UnityEditor.EditorPrefs.GetBool("_forceBundle_");
                if (!forceBundle)
                {
                    assetProvider = new AssetDatabaseProvider();
                }
            }
#endif
            assetProvider ??= new AssetBundleProvider(pathProvider);
            await assetProvider.Initialize();
        }

        public static async Awaitable<AssetRequest<T>> LoadAsset<T>(string name) where T : Object
        {
            throw new System.NotImplementedException();
        }


        public static void Destroy()
        {
            assetProvider?.Destroy();
        }
    }
}
