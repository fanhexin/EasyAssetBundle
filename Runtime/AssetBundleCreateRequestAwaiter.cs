using System;
using UniRx.Async;
using UnityEngine;

namespace EasyAssetBundle
{
    public struct AssetBundleCreateRequestAwaiter : IAwaiter<AssetBundle>
    {
        readonly AssetBundleCreateRequest _asyncOperation;
        UnityAsyncExtensions.AsyncOperationAwaiter _asyncOperationAwaiter;
        
        AssetBundle _result;
        
        public AssetBundleCreateRequestAwaiter(AssetBundleCreateRequest asyncOperation)
        {
            _asyncOperation = asyncOperation;
            _asyncOperationAwaiter = new UnityAsyncExtensions.AsyncOperationAwaiter(asyncOperation);
            _result = _asyncOperationAwaiter.Status.IsCompletedSuccessfully() ? asyncOperation.assetBundle : null;
        }
        
        public void OnCompleted(Action continuation) => _asyncOperationAwaiter.OnCompleted(continuation);
        public void UnsafeOnCompleted(Action continuation) => _asyncOperationAwaiter.UnsafeOnCompleted(continuation);

        public AwaiterStatus Status => _asyncOperationAwaiter.Status;
        public bool IsCompleted => _asyncOperationAwaiter.IsCompleted;
        public AssetBundle GetResult()
        {
            if (_asyncOperationAwaiter.Status == AwaiterStatus.Succeeded)
            {
                return _result;
            }

            _asyncOperationAwaiter.GetResult();
            _result = _asyncOperation.assetBundle;
            return _result;
        }

        void IAwaiter.GetResult() => GetResult();
    }
}