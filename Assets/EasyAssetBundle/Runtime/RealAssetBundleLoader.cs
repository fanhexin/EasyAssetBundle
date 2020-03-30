#if UNITY_EDITOR
using EasyAssetBundle.Common.Editor;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using EasyAssetBundle.Common;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace EasyAssetBundle
{
    // todo 添加获取更新文件大小的支持(做成可选配置)
    // todo *添加检测和更新指定单个或多个bundle的功能
    public partial class RealAssetBundleLoader : IAssetBundleLoader
    {
        private const string VERSION_KEY = "easyassetbundle_version";

        readonly string _basePath;
        private readonly RuntimeSettings _runtimeSettings;
        private readonly UniTask<AssetBundleManifest> _remoteManifest;
        private readonly UniTask<AssetBundleManifest> _localManifest;

        readonly Dictionary<string, SharedReference<AssetBundle>> _abRefs =
            new Dictionary<string, SharedReference<AssetBundle>>();

        private readonly string _remoteUrl;

        private string _manifestName => Application.platform.ToGenericName();

        public RealAssetBundleLoader(string basePath, RuntimeSettings runtimeSettings)
        {
            _basePath = basePath;
            _runtimeSettings = runtimeSettings;

#if UNITY_EDITOR
            _remoteUrl = $"{Settings.instance.simulateUrl}/{_manifestName}";
#else
            _remoteUrl = string.IsNullOrEmpty(runtimeSettings.cdnUrl) ? string.Empty : $"{runtimeSettings.cdnUrl}/{_manifestName}";
#endif

            _localManifest = LoadManifestAsync(GetLocalPath(_manifestName), _runtimeSettings.version);
            _remoteManifest = LoadRemoteManifestAsync();
        }

        async UniTask<int> LoadVersionAsync()
        {
            int version = PlayerPrefs.GetInt(VERSION_KEY, _runtimeSettings.version);

            if (string.IsNullOrEmpty(_remoteUrl))
            {
                return version;
            }

            using (var req = UnityWebRequest.Get($"{_remoteUrl}/version"))
            {
                req.timeout = _runtimeSettings.timeout;
                await req.SendWebRequest();
                
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
            using (var req = UnityWebRequestAssetBundle.GetAssetBundle(url, (uint) version, 0))
            {
                req.timeout = _runtimeSettings.timeout;
                await req.SendWebRequest();
                
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
            var remoteManifest = await LoadManifestAsync(GetRemoteAbUrl(_manifestName), version);
            // 如果发生错误加载远程manifest失败，直接将其设置为localmanifest
            if (remoteManifest == null)
            {
                remoteManifest = manifest;
            }

            return remoteManifest;
        }

        SharedReference<AssetBundle> CreateSharedRef(AssetBundle ab)
        {
            return new SharedReference<AssetBundle>(ab, (bundle, b) =>
            {
                _abRefs.Remove(bundle.name);
                bundle.Unload((bool) b);
            });
        }

        async UniTask<AssetBundle> LoadAssetBundleAsync(string name, IProgress<float> progress, CancellationToken token)
        {
            if (_abRefs.TryGetValue(name, out var abRef))
            {
                progress.Report(1);
                return abRef.GetValue();
            }

            var webRequest = await CreateLoadAssetBundleReq(name);
            //todo 原来用的 configureAwait在首次调用时不会上报Progress，也许升级unitytask版本试试
            using (var request = await webRequest.SendWebRequest().WaitUntilDone(progress, token))
            {
                AssetBundle ab;
                
                if (request.isHttpError ||
                    request.isNetworkError ||
                    (ab = DownloadHandlerAssetBundle.GetContent(request)) == null)
                {
                    // 发生错误加载缓存的最新版本，没有再抛异常
                    var hash = GetNewestCachedVersion(name);
                    if (hash == null)
                    {
                        throw new Exception($"{nameof(request)} {request.error}");
                    }

                    var newReq = await UnityWebRequestAssetBundle
                        .GetAssetBundle(request.url, hash.Value)
                        .SendWebRequest()
                        .WaitUntilDone(progress, token);
                    ab = DownloadHandlerAssetBundle.GetContent(newReq);
                }
                
                abRef = CreateSharedRef(ab);
                _abRefs[name] = abRef;
                return abRef.GetValue();
            }
        }

        // todo 重构缩减重复代码
        async UniTask<UnityWebRequest> CreateLoadAssetBundleReq(string name)
        {
            string url = string.Empty;
            Hash128 hash;
            AssetBundleManifest remoteManifest = await _remoteManifest;

            // 找不到的是增量更新的AssetBundle，直接远程加载处理，因为只可能在远端
            if (_runtimeSettings.name2BundleDic.TryGetValue(name, out var bundle))
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
                        hash = remoteManifest.GetAssetBundleHash(name);
                        break;
                    case BundleType.Remote:
                        url = GetRemoteAbUrl(name);
                        hash = remoteManifest.GetAssetBundleHash(name);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                url = GetRemoteAbUrl(name);
                hash = remoteManifest.GetAssetBundleHash(name);
            }

            var req = UnityWebRequestAssetBundle.GetAssetBundle(url, hash);
            req.timeout = _runtimeSettings.timeout;
            return req;
        }

        public async UniTask<IAssetBundle> LoadAsync(string name, IProgress<float> progress, CancellationToken token)
        {
            string[] dependencies = (await _remoteManifest).GetAllDependencies(name);
            using (var handler = ProgressDispatcher.instance.Create(progress))
            {
                progress = handler.CreateProgress();
                if (dependencies.Length != 0)
                {
                    await UniTask.WhenAll(dependencies.Select(x => LoadAssetBundleAsync(x, handler.CreateProgress(), token)));
                }
                var ab = await LoadAssetBundleAsync(name, progress, token);
                return new RealAssetBundle(this, ab);
            }
        }

        public async UniTask<(IAssetBundle ab, T asset)> LoadAssetAsync<T>(string abName, string assetName,
            IProgress<float> progress = null, CancellationToken token = default) where T : Object
        {
            using (var handler = ProgressDispatcher.instance.Create(progress))
            {
                IProgress<float> loadAssetProgress = handler.CreateProgress();
                var ab = await LoadAsync(abName, handler.CreateProgress(), token);
                var asset = await ab.LoadAssetAsync<T>(assetName, loadAssetProgress, token);
                return (ab, asset);
            }
        }

        public UniTask<IAssetBundle> LoadByGuidAsync(string guid, IProgress<float> progress, CancellationToken token)
        {
            string name = _runtimeSettings.guid2BundleDic[guid].name;
            return LoadAsync(name, progress, token);
        }

        public UniTask<(IAssetBundle ab, T asset)> LoadAssetByGuidAsync<T>(string guid, string assetName,
            IProgress<float> progress = null, CancellationToken token = default) where T : Object
        {
            string name = _runtimeSettings.guid2BundleDic[guid].name;
            return LoadAssetAsync<T>(name, assetName, progress, token);
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

        Hash128? GetNewestCachedVersion(string abName)
        {
            string path = Path.Combine(Caching.defaultCache.path, abName);
            if (!Directory.Exists(path))
            {
                return null;
            }

            var directories = Directory.GetDirectories(path);
            if (directories.Length == 0)
            {
                return null;
            }

            return directories.OrderByDescending(Directory.GetCreationTime)
                .Select(x => Hash128.Parse(Path.GetFileNameWithoutExtension(x)))
                .First();
        }
    }
}