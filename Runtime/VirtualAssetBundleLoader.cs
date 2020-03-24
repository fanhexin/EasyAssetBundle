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

        public UniTask<IAssetBundle> LoadByGuidAsync(string guid)
        {
            string name = Config.instance.guid2Bundle[guid].name;
            return LoadAsync(name);
        }

        public IAssetBundle LoadByGuid(string guid)
        {
            string name = Config.instance.guid2Bundle[guid].name;
            return Load(name);
        }
    }
}
#endif