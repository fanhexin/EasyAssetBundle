#if UNITY_EDITOR
using EasyAssetBundle.Common.Editor;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using EasyAssetBundle.Common;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;

namespace EasyAssetBundle
{
    // todo 添加获取更新文件大小的支持(做成可选配置)
    // todo *添加检测和更新指定单个或多个bundle的功能
    internal partial class RealAssetBundleLoader : BaseAssetBundleLoader
    {
        const string VERSION_KEY = "easyassetbundle_version";

        readonly string _basePath;
        readonly Dictionary<string, SharedReference<AssetBundle>> _abRefs =
            new Dictionary<string, SharedReference<AssetBundle>>();
        
        readonly Dictionary<string, UniTask<AssetBundle>> _abLoadingTasks = 
            new Dictionary<string, UniTask<AssetBundle>>();

        AssetBundleManifest _remoteManifest;
        AssetBundleManifest _localManifest;
        readonly string _remoteUrl;
        UniTask? _initTask;

        string _manifestName => Application.platform.ToGenericName();

        public RealAssetBundleLoader(string basePath, RuntimeSettings runtimeSettings)
            : base(runtimeSettings)
        {
            if (!string.IsNullOrEmpty(runtimeSettings.encryptKey))
            {
                var method = typeof(AssetBundle).GetMethod("SetAssetBundleDecryptKey", BindingFlags.Static | BindingFlags.Public);
                method?.Invoke(null, new[] {runtimeSettings.encryptKey});
            }
            _basePath = basePath;

#if UNITY_EDITOR
            if (Settings.instance.httpServiceSettings.enabled)
            {
                _remoteUrl = $"{Settings.instance.simulateUrl}/{_manifestName}";
            }
            else
            {
                _remoteUrl = string.IsNullOrEmpty(runtimeSettings.cdnUrl) ? string.Empty : $"{runtimeSettings.cdnUrl}/{_manifestName}";
            }
#else
            _remoteUrl = string.IsNullOrEmpty(runtimeSettings.cdnUrl) ? string.Empty : $"{runtimeSettings.cdnUrl}/{_manifestName}";
#endif
        }

        public override async UniTask InitAsync()
        {
            _localManifest = await LoadManifestAsync(GetLocalPath(_manifestName));
            _remoteManifest = await LoadRemoteManifestAsync();
        }

        async UniTask<int> LoadRemoteVersionAsync()
        {
            if (string.IsNullOrEmpty(_remoteUrl))
            {
                return -1;
            }

            using (var req = CreateWebRequest($"{_remoteUrl}/version", UnityWebRequest.Get))
            {
                req.timeout = _runtimeSettings.timeout;
                await req.SendWebRequest();
                
                if (req.isNetworkError || req.isHttpError ||
                    !int.TryParse(req.downloadHandler.text, out int remoteVersion))
                {
                    return -1;
                }

                return remoteVersion;
            }
        }

