using System;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace EasyAssetBundle
{
    [Serializable]
    public abstract class BaseAssetBundleReference
    {
        [SerializeField] string _guid;

        IAssetBundle _assetBundle;

        protected async UniTask<T> LoadAssetAsync<T>(string assetName, IProgress<float> progress = null,
            CancellationToken token = default) where T : UnityEngine.Object
        {
            ProgressDispatcher.Handler handler = null;
            if (_assetBundle == null)
            {
                handler = ProgressDispatcher.instance.Create(progress);
                progress = handler.CreateProgress();
                _assetBundle = await AssetBundleLoader.instance.LoadByGuidAsync(_guid, handler.CreateProgress(), token);
            }

            var ret = await _assetBundle.LoadAssetAsync<T>(assetName, progress, token);
            handler?.Dispose();
            return ret;
        }

        public void Unload(bool unloadAllLoadedObjects = true)
        {
            _assetBundle?.Unload(unloadAllLoadedObjects);
            _assetBundle = null;
        }
    }
}