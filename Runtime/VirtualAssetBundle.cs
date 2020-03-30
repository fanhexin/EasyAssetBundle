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
        readonly string _assetBundleName;

        public VirtualAssetBundle(string assetBundleName)
        {
            _assetBundleName = assetBundleName;
        }

        private T LoadAsset<T>(string name) where T : Object
        {
            string[] paths = AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(_assetBundleName, name);
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