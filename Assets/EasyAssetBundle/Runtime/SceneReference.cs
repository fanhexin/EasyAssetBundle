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
        [SerializeField] string _guid;

        [SerializeField] string _assetName;

        public string guid => _guid;
        public string assetName => _assetName;

        IAssetBundle _assetBundle;

        public async UniTask<Scene> LoadAsync(LoadSceneMode loadSceneMode = LoadSceneMode.Additive,
            IProgress<float> progress = null, CancellationToken token = default)
        {
            // todo 次数处理Progress的代码跟AssetReference中的有些重复，考虑重构
            ProgressDispatcher.Handler handler = null;
            if (_assetBundle == null)
            {
                handler = ProgressDispatcher.instance.Create(progress);
                progress = handler.CreateProgress();
                _assetBundle = await AssetBundleLoader.instance.LoadByGuidAsync(_guid, handler.CreateProgress(), token);
            }

            Scene scene = await _assetBundle.LoadSceneAsync(_assetName, loadSceneMode, progress);
            handler?.Dispose();
            return scene;
        }

        public void Unload(bool unloadAllLoadedObjects = true)
        {
            _assetBundle?.Unload(unloadAllLoadedObjects);
            _assetBundle = null;
        }
    }
}