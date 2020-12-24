using System;
using System.Collections.Generic;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace EasyAssetBundle
{
    public interface IAssetBundleLoader
    {
        UniTask InitAsync();
        UniTask<IAssetBundle> LoadAsync(string name, IProgress<float> progress = null, CancellationToken token = default);

        UniTask<(IAssetBundle ab, T asset)> LoadAssetAsync<T>(string abName, string assetName, IProgress<float> progress = null,
            CancellationToken token = default) where T : UnityEngine.Object;
        
        UniTask<IAssetBundle> LoadByGuidAsync(string guid, IProgress<float> progress = null, CancellationToken token = default);
        
        UniTask<(IAssetBundle ab, T asset)> LoadAssetByGuidAsync<T>(string guid, string assetName, IProgress<float> progress = null,
            CancellationToken token = default) where T : UnityEngine.Object;

        Hash128? GetCachedVersionRecently(string abName);
        IEnumerable<Hash128> GetCachedVersions(string abName);
        int version { get; }
        bool Contains(string abName);
        bool CheckForUpdates(string abName);
        IEnumerable<string> CheckForUpdates();
    }
}