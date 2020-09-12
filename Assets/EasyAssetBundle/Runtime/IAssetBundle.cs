using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EasyAssetBundle
{
    public interface IAssetBundle
    {
        string name { get; }
        UniTask<T> LoadAssetAsync<T>(string name, IProgress<float> progress = null, CancellationToken token = default)
            where T : Object;

        UniTask<T[]> LoadAllAssetsAsync<T>(IProgress<float> progress = null, CancellationToken token = default)
            where T : Object;

        UniTask<Scene> LoadSceneAsync(string name, LoadSceneMode loadSceneMode = LoadSceneMode.Additive,
            IProgress<float> progress = null, CancellationToken token = default);

        void Unload(bool unloadAllLoadedObjects = true);
    }
}