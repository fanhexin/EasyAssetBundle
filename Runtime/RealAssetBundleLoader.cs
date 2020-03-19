using System.Collections.Generic;
using System.IO;
using UniRx.Async;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasyAssetBundle
{
    public class RealAssetBundleLoader : IAssetBundleLoader
    {
        struct RealAssetBundle : IAssetBundle
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

        readonly string _basePath;
        AssetBundleManifest _manifest;
        readonly Dictionary<string, (AssetBundle ab, int refCnt)> _abRefs = new Dictionary<string, (AssetBundle ab, int refCnt)>();
        readonly List<UniTask<AssetBundle>> _abLoadTasks = new List<UniTask<AssetBundle>>();
        readonly Dictionary<string, AssetBundleCreateRequest> _abLoadingTasks = new Dictionary<string, AssetBundleCreateRequest>();
        
        public RealAssetBundleLoader(string basePath, string manifestName)
        {
            _basePath = basePath;
            Init(manifestName);
        }

        void Init(string manifestName)
        {
            string path = Path.Combine(_basePath, manifestName);
            AssetBundle manifestAb = AssetBundle.LoadFromFile(path);
            _manifest = manifestAb.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            manifestAb.Unload(false);
        }

        AssetBundle IncreaseRef(string name, AssetBundle ab = null)
        {
            if (!_abRefs.TryGetValue(name, out var item))
            {
                if (ab != null)
                {
                    _abRefs[name] = (ab, 1);
                }
                return ab;
            }

            ++item.refCnt;
            _abRefs[name] = item;
            return item.ab;
        }

        AssetBundle LoadAssetBundle(string name)
        {
            string path = Path.Combine(_basePath, name);
            var ab = AssetBundle.LoadFromFile(path);
            _abRefs[name] = (ab, 1);
            return ab;
        }

        async UniTask<AssetBundle> LoadAssetBundleAsync(string name)
        {
            if (!_abLoadingTasks.TryGetValue(name, out var req))
            {
                string path = Path.Combine(_basePath, name);
                req = AssetBundle.LoadFromFileAsync(path);
                _abLoadingTasks[name] = req;
            }

            var ab = await req;
            IncreaseRef(name, ab);
            _abLoadingTasks.Remove(name);
            return ab;
        }
        
        public async UniTask<IAssetBundle> LoadAsync(string name)
        {
            await UpdateDependencies(name);
            AssetBundle ab = IncreaseRef(name);
            if (ab == null)
            {
                ab = await LoadAssetBundleAsync(name);
                if (ab == null)
                {
                    return null;
                }
            }
            return new RealAssetBundle(this, ab);
        }

        public IAssetBundle Load(string name)
        {
            string[] dependencies = _manifest.GetAllDependencies(name);
            foreach (string dependency in dependencies)
            {
                if (IncreaseRef(dependency) != null)
                {
                    continue;
                }

                string path = Path.Combine(_basePath, dependency);
                _abRefs[dependency] = (AssetBundle.LoadFromFile(path), 1);
            }

            AssetBundle ab = IncreaseRef(name);
            if (ab == null)
            {
                ab = LoadAssetBundle(name);
                if (ab == null)
                {
                    return null;
                }
            }
            return new RealAssetBundle(this, ab);
        }

        async UniTask UpdateDependencies(string name)
        {
            _abLoadTasks.Clear();
            string[] dependencies = _manifest.GetAllDependencies(name);
            foreach (string dependency in dependencies)
            {
                if (IncreaseRef(dependency) != null)
                {
                    continue;
                }

                _abLoadTasks.Add(LoadAssetBundleAsync(dependency));
            }

            if (_abLoadTasks.Count == 0)
            {
                return;
            }

            await UniTask.WhenAll(_abLoadTasks);
            _abLoadTasks.Clear();
        }

        void Unload(AssetBundle assetBundle, bool unloadAllLoadedObjects)
        {
            string[] dependencies = _manifest.GetAllDependencies(assetBundle.name);
            foreach (string dependency in dependencies)
            {
                ReduceRef(dependency, unloadAllLoadedObjects);
            }
            ReduceRef(assetBundle.name, unloadAllLoadedObjects);
        }

        void ReduceRef(string name, bool unloadAllLoadedObjects)
        {
            var (ab, refCnt) = _abRefs[name];
            if (refCnt == 1)
            {
                _abRefs.Remove(name);
                ab.Unload(unloadAllLoadedObjects);
                return;
            }

            _abRefs[name] = (ab, --refCnt);
        }
    }
}