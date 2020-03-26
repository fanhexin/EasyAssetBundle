#if UNITY_EDITOR
using System;
using System.Linq;
using UniRx.Async;
using UnityEditor;
using UnityEditor.SceneManagement;
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

        public async UniTask<T> LoadAssetAsync<T>(string name) where T : Object
        {
            return LoadAsset<T>(name);
        }

        public async UniTask LoadSceneAsync(string name, LoadSceneMode loadSceneMode = LoadSceneMode.Additive)
        {
            string[] guids = AssetDatabase.FindAssets($"t: Scene {name}");
            if (guids.Length == 0)
            {
                throw new Exception($"Scene {name} not found!");
            }

            string scenePath = AssetDatabase.GUIDToAssetPath(guids.First());
            await EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, new LoadSceneParameters(loadSceneMode));
        }

        public void Unload(bool unloadAllLoadedObjects)
        {
        }
    }
}
#endif