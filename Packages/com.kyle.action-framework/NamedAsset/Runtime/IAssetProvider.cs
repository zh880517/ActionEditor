using System.Collections;

namespace NamedAsset
{
    internal interface IAssetProvider
    {
        IEnumerable Initialize();
        NamedAssetRequest LoadAsset(string name);

        void Destroy();
    }
}
