using UniRx.Async;
using UnityEngine;
using UnityEngine.SceneManagement;

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

            public T LoadAsset<T>(string name) where T : Object
            {
                return _assetBundle.LoadAsset<T>(name);
            }

            public async UniTask<T> LoadAssetAsync<T>(string name) where T : Object
            {
                Object asset = await _assetBundle.LoadAssetAsync<T>(name);
                return asset as T;
            }

            public async UniTask LoadSceneAsync(string name, LoadSceneMode loadSceneMode = LoadSceneMode.Additive)
            {
                await SceneManager.LoadSceneAsync(name, loadSceneMode);
            }

            public void Unload(bool unloadAllLoadedObjects)
            {
                _loader.Unload(_assetBundle, unloadAllLoadedObjects);
            }
        }
    }
}