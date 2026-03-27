using System.Collections.Generic;

namespace NamedAsset
{
    public interface IAssetPackageInfoProvider
    {
        Dictionary<string, string> GetAllAssets();
    }
}
