using System;
using UniRx.Async;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyAssetBundle
{
    [Serializable]
    public class AssetReference
    {
        [SerializeField]
        string _abName;

        [SerializeField]
        string _assetName;

        IAssetBundle _assetBundle;

        public async UniTask<T> LoadAsync<T>() where T : Object
        {
            if (_assetBundle == null)
            {
                _assetBundle = await AssetBundleLoader.instance.LoadAsync(_abName);
            }

            return await _assetBundle.LoadAssetAsync<T>(_assetName);
        }

        public T Load<T>() where T : Object
        {
            if (_assetBundle == null)
            {
                _assetBundle = AssetBundleLoader.instance.Load(_abName);
            }

            return _assetBundle.LoadAsset<T>(_assetName);
        }

        public void Unload(bool unloadAllLoadedObjects = true)
        {
            _assetBundle?.Unload(unloadAllLoadedObjects);
            _assetBundle = null;
        }
    }
}