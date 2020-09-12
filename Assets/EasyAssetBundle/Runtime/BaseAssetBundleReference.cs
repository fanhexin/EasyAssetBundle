using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EasyAssetBundle
{
    [Serializable]
    public abstract class BaseAssetBundleReference
    {
        [SerializeField] string _guid;
        public string guid => _guid;

        IAssetBundle _ab;

        protected async UniTask<T> LoadAssetAsync<T>(string assetName, IProgress<float> progress = null,
            CancellationToken token = default) where T : UnityEngine.Object
        {
            ProgressDispatcher.Handler handler = null;
            if (_ab == null)
            {
                handler = ProgressDispatcher.instance.Create(progress);
                progress = handler.CreateProgress();
                _ab = await AssetBundleLoader.instance.LoadByGuidAsync(_guid, handler.CreateProgress(), token);
            }

            var ret = await _ab.LoadAssetAsync<T>(assetName, progress, token);
            handler?.Dispose();
            return ret;
        }

        public void Unload(bool unloadAllLoadedObjects = true)
        {
            if (_ab == null)
            {
                return;
            }
            _ab?.Unload(unloadAllLoadedObjects);
            _ab = null;
        }
    }
}