using System;
using System.Threading;
using UniRx.Async;

namespace EasyAssetBundle
{
    [Serializable]
    public class AssetBundleReference : BaseAssetBundleReference
    {
        public UniTask<T> LoadAssetAsync<T>(string assetName, IProgress<float> progress = null,
            CancellationToken token = default) where T : UnityEngine.Object
        {
            return base.LoadAssetAsync<T>(assetName, progress, token);
        }
    }
}