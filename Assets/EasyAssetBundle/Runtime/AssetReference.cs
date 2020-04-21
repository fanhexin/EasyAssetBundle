using System;
using System.Threading;
using UniRx.Async;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyAssetBundle
{
    [Serializable]
    public class AssetReference : BaseAssetBundleReference
    {
        [SerializeField] string _assetName;

        public string assetName => _assetName;

        public UniTask<T> LoadAsync<T>(IProgress<float> progress = null, CancellationToken token = default) 
            where T : Object
        {
            return LoadAssetAsync<T>(_assetName, progress, token);
        }
    }
}