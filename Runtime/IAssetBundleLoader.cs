using System;
using System.Threading;
using UniRx.Async;

namespace EasyAssetBundle
{
    public interface IAssetBundleLoader
    {
        UniTask<IAssetBundle> LoadAsync(string name, IProgress<float> progress = null, CancellationToken token = default);

        UniTask<(IAssetBundle ab, T asset)> LoadAsync<T>(string abName, string assetName, IProgress<float> progress = null,
            CancellationToken token = default) where T : UnityEngine.Object;
        
        UniTask<IAssetBundle> LoadByGuidAsync(string guid, IProgress<float> progress = null, CancellationToken token = default);
        
        UniTask<(IAssetBundle ab, T asset)> LoadByGuidAsync<T>(string guid, string assetName, IProgress<float> progress = null,
            CancellationToken token = default) where T : UnityEngine.Object;
    }
}