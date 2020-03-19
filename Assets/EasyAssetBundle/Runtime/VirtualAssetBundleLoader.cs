#if UNITY_EDITOR
using UniRx.Async;

namespace EasyAssetBundle
{
    public class VirtualAssetBundleLoader : IAssetBundleLoader
    {
        public async UniTask<IAssetBundle> LoadAsync(string name)
        {
            return new VirtualAssetBundle(name);    
        }

        public IAssetBundle Load(string name)
        {
            return new VirtualAssetBundle(name);
        }
    }
}
#endif