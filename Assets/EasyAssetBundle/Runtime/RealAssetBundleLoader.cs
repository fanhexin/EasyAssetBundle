using System.Collections.Generic;
using System.IO;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;

namespace EasyAssetBundle
{
    public partial class RealAssetBundleLoader : IAssetBundleLoader
    {
        readonly string _basePath;
        AssetBundleManifest _manifest;
        
        readonly Dictionary<string, SharedReference<AssetBundle>> _abRefs = 
            new Dictionary<string, SharedReference<AssetBundle>>();

        readonly Dictionary<string, UnityWebRequestAsyncOperation> _abLoadingTasks =
            new Dictionary<string, UnityWebRequestAsyncOperation>();
        
        public RealAssetBundleLoader(string basePath)
        {
            _basePath = basePath;
            Init(Application.platform.ToGenericName());
        }

        void Init(string manifestName)
        {
            string path = Path.Combine(_basePath, manifestName);
            AssetBundle manifestAb = AssetBundle.LoadFromFile(path);
            _manifest = manifestAb.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            manifestAb.Unload(false);
        }

        SharedReference<AssetBundle> CreateSharedRef(AssetBundle ab)
        {
            return new SharedReference<AssetBundle>(ab, (bundle, b) =>
            {
                _abRefs.Remove(bundle.name);
                bundle.Unload((bool) b);
            });
        }

        AssetBundle LoadAssetBundle(string name)
        {
            if (_abRefs.TryGetValue(name, out var abRef))
            {
                return abRef.GetValue();
            }
            
            string path = Path.Combine(_basePath, name);
            var ab = AssetBundle.LoadFromFile(path);
            abRef = CreateSharedRef(ab);
            _abRefs[name] = abRef;
            return abRef.GetValue();
        }

        async UniTask<AssetBundle> LoadAssetBundleAsync(string name)
        {
            if (_abRefs.TryGetValue(name, out var abRef))
            {
                return abRef.GetValue();
            }
            
            if (!_abLoadingTasks.TryGetValue(name, out var req))
            {
                string path = Path.Combine(_basePath, name);
#if UNITY_EDITOR || UNITY_IOS
                path = $"file://{path}";
#endif
                req = UnityWebRequestAssetBundle.GetAssetBundle(path).SendWebRequest();
                _abLoadingTasks[name] = req;
            }

            UnityWebRequest unityWebRequest = await req;
            _abLoadingTasks.Remove(name);
            
            if (_abRefs.TryGetValue(name, out abRef))
            {
                return abRef.GetValue();
            }
            
            abRef = CreateSharedRef(DownloadHandlerAssetBundle.GetContent(unityWebRequest));
            unityWebRequest.Dispose();
            _abRefs[name] = abRef;
            return abRef.GetValue();
        }
        
        public async UniTask<IAssetBundle> LoadAsync(string name)
        {
            string[] dependencies = _manifest.GetAllDependencies(name);
            await UniTask.WhenAll(dependencies.Select(LoadAssetBundleAsync));
            var ab = await LoadAssetBundleAsync(name);
            return new RealAssetBundle(this, ab);
        }

        public IAssetBundle Load(string name)
        {
            string[] dependencies = _manifest.GetAllDependencies(name);
            foreach (string dependency in dependencies)
            {
                LoadAssetBundle(dependency);
            }

            AssetBundle ab = LoadAssetBundle(name);
            return new RealAssetBundle(this, ab);
        }

        public UniTask<IAssetBundle> LoadByGuidAsync(string guid)
        {
            string name = Config.instance.guid2Bundle[guid].name;
            return LoadAsync(name);
        }

        public IAssetBundle LoadByGuid(string guid)
        {
            string name = Config.instance.guid2Bundle[guid].name;
            return Load(name);
        }

        void Unload(AssetBundle assetBundle, bool unloadAllLoadedObjects)
        {
            string[] dependencies = _manifest.GetAllDependencies(assetBundle.name);
            foreach (string dependency in dependencies)
            {
                _abRefs[dependency].Dispose(unloadAllLoadedObjects);
            }
            _abRefs[assetBundle.name].Dispose(unloadAllLoadedObjects);
        }
    }
}