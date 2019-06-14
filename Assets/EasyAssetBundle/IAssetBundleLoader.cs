using UniRx.Async;

namespace EasyAssetBundle
{
    public interface IAssetBundleLoader
    {
        UniTask<IAssetBundle> LoadAsync(string name);
        IAssetBundle Load(string name);
    }
}