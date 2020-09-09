#if UNITY_EDITOR
using System;
using System.Linq;
using System.Threading;
using UniRx.Async;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EasyAssetBundle
{
    internal struct VirtualAssetBundle : IAssetBundle
    {
        public string name { get; }

        public VirtualAssetBundle(string assetBundleName)
        {
            name = assetBundleName;
        }

        T LoadAsset<T>(string name) where T : Object
        {
            string[] paths = AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(this.name, name);
            if (paths.Length == 0)
            {
                return null;
            }

            var asset = paths.Select(AssetDatabase.LoadAssetAtPath<T>).FirstOrDefault(x => x != null);
            return asset;
        }

        public async UniTask<T> LoadAssetAsync<T>(string name, IProgress<float> progress, CancellationToken token)
            where T : Object
        {
            progress?.Report(1);
            return LoadAsset<T>(name);
        }

        public UniTask<T[]> LoadAllAssetsAsync<T>(IProgress<float> progress = null, CancellationToken token = default) where T : Object
        {
            progress?.Report(1);
            string[] paths = AssetDatabase.GetAssetPathsFromAssetBundle(name);
            if (paths == null || paths.Length == 0)
            {
                return UniTask.FromResult<T[]>(null);
            }

            return UniTask.FromResult(paths
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(x => x != null)
                .ToArray());
        }

        public async UniTask<Scene> LoadSceneAsync(string name, LoadSceneMode loadSceneMode, IProgress<float> progress,
            CancellationToken token)
        {
            string[] guids = AssetDatabase.FindAssets($"t: Scene {name}");
            if (guids.Length == 0)
            {
                throw new Exception($"Scene {name} not found!");
            }

            string scenePath = AssetDatabase.GUIDToAssetPath(guids.First());
            AsyncOperation operation = EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, new LoadSceneParameters(loadSceneMode));
            await operation.ConfigureAwait(progress, cancellation: token);
            return SceneManager.GetSceneByName(name);
        }

        public void Unload(bool unloadAllLoadedObjects)
        {
        }
    }
}
#endif