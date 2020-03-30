using System;
using System.Threading;
using UniRx.Async;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EasyAssetBundle
{
    public partial class RealAssetBundleLoader
    {
        private struct RealAssetBundle : IAssetBundle
        {
            readonly RealAssetBundleLoader _loader;
            readonly AssetBundle _assetBundle;

            public RealAssetBundle(RealAssetBundleLoader loader, AssetBundle assetBundle)
            {
                _loader = loader;
                _assetBundle = assetBundle;
            }

            public async UniTask<T> LoadAssetAsync<T>(string name, IProgress<float> progress, CancellationToken token)
                where T : Object
            {
                AssetBundleRequest req = _assetBundle.LoadAssetAsync<T>(name);
                await req.ConfigureAwait(progress, cancellation: token);
                return req.asset as T;
            }

            public async UniTask<Scene> LoadSceneAsync(string name, LoadSceneMode loadSceneMode, IProgress<float> progress,
                CancellationToken token)
            {
                AsyncOperation req = SceneManager.LoadSceneAsync(name, loadSceneMode);
                await req.ConfigureAwait(progress, cancellation: token);
                return SceneManager.GetSceneByName(name);
            }

            public void Unload(bool unloadAllLoadedObjects)
            {
                _loader.Unload(_assetBundle, unloadAllLoadedObjects);
            }
        }
    }
}