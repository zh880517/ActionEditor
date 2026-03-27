using System.Collections.Generic;
using UnityEditor;
namespace EasyConfig
{
    public interface IEditorDataProvider
    {
        byte[] Load(string type, string name);
        bool OnAssetModify(string assetPath);
    }

    public class TEditorDataProvide<TProvide> : IDataProvider where TProvide : TEditorDataProvide<TProvide>, new()
    {
        private static TProvide instance;
        public static TProvide Instance
        {
            get
            {
                instance ??= new TProvide();
                return instance;
            }
        }
        /*
         *需要在子类添加下面的函数
        [InitializeOnLoadMethod]
        [RuntimeInitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            ConfigLoaderManager.Instance.TrySetDataProvider(Instance);
        }
        */
        //需要在子类构造函数注册所有的Provider
        protected List<IEditorDataProvider> providers;
        private readonly HashSet<string> importedAsset = new HashSet<string>();
        private bool hasDelayedCall;
        public byte[] LoadData(string type, string name)
        {
            foreach (var provider in providers)
            {
                var data = provider.Load(type, name);
                if (data != null)
                    return data;
            }
            return null;
        }

        public void OnAssetModify(string asset)
        {
            if (importedAsset.Contains(asset))
                return;
            importedAsset.Add(asset);
            if (!hasDelayedCall)
            {
                hasDelayedCall = true;
                EditorApplication.delayCall += ProcessAssetImport;
            }
        }

        private void ProcessAssetImport()
        {
            hasDelayedCall = false;
            foreach (var asset in importedAsset)
            {
                foreach (var provider in providers)
                {
                    if (provider.OnAssetModify(asset))
                        break;
                }
            }
        }
    }
}
