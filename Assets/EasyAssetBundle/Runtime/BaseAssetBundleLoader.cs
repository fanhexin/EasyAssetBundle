using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EasyAssetBundle.Common;
using UniRx.Async;
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

        private string GuidToName(string guid)
        {
            return _runtimeSettings.guid2BundleDic[guid].name;
        }

        public abstract UniTask InitAsync();
        public abstract UniTask<IAssetBundle> LoadAsync(string name, IProgress<float> progress = null, CancellationToken token = default, bool exceptionFallback = true);

        public async UniTask<(IAssetBundle ab, T asset)> LoadAssetAsync<T>(string abName, string assetName, IProgress<float> progress = null,
            CancellationToken token = default, bool exceptionFallback = true) where T : Object
        {
            using (var handler = ProgressDispatcher.instance.Create(progress))
            {
                IProgress<float> loadAssetProgress = handler.CreateProgress();
                var ab = await LoadAsync(abName, handler.CreateProgress(), token, exceptionFallback);
                var asset = await ab.LoadAssetAsync<T>(assetName, loadAssetProgress, token);
                return (ab, asset);
            }        
        }

        public UniTask<IAssetBundle> LoadByGuidAsync(string guid, IProgress<float> progress = null, CancellationToken token = default, bool exceptionFallback = true)
        {
            return LoadAsync(GuidToName(guid), progress, token, exceptionFallback);
        }

        public UniTask<(IAssetBundle ab, T asset)> LoadAssetByGuidAsync<T>(string guid, string assetName, IProgress<float> progress = null,
            CancellationToken token = default, bool exceptionFallback = true) where T : Object
        {
            return LoadAssetAsync<T>(GuidToName(guid), assetName, progress, token, exceptionFallback);
        }

        public virtual Hash128? GetCachedVersionRecently(string abName)
        {
            IEnumerable<Hash128> cachedVersions = GetCachedVersions(abName);
            return cachedVersions.Any() ? cachedVersions.First() : (Hash128?) null;
        }
        
        public abstract IEnumerable<Hash128> GetCachedVersions(string abName);
        public virtual int version => _runtimeSettings.version;
        public abstract bool Contains(string abName);
        public abstract bool CheckForUpdates(string abName);
        public abstract IEnumerable<string> CheckForUpdates();
    }
}