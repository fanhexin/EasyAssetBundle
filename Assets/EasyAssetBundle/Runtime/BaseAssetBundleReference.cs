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

        UniTask<IAssetBundle>? _assetBundle;

        protected async UniTask<T> LoadAssetAsync<T>(string assetName, IProgress<float> progress = null,
            CancellationToken token = default) where T : UnityEngine.Object
        {
            ProgressDispatcher.Handler handler = null;
            if (_assetBundle == null)
            {
                handler = ProgressDispatcher.instance.Create(progress);
                progress = handler.CreateProgress();
                _assetBundle = AssetBundleLoader.instance.LoadByGuidAsync(_guid, handler.CreateProgress(), token);
            }

            var ab = await _assetBundle.Value;
            var ret = await ab.LoadAssetAsync<T>(assetName, progress, token);
            handler?.Dispose();
            return ret;
        }

        public async void Unload(bool unloadAllLoadedObjects = true)
        {
            (await _assetBundle.Value)?.Unload(unloadAllLoadedObjects);
            _assetBundle = null;
        }
    }
}