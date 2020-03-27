using System;
using System.Threading;
using UniRx.Async;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyAssetBundle
{
    [Serializable]
    public class AssetReference
    {
        [SerializeField]
        string _guid;

        [SerializeField]
        string _assetName;

        IAssetBundle _assetBundle;

        public string assetName => _assetName;

        public async UniTask<T> LoadAsync<T>(IProgress<float> progress = null, CancellationToken token = default) 
            where T : Object
        {
            ProgressDispatcher.Handler handler = null;
            if (_assetBundle == null)
            {
                handler = ProgressDispatcher.instance.Create(progress);
                progress = handler.CreateProgress();
                _assetBundle = await AssetBundleLoader.instance.LoadByGuidAsync(_guid, handler.CreateProgress(), token);
            }

            var ret = await _assetBundle.LoadAssetAsync<T>(_assetName, progress, token);
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