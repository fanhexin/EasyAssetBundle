#if UNITY_EDITOR
using EasyAssetBundle.Common.Editor;
using UnityEditor;
#endif
using System;
using System.Threading;
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
    }
}