#if UNITY_EDITOR
using EasyAssetBundle.Common.Editor;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using EasyAssetBundle.Common;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;

[assembly:InternalsVisibleTo("Tests")]
namespace EasyAssetBundle
{
    // todo 添加获取更新文件大小的支持(做成可选配置)
    internal partial class RealAssetBundleLoader : BaseAssetBundleLoader
    {
        const string VERSION_KEY = "easyassetbundle_version";

        readonly string _basePath;
        readonly Dictionary<string, SharedReference<AssetBundle>> _abRefs =
            new Dictionary<string, SharedReference<AssetBundle>>();
        
        readonly Dictionary<string, UniTask<AssetBundle>> _abLoadingTasks = 
            new Dictionary<string, UniTask<AssetBundle>>();
        
        readonly string _remoteUrl;
        readonly string _manifestName;

        BaseAbManifest _remoteManifest;
        BaseAbManifest _localManifest;
        UniTask? _initTask;

        public RealAssetBundleLoader(string basePath, RuntimeSettings runtimeSettings)
            : base(runtimeSettings)
        {
            _basePath = basePath;
            string platformName = Application.platform.ToGenericName();
            _manifestName = platformName;
            _remoteUrl = CreateRemoteUrl(runtimeSettings, platformName);
        }

        string CreateRemoteUrl(RuntimeSettings runtimeSettings, string platformName)
        {
#if UNITY_EDITOR
            if (Settings.instance.httpServiceSettings.enabled)
            {
                return $"{Settings.instance.simulateUrl}/{platformName}";
            }
#endif
            return string.IsNullOrEmpty(runtimeSettings.cdnUrl) ? string.Empty : $"{runtimeSettings.cdnUrl}/{platformName}";
        }

        public override async UniTask InitAsync()
        {
            _localManifest = await LoadLocalManifestAsync(GetLocalPath(_manifestName));
            _remoteManifest = await LoadRemoteManifestAsync();
        }

        protected virtual async UniTask<int> LoadRemoteVersionAsync()
        {
            if (string.IsNullOrEmpty(_remoteUrl))
            {
                return -1;
            }

            using (var req = CreateWebRequest($"{_remoteUrl}/version", UnityWebRequest.Get))
            {
                req.timeout = _runtimeSettings.loadVersionTimeout;
                await req.SendWebRequest();
                
                if (req.isNetworkError || req.isHttpError ||
                    !int.TryParse(req.downloadHandler.text, out int remoteVersion))
                {
                    return -1;
                }

                return remoteVersion;
            }
        }

        protected virtual async UniTask<BaseAbManifest> LoadManifestAsync(string url, int version, Func<string, UnityWebRequest> createFn)
        {
             AssetBundle ab;
             using (var req = CreateWebRequest(url, createFn))
             {
                 req.timeout = _runtimeSettings.loadManifestTimeout;
                 await req.SendWebRequest();
                 
                 if (req.isNetworkError || req.isHttpError)
                 {
                     return null;
                 }
 
                 ab = DownloadHandlerAssetBundle.GetContent(req);
             }
 
             var manifest = await ab.LoadAssetAsync<AssetBundleManifest>(nameof(AssetBundleManifest));
             ab.Unload(false);
             return new AbManifest(version, manifest as AssetBundleManifest);           
        }

        UniTask<BaseAbManifest> LoadLocalManifestAsync(string url)
        {
            return LoadManifestAsync(url, _runtimeSettings.version, UnityWebRequestAssetBundle.GetAssetBundle);
        }

        UniTask<BaseAbManifest> LoadManifestAsync(string url, int version)
        {
            return LoadManifestAsync(url, version, s => UnityWebRequestAssetBundle.GetAssetBundle(s, (uint) version, 0));
        }

        async UniTask<BaseAbManifest> LoadRemoteManifestAsync()
        {
            if (string.IsNullOrEmpty(_remoteUrl))
            {
                return _localManifest;
            }

            int localVersion = version;
            int remoteVersion = localVersion;
            try
            {
                remoteVersion = await LoadRemoteVersionAsync();
            }
            catch (Exception)
            {
            }
            
            if (remoteVersion <= _runtimeSettings.version)
            {
                return _localManifest;
            }

            BaseAbManifest remoteManifest = null;
            try
            {
                remoteManifest = await LoadManifestAsync(GetRemoteAbUrl(_manifestName), Mathf.Max(localVersion, remoteVersion));
            }
            catch (Exception) { }
            
            // 如果发生错误加载远程manifest失败，直接将其设置为localmanifest
            if (remoteManifest == null)
            {
                remoteManifest = _localManifest;
            }
            else if (remoteVersion > localVersion)
            {
                // 只有成功加载远端manifest时才更新本地version值
                SaveVersion(remoteVersion);
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

        async UniTask<AssetBundle> CreateLoadAssetBundleTask(string name, IProgress<float> progress, CancellationToken token, bool exceptionFallback = true)
        {
            var (bundleType, url, hash) = GetBundleInfo(name);
            //todo 原来用的 configureAwait在首次调用时不会上报Progress，也许升级unitytask版本试试
            AssetBundle ab;
            try
            {
                ab = await LoadAssetBundleAsync(url, hash, progress, token);
            }
            catch (OperationCanceledException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                if (!exceptionFallback)
                {
                    throw e;
                }
                
                var newHash = GetCachedVersionRecently(name);
                if (newHash == null)
                {
                    // patchable第一次加载时也尝试从远端加载，出现异常加载保内版本
                    if (bundleType == BundleType.Patchable)
                    {
                        newHash = _localManifest.GetAssetBundleHash(name);
                        url = GetLocalPath(name);
                    }
                    else
                    {
                        throw e;
                    }
                }

                return await LoadAssetBundleAsync(url, newHash.Value, progress, token);
            }

            return ab;
        }

        protected virtual async UniTask<AssetBundle> LoadAssetBundleAsync(string url, 
            Hash128 hash, 
            IProgress<float> progress,
            CancellationToken token)
        {
            var req = CreateWebRequest(url, s => UnityWebRequestAssetBundle.GetAssetBundle(s, hash));
            req.timeout = _runtimeSettings.timeout;
            using (var request = await req.SendWebRequest().WaitUntilDone(progress, token))
            {
                if (request.isHttpError || request.isNetworkError)
                {
                    throw new UnityWebRequestException(request.error, request.url);
                }

                AssetBundle ab = DownloadHandlerAssetBundle.GetContent(request);
                if (ab == null)
                {
                    throw new Exception($"{nameof(DownloadHandlerAssetBundle.GetContent)} return null!");
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

        async UniTask<AssetBundle> LoadAssetBundleAsync(string name, IProgress<float> progress, CancellationToken token, bool exceptionFallback = true)
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
                task = CreateLoadAssetBundleTask(name, progress, token, exceptionFallback);
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

        (BundleType, string, Hash128) GetBundleInfo(string name)
        {
            string url = string.Empty;
            Hash128 hash;
            BundleType bundleType;

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
                        var remoteHash = _remoteManifest.GetAssetBundleHash(name);
                        url = localHash == remoteHash ? GetLocalPath(name) : GetRemoteAbUrl(name);
                        hash = remoteHash;
                        break;
                    case BundleType.Remote:
                        url = GetRemoteAbUrl(name);
                        hash = _remoteManifest.GetAssetBundleHash(name);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                bundleType = bundle.type;
            }
            else
            {
                url = GetRemoteAbUrl(name);
                hash = _remoteManifest.GetAssetBundleHash(name);
                bundleType = BundleType.Remote;
            }

            return (bundleType, url, hash);
        }

        public override async UniTask<IAssetBundle> LoadAsync(string name, IProgress<float> progress, CancellationToken token, bool exceptionFallback = true)
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
                var ab = await LoadAssetBundleAsync(name, progress, token, exceptionFallback);
                return new RealAssetBundle(this, ab);
            }
        }

        string[] GetAllDependencies(string abName)
        {
            BaseAbManifest manifest = _runtimeSettings.name2BundleDic.TryGetValue(abName, out var bundle) && bundle.type == BundleType.Static
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

        public override IEnumerable<Hash128> GetCachedVersions(string abName)
        {
            string path = Path.Combine(Caching.defaultCache.path, abName);
            if (!Directory.Exists(path))
            {
                return Enumerable.Empty<Hash128>();
            }

            return Directory.GetDirectories(path)
                .OrderByDescending(Directory.GetCreationTime)
                .Select(x => Hash128.Parse(Path.GetFileNameWithoutExtension(x)));
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

        protected virtual void SaveVersion(int v)
        {
            PlayerPrefs.SetInt(VERSION_KEY, v);
        }

        public override bool Contains(string abName)
        {
            return _remoteManifest.Contains(abName);
        }

        public override bool CheckForUpdates(string abName)
        {
            Hash128 hash = _remoteManifest.GetAssetBundleHash(abName);
            return hash.isValid && hash != GetCachedVersionRecently(abName);
        }

        public override IEnumerable<string> CheckForUpdates()
        {
            return _remoteManifest.Where(CheckForUpdates);
        }
    }
}