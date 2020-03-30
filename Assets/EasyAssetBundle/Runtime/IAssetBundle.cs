using System;
using System.Threading;
using UniRx.Async;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EasyAssetBundle
{
    public interface IAssetBundle
    {
        UniTask<T> LoadAssetAsync<T>(string name, IProgress<float> progress = null, CancellationToken token = default)
            where T : Object;

        UniTask<Scene> LoadSceneAsync(string name, LoadSceneMode loadSceneMode = LoadSceneMode.Additive,
            IProgress<float> progress = null, CancellationToken token = default);

        void Unload(bool unloadAllLoadedObjects = true);
    }
}