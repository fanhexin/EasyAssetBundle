using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EasyAssetBundle.Common;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyAssetBundle
{
    public abstract class BaseAssetBundleLoader : IAssetBundleLoader
    {
        protected readonly RuntimeSettings _runtimeSettings;

        protected BaseAssetBundleLoader(RuntimeSettings runtimeSettings)
        {
            _runtimeSettings = runtimeSettings;
        }

        string GuidToName(string guid)
        {
            return _runtimeSettings.guid2BundleDic[guid].name;
        }

        public abstract UniTask InitAsync();
        public abstract UniTask<IAssetBundle> LoadAsync(string name, IProgress<float> progress = null, CancellationToken token = default);

        public async UniTask<(IAssetBundle ab, T asset)> LoadAssetAsync<T>(string abName, string assetName, IProgress<float> progress = null,
            CancellationToken token = default) where T : Object
        {
            using (var handler = ProgressDispatcher.instance.Create(progress))
            {
                IProgress<float> loadAssetProgress = handler.CreateProgress();
                var ab = await LoadAsync(abName, handler.CreateProgress(), token);
                var asset = await ab.LoadAssetAsync<T>(assetName, loadAssetProgress, token);
                return (ab, asset);
            }        
        }

        public UniTask<IAssetBundle> LoadByGuidAsync(string guid, IProgress<float> progress = null, CancellationToken token = default)
        {
            return LoadAsync(GuidToName(guid), progress, token);
        }

        public UniTask<(IAssetBundle ab, T asset)> LoadAssetByGuidAsync<T>(string guid, string assetName, IProgress<float> progress = null,
            CancellationToken token = default) where T : Object
        {
            return LoadAssetAsync<T>(GuidToName(guid), assetName, progress, token);
        }

        public abstract Hash128? GetCachedVersionRecently(string abName);
        public virtual int version => _runtimeSettings.version;
    }
}