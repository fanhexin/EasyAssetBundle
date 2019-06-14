using System;
using UniRx.Async;
using UnityEngine;

namespace EasyAssetBundle
{
    [Serializable]
    public class SceneReference
    {
        [SerializeField]
        string _abName;

        [SerializeField]
        string _assetName;
        
        IAssetBundle _assetBundle;
        
        public async UniTask LoadAsync()
        {
            if (_assetBundle == null)
            {
                _assetBundle = await AssetBundleLoader.instance.LoadAsync(_abName);
            }

            await _assetBundle.LoadSceneAsync(_assetName);
        }
        
        public void Unload(bool unloadAllLoadedObjects = true)
        {
            _assetBundle?.Unload(unloadAllLoadedObjects);
        }
    }
}