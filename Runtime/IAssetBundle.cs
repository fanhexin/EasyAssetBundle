using UniRx.Async;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasyAssetBundle
{
    public interface IAssetBundle
    {
        UniTask<T> LoadAssetAsync<T>(string name) where T : Object;
        UniTask LoadSceneAsync(string name, LoadSceneMode loadSceneMode = LoadSceneMode.Additive);
        void Unload(bool unloadAllLoadedObjects = true);
    }
}