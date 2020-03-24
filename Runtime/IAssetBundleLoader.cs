using UniRx.Async;

namespace EasyAssetBundle
{
    public interface IAssetBundleLoader
    {
        UniTask<IAssetBundle> LoadAsync(string name);
        IAssetBundle Load(string name);
        UniTask<IAssetBundle> LoadByGuidAsync(string guid);
        IAssetBundle LoadByGuid(string guid);
    }
}