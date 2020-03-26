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
        string _guid;

        [SerializeField]
        string _assetName;

        IAssetBundle _assetBundle;

        public string assetName => _assetName;

        public async UniTask<T> LoadAsync<T>() where T : Object
        {
            if (_assetBundle == null)
            {
                _assetBundle = await AssetBundleLoader.instance.LoadByGuidAsync(_guid);
            }

            return await _assetBundle.LoadAssetAsync<T>(_assetName);
        }

        public void Unload(bool unloadAllLoadedObjects = true)
        {
            _assetBundle?.Unload(unloadAllLoadedObjects);
            _assetBundle = null;
        }
    }
}