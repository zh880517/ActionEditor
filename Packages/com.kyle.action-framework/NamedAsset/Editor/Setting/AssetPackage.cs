namespace NamedAsset.Editor
{
    public enum AssetPackType
    {
        PackAllInOne,//全部打包在一起
        PackSingleFile,//单一文件打包
        PackDirectory,//目录打包
    }

    [System.Serializable]
    public class AssetPackage
    {
        public string Name;
        public AssetPackType PackType;
        [FolderSelector]
        public string Path;
        public string SearchPattern;
        public System.IO.SearchOption SearchOption;

        public string[] GetAssetPaths()
        {
            if (string.IsNullOrEmpty(Name) 
                || string.IsNullOrEmpty(Path) 
                || string.IsNullOrEmpty(SearchPattern))
                return new string[0];
            var files = System.IO.Directory.GetFiles(Path, SearchPattern, SearchOption);
            for (int i=0; i<files.Length; ++i)
            {
                files[i] = files[i].Replace("\\", "/");
            }
            return files;
        }
    }
}