        async UniTask<AssetBundleManifest> LoadManifestAsync(string url, Func<string, UnityWebRequest> createFn)
        {
             AssetBundle ab;
             using (var req = CreateWebRequest(url, createFn))
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

        UniTask<AssetBundleManifest> LoadManifestAsync(string url)
        {
            return LoadManifestAsync(url, UnityWebRequestAssetBundle.GetAssetBundle);
        }

        UniTask<AssetBundleManifest> LoadManifestAsync(string url, int version)
        {
            return LoadManifestAsync(url, s => UnityWebRequestAssetBundle.GetAssetBundle(s, (uint) version, 0));
        }

        internal async UniTask<AssetBundleManifest> LoadRemoteManifestAsync()
        {
            if (string.IsNullOrEmpty(_remoteUrl))
            {
                return _localManifest;
            }

            int localVersion = version;
            int remoteVersion = await LoadRemoteVersionAsync();
            var remoteManifest = await LoadManifestAsync(GetRemoteAbUrl(_manifestName), Mathf.Max(localVersion, remoteVersion));
            // 如果发生错误加载远程manifest失败，直接将其设置为localmanifest
            if (remoteManifest == null)
            {
                remoteManifest = _localManifest;
            }
            else if (remoteVersion > localVersion)
            {
                // 只有成功加载远端manifest时才更新本地version值
                PlayerPrefs.SetInt(VERSION_KEY, remoteVersion);
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

        async UniTask<AssetBundle> CreateLoadAssetBundleTask(string name, IProgress<float> progress, CancellationToken token)
        {
            UnityWebRequest webRequest = CreateLoadAssetBundleReq(name);
            //todo 原来用的 configureAwait在首次调用时不会上报Progress，也许升级unitytask版本试试
            using (var request = await webRequest.SendWebRequest().WaitUntilDone(progress, token))
            {
                AssetBundle ab;

                if (request.isHttpError ||
                    request.isNetworkError ||
                    (ab = DownloadHandlerAssetBundle.GetContent(request)) == null)
                {
                    // 发生错误加载缓存的最新版本，没有再抛异常
                    var hash = GetCachedVersionRecently(name);
                    if (hash == null)
                    {
                        throw new Exception($"Load {name} {nameof(request)} {request.error}");
                    }

                    var newReq = await CreateWebRequest(request.url, s =>
                            UnityWebRequestAssetBundle.GetAssetBundle(s, hash.Value))
                        .SendWebRequest()
                        .WaitUntilDone(progress, token);
                    ab = DownloadHandlerAssetBundle.GetContent(newReq);
                }

                return ab;
            }
        }

        // todo 最开始加载的task完成后立即unload，会导致task取出的ab为null
        async UniTask<T> WaitTask<T>(UniTask<T> task, IProgress<float> progress, CancellationToken token)
        {
            while (task.Status != AwaiterStatus.Succeeded)
            {
                if (task.Status == AwaiterStatus.Canceled || task.Status == AwaiterStatus.Faulted)
                {
                    throw new OperationCanceledException();
                }
                token.ThrowIfCancellationRequested();
                await UniTask.DelayFrame(1, cancellationToken: token);
            }
            progress?.Report(1);
            return task.Result;
        }

        async UniTask<AssetBundle> LoadAssetBundleAsync(string name, IProgress<float> progress, CancellationToken token)
        {
            if (_abRefs.TryGetValue(name, out var abRef))
            {
                progress?.Report(1);
                return abRef.GetValue();
            }

            if (_abLoadingTasks.TryGetValue(name, out var task))
            {
                task = WaitTask(task, progress, token);
            }
            else
            {
                var cancellationTokenRegistration = token.Register(() => _abLoadingTasks.Remove(name));
                task = CreateLoadAssetBundleTask(name, progress, token);
                task.ContinueWith(_ => cancellationTokenRegistration.Dispose());
                _abLoadingTasks[name] = task;
            }

            AssetBundle ab;
            try
            {
                ab = await task;
            }
            finally
            {
                _abLoadingTasks.Remove(name);
            }

            if (!_abRefs.TryGetValue(name, out abRef))
            {
                abRef = CreateSharedRef(ab);
                _abRefs[name] = abRef;
            }
            return abRef.GetValue();
        }

        // todo 重构缩减重复代码
        UnityWebRequest CreateLoadAssetBundleReq(string name)
        {
            string url = string.Empty;
            Hash128 hash;

            // 找不到的是增量更新的AssetBundle，直接远程加载处理，因为只可能在远端
            if (_runtimeSettings.name2BundleDic.TryGetValue(name, out var bundle))
            {
                switch (bundle.type)
                {
                    case BundleType.Static:
                        url = GetLocalPath(name);
                        hash = _localManifest.GetAssetBundleHash(name);
                        break;
                    case BundleType.Patchable:
                        var localHash = _localManifest.GetAssetBundleHash(name);
                        if (!Caching.IsVersionCached(GetLocalPath(name), localHash))
                        {
                            url = GetLocalPath(name);
                            hash = localHash;
                            break;
                        }

                        url = GetRemoteAbUrl(name);
                        hash = _remoteManifest.GetAssetBundleHash(name);
                        break;
                    case BundleType.Remote:
                        url = GetRemoteAbUrl(name);
                        hash = _remoteManifest.GetAssetBundleHash(name);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                url = GetRemoteAbUrl(name);
                hash = _remoteManifest.GetAssetBundleHash(name);
            }

            var req = CreateWebRequest(url, s => 
                UnityWebRequestAssetBundle.GetAssetBundle(s, hash));
            req.timeout = _runtimeSettings.timeout;
            return req;
        }

        public override async UniTask<IAssetBundle> LoadAsync(string name, IProgress<float> progress, CancellationToken token)
        {
            if (_localManifest == null || _remoteManifest == null)
            {
                if (_initTask == null)
                {
                    _initTask = InitAsync();
                }
                await _initTask.Value;
                _initTask = null;
            }

            string[] dependencies = GetAllDependencies(name);
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

        string[] GetAllDependencies(string abName)
        {
            AssetBundleManifest manifest = _runtimeSettings.name2BundleDic.TryGetValue(abName, out var bundle) && bundle.type == BundleType.Static
                    ? _localManifest
                    : _remoteManifest;
            return manifest.GetAllDependencies(abName);
        }

        void Unload(AssetBundle assetBundle, bool unloadAllLoadedObjects)
        {
            string[] dependencies = GetAllDependencies(assetBundle.name);
            foreach (string dependency in dependencies)
            {
                DisposeAssetBundle(dependency, unloadAllLoadedObjects);
            }

            DisposeAssetBundle(assetBundle.name, unloadAllLoadedObjects);
        }

        void DisposeAssetBundle(string abName, bool unloadAllLoadedObjects)
        {
            if (!_abRefs.TryGetValue(abName, out var sharedReference))
            {
                return;
            }
            
            sharedReference.Dispose(unloadAllLoadedObjects);
        }

        string GetRemoteAbUrl(string name)
        {
#if UNITY_EDITOR
            // 编辑器模式下如果没有启用HTTPService或者设置cdn url，就直接从本地cache中加载
            if (string.IsNullOrEmpty(_remoteUrl))
            {
                return GetLocalPath(name);
            }
#endif
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

        public override Hash128? GetCachedVersionRecently(string abName)
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

        UnityWebRequest CreateWebRequest(string url, Func<string, UnityWebRequest> createFn)
        {
            if (_runtimeSettings.webRequestProcessor == null)
            {
                return createFn(url);
            }

            url = _runtimeSettings.webRequestProcessor.HandleUrl(url);
            return _runtimeSettings.webRequestProcessor.HandleRequest(createFn(url));
        }

        public override int version => PlayerPrefs.GetInt(VERSION_KEY, _runtimeSettings.version);
    }
}