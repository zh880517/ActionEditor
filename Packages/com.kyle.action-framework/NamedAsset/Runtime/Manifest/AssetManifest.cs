using System.Collections.Generic;

namespace NamedAsset
{
    [System.Serializable]
    public class AssetManifest
    {
        [System.Serializable]
        public class BundleInfo
        {
            //AssetBundle包名，可能会带有XX/
            public string Name;
            public UnityEngine.Hash128 Hash;
            public uint Crc;
            //仅包含指定打包资源
            public string[] Assets;
            public string[] Dependencies;
        }
        [System.Serializable]
        public struct AssetInfo
        {
            //提供给逻辑接口的资源名Package/AssetName
            public string Name;
            public int Location;//BundleInde << 16 | AssetIndex
        }
        //不记录不依赖任何资源的并且不是指定打包资源的Bundle
        public List<BundleInfo> Bundles = new List<BundleInfo>();
        public List<AssetInfo> Assets = new List<AssetInfo>();
    }
}