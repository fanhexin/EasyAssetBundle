using System;
using System.Threading;
using UniRx.Async;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasyAssetBundle
{
    [Serializable]
    public class SceneReference
    {
        [SerializeField] string _abName;

        [SerializeField] string _assetName;

        IAssetBundle _assetBundle;

        public async UniTask LoadAsync(LoadSceneMode loadSceneMode = LoadSceneMode.Additive,
            IProgress<float> progress = null, CancellationToken token = default)
        {
            if (_assetBundle == null)
            {
                _assetBundle = await AssetBundleLoader.instance.LoadAsync(_abName, progress, token);
            }

            await _assetBundle.LoadSceneAsync(_assetName, loadSceneMode, progress, token);
        }

        public void Unload(bool unloadAllLoadedObjects = true)
        {
            _assetBundle?.Unload(unloadAllLoadedObjects);
        }
    }
}