using System.Collections;
using System.Collections.Generic;

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

        public static IEnumerable Initialize(IPathProvider pathProvider)
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
            yield return assetProvider.Initialize();
        }

        public static AssetRequestRef Load(string name)
        {
            var request = assetProvider?.LoadAsset(name);
            if (request != null)
            {
                int key = ++keyIndex;
                while (key == 0 || unReleaseKey.Contains(key))
                {
                    key = ++keyIndex;
                }
                unReleaseKey.Add(key);
                return new AssetRequestRef
                {
                    Name = name,
                    KeyIndex = ++keyIndex,
                    request = request,
                    Version = request != null ? request.Version : 0,
                };
            }
            return new AssetRequestRef
            {
                Name = name
            };
        }

        public static void Destroy()
        {
            assetProvider?.Destroy();
        }
    }
}
