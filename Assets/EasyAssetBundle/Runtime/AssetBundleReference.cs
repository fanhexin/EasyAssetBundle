using System;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace EasyAssetBundle
{
    [Serializable]
    public class AssetBundleReference
    {
        [SerializeField]
        string _guid;
        
        public UniTask<IAssetBundle> LoadAsync(IProgress<float> progress = null, CancellationToken token = default)
        {
            return AssetBundleLoader.instance.LoadByGuidAsync(_guid, progress, token);
        }
    }
}