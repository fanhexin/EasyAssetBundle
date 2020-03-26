using UniRx.Async;

namespace EasyAssetBundle
{
    public interface IAssetBundleLoader
    {
        UniTask<IAssetBundle> LoadAsync(string name);
        UniTask<IAssetBundle> LoadByGuidAsync(string guid);
    }
}