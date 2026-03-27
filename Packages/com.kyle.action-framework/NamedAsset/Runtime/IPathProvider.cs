namespace NamedAsset
{
    public enum FilePathType
    {
        None,//无效
        File,//单一文件
        CombinFile,//合并文件
        URL,//网络文件或者Android平台的StreamingAssets目录
        Bytes,//内存数据
    }
    public struct FileLocaltion
    {
        public string Path;
        public FilePathType Type;
        public ulong Offset;
        public ulong Length;//AssetBundle不需要
        public byte[] Data;
    }

    public interface IPathProvider
    {
        FileLocaltion GetAssetManifestPath();
        FileLocaltion GetAssetBundlePath(string bundleName);
    }
}
