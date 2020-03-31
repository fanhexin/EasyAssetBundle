#if UNITY_EDITOR
using System;
using System.Threading;
using EasyAssetBundle.Common;
using UniRx.Async;
using UnityEngine;
using Object = UnityEngine.Object;

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

        public async UniTask<(IAssetBundle ab, T asset)> LoadAssetAsync<T>(string abName, string assetName,
            IProgress<float> progress = null, CancellationToken token = default) where T : Object
        {
            var ab = new VirtualAssetBundle(abName);
            var asset = await ab.LoadAssetAsync<T>(assetName, progress, token);
            return (ab, asset);
        }

        public UniTask<IAssetBundle> LoadByGuidAsync(string guid, IProgress<float> progress, CancellationToken token)
        {
            string name = _runtimeSettings.guid2BundleDic[guid].name;
            return LoadAsync(name, progress, token);
        }

        public UniTask<(IAssetBundle ab, T asset)> LoadAssetByGuidAsync<T>(string guid, string assetName,
            IProgress<float> progress = null, CancellationToken token = default) where T : Object
        {
            string name = _runtimeSettings.guid2BundleDic[guid].name;
            return LoadAssetAsync<T>(name, assetName, progress, token);
        }

        public Hash128? GetCachedVersionRecently(string abName)
        {
            return new Hash128();
        }
    }
}
#endif