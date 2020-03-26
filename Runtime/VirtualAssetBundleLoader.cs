#if UNITY_EDITOR
using System;
using System.Threading;
using EasyAssetBundle.Common;
using UniRx.Async;

namespace EasyAssetBundle
{
    public class VirtualAssetBundleLoader : IAssetBundleLoader
    {
        private readonly RuntimeSettings _runtimeSettings;

        public VirtualAssetBundleLoader(RuntimeSettings runtimeSettings)
        {
            _runtimeSettings = runtimeSettings;
        }
        
        public async UniTask<IAssetBundle> LoadAsync(string name, IProgress<float> progress, CancellationToken token)
        {
            progress?.Report(1);
            return new VirtualAssetBundle(name);    
        }

        public UniTask<IAssetBundle> LoadByGuidAsync(string guid, IProgress<float> progress, CancellationToken token)
        {
            string name = _runtimeSettings.guid2BundleDic[guid].name;
            return LoadAsync(name, progress, token);
        }
    }
}
#endif