#if UNITY_EDITOR
using EasyAssetBundle.Common;
using UniRx.Async;

namespace EasyAssetBundle
{
    public class VirtualAssetBundleLoader : IAssetBundleLoader
    {
        private readonly Manifest _manifest;

        public VirtualAssetBundleLoader(Manifest manifest)
        {
            _manifest = manifest;
        }
        
        public async UniTask<IAssetBundle> LoadAsync(string name)
        {
            return new VirtualAssetBundle(name);    
        }

        public IAssetBundle Load(string name)
        {
            return new VirtualAssetBundle(name);
        }

        public UniTask<IAssetBundle> LoadByGuidAsync(string guid)
        {
            string name = _manifest.guid2BundleDic[guid].name;
            return LoadAsync(name);
        }

        public IAssetBundle LoadByGuid(string guid)
        {
            string name = _manifest.guid2BundleDic[guid].name;
            return Load(name);
        }
    }
}
#endif