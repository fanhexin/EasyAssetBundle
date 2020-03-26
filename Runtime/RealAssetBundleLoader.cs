#if UNITY_EDITOR
using EasyAssetBundle.Common.Editor;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using EasyAssetBundle.Common;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;

namespace EasyAssetBundle
{
    // todo 添加获取更新文件大小的支持(做成可选配置)
    // todo 添加检测和更新指定单个或多个bundle的功能
    public partial class RealAssetBundleLoader : IAssetBundleLoader
    {
        private const string VERSION_KEY = "easyassetbundle_version";

        readonly string _basePath;
        private readonly Manifest _manifest;
        private readonly UniTask<AssetBundleManifest> _remoteManifest;
        private readonly UniTask<AssetBundleManifest> _localManifest;

        readonly Dictionary<string, SharedReference<AssetBundle>> _abRefs =
            new Dictionary<string, SharedReference<AssetBundle>>();

        readonly Dictionary<string, UnityWebRequestAsyncOperation> _abLoadingTasks =
            new Dictionary<string, UnityWebRequestAsyncOperation>();

        private readonly string _remoteUrl;

        private string _manifestName => Application.platform.ToGenericName();

        public RealAssetBundleLoader(string basePath, Manifest manifest)
        {
            _basePath = basePath;
            _manifest = manifest;

#if UNITY_EDITOR
            _remoteUrl = $"{Settings.instance.simulateUrl}/{_manifestName}";
#else
            _remoteUrl = string.IsNullOrEmpty(manifest.cdnUrl) ? string.Empty : $"{manifest.cdnUrl}/{_manifestName}";
#endif

            _localManifest = LoadManifestAsync(GetLocalPath(_manifestName), _manifest.version);
            _remoteManifest = LoadRemoteManifestAsync();
        }

        async UniTask<int> LoadVersionAsync()
        {
            int version = PlayerPrefs.GetInt(VERSION_KEY, _manifest.version);

            if (string.IsNullOrEmpty(_remoteUrl))
            {
                return version;
            }

            using (var req = await UnityWebRequest.Get($"{_remoteUrl}/version").SendWebRequest())
            {
                if (req.isNetworkError || req.isHttpError ||
                    !int.TryParse(req.downloadHandler.text, out int remoteVersion))
                {
                    return version;
                }

                if (remoteVersion <= version)
                {
                    return version;
                }

                version = remoteVersion;
                PlayerPrefs.SetInt(VERSION_KEY, version);
            }

            return version;
        }

        async UniTask<AssetBundleManifest> LoadManifestAsync(string url, int version)
        {
            AssetBundle ab;
            using (var req = await UnityWebRequestAssetBundle.GetAssetBundle(url, (uint) version, 0).SendWebRequest())
            {
                if (req.isNetworkError || req.isHttpError)
                {
                    return null;
                }

                ab = DownloadHandlerAssetBundle.GetContent(req);
            }

            var manifest = await ab.LoadAssetAsync<AssetBundleManifest>(nameof(AssetBundleManifest));
            ab.Unload(false);
            return manifest as AssetBundleManifest;
        }

        async UniTask<AssetBundleManifest> LoadRemoteManifestAsync()
        {
            var manifest = await _localManifest;
            if (string.IsNullOrEmpty(_remoteUrl))
            {
                return manifest;
            }

            int version = await LoadVersionAsync();
            return await LoadManifestAsync(GetRemoteAbUrl(_manifestName), version);
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
                req = await CreateLoadAssetBundleReq(name);
                _abLoadingTasks[name] = req;
            }

            UnityWebRequest unityWebRequest = await req;
            // todo 发生error的异常处理或者抛出异常
            // todo 添加取消操作和report progress的能力
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

        // todo 重构缩减重复代码
        async UniTask<UnityWebRequestAsyncOperation> CreateLoadAssetBundleReq(string name)
        {
            string url = string.Empty;
            Hash128 hash = (await _remoteManifest).GetAssetBundleHash(name);

            // 找不到的是增量更新的AssetBundle，直接远程加载处理，因为只可能在远端
            if (_manifest.name2BundleDic.TryGetValue(name, out var bundle))
            {
                switch (bundle.type)
                {
                    case BundleType.Static:
                        url = GetLocalPath(name);
                        hash = (await _localManifest).GetAssetBundleHash(name);
                        break;
                    case BundleType.Patchable:
                        var localHash = (await _localManifest).GetAssetBundleHash(name);
                        if (!Caching.IsVersionCached(GetLocalPath(name), localHash))
                        {
                            url = GetLocalPath(name);
                            hash = localHash;
                            break;
                        }

                        url = GetRemoteAbUrl(name);
                        hash = (await _remoteManifest).GetAssetBundleHash(name);
                        break;
                    case BundleType.Remote:
                        url = GetRemoteAbUrl(name);
                        hash = (await _remoteManifest).GetAssetBundleHash(name);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                url = GetRemoteAbUrl(name);
                hash = (await _remoteManifest).GetAssetBundleHash(name);
            }

            return UnityWebRequestAssetBundle.GetAssetBundle(url, hash).SendWebRequest();
        }

        public async UniTask<IAssetBundle> LoadAsync(string name)
        {
            string[] dependencies = (await _remoteManifest).GetAllDependencies(name);
            await UniTask.WhenAll(dependencies.Select(LoadAssetBundleAsync));
            var ab = await LoadAssetBundleAsync(name);
            return new RealAssetBundle(this, ab);
        }

        public IAssetBundle Load(string name)
        {
            string[] dependencies = _remoteManifest.Result.GetAllDependencies(name);
            foreach (string dependency in dependencies)
            {
                LoadAssetBundle(dependency);
            }

            AssetBundle ab = LoadAssetBundle(name);
            return new RealAssetBundle(this, ab);
        }

        public UniTask<IAssetBundle> LoadByGuidAsync(string guid)
        {
            string name = _manifest.guid2BundleDic[guid].name;
            return LoadAsync(name);
        }

        public IAssetBundle LoadByGuid(string guid)
        {
            string name = _manifest.guid2BundleDic[guid].name;
            return Load(name);
        }

        async void Unload(AssetBundle assetBundle, bool unloadAllLoadedObjects)
        {
            string[] dependencies = (await _remoteManifest).GetAllDependencies(assetBundle.name);
            foreach (string dependency in dependencies)
            {
                _abRefs[dependency].Dispose(unloadAllLoadedObjects);
            }

            _abRefs[assetBundle.name].Dispose(unloadAllLoadedObjects);
        }

        string GetRemoteAbUrl(string name)
        {
            return $"{_remoteUrl}/{name}";
        }

        string GetLocalPath(string name)
        {
            string path = Path.Combine(_basePath, name);
#if UNITY_EDITOR || UNITY_IOS
            path = $"file://{path}";
#endif
            return path;
        }
    }
}