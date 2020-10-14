#if UNITY_EDITOR
using EasyAssetBundle.Common.Editor;
using UnityEditor;
#endif
using System;
using System.IO;
using System.Threading;
using EasyAssetBundle.Common;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;

namespace EasyAssetBundle
{
    public static class Extensions
    {
        public static string ToGenericName(this RuntimePlatform platform)
        {
#if UNITY_EDITOR
            return EditorUserBuildSettings.activeBuildTarget.ToString();
#else
            switch (platform)
            {
                case RuntimePlatform.IPhonePlayer:
                    return "iOS";
                default:
                    return platform.ToString();
            }
#endif
        }

        public static async UniTask<UnityWebRequest> WaitUntilDone(this UnityWebRequestAsyncOperation operation,
            IProgress<float> progress = null,
            CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
            {
                operation.webRequest.Abort();
                return operation.webRequest;
            }
            
            using (token.Register(operation.webRequest.Abort))
            {
                while (!operation.isDone)
                {
                    if (token.IsCancellationRequested)
                    {
                        return operation.webRequest;
                    }
                    
                    progress?.Report(operation.progress);
                    await UniTask.DelayFrame(1, cancellationToken: token);
                }
            }
            progress?.Report(1);
            return operation.webRequest;
        }

//         static string GetRemoteBaseUrl(this RuntimeSettings runtimeSettings)
//         {
// #if UNITY_EDITOR
//             string platformName = Application.platform.ToGenericName();
//             if (Settings.instance.httpServiceSettings.enabled)
//             {
//                 return $"{Settings.instance.simulateUrl}/{platformName}";
//             }
//
//             return string.IsNullOrEmpty(runtimeSettings.cdnUrl) ? string.Empty : $"{runtimeSettings.cdnUrl}/{platformName}";
// #else
//             return string.IsNullOrEmpty(runtimeSettings.cdnUrl) ? string.Empty : $"{runtimeSettings.cdnUrl}/{platformName}";
// #endif
//         }
//
//         public static string GetLocalUrl(string name)
//         {
//             string basePath;
// #if UNITY_EDITOR
//             basePath = Settings.currentTargetCachePath;
// #else
//             basePath = Config.streamingAssetsBundlePath;
// #endif
//             
//             string path = Path.Combine(basePath, name);
// #if UNITY_EDITOR || UNITY_IOS
//             path = $"file://{path}";
// #endif
//             return path;
//         }
//
//         public static string GetRemoteUrl(this RuntimeSettings runtimeSettings, string name)
//         {
//             string baseUrl = runtimeSettings.GetRemoteBaseUrl();
//             string url = $"{baseUrl}/{name}";
//             
// #if UNITY_EDITOR
//             // 编辑器模式下如果没有启用HTTPService或者设置cdn url，就直接从本地cache中加载
//             if (string.IsNullOrEmpty(baseUrl))
//             {
//                 return GetLocalUrl(name);
//             }
//             
//             if (Settings.instance.httpServiceSettings.enabled)
//             {
//                 return url;
//             }
// #endif
//             
//             if (!string.IsNullOrEmpty(runtimeSettings.cdnUrl) &&
//                 runtimeSettings.webRequestProcessor != null)
//             {
//                 url = runtimeSettings.webRequestProcessor.HandleUrl(url);
//             }
//             return url;
//         }
     }
}