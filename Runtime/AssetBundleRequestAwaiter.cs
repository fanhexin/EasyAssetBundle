using System;
using UniRx.Async;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyAssetBundle
{
    public struct AssetBundleRequestAwaiter : IAwaiter<Object>
    {
        readonly AssetBundleRequest _asyncOperation;
        UnityAsyncExtensions.AsyncOperationAwaiter _asyncOperationAwaiter;
        Object _result;
        
        public AssetBundleRequestAwaiter(AssetBundleRequest asyncOperation)
        {
            _asyncOperation = asyncOperation;
            _asyncOperationAwaiter = new UnityAsyncExtensions.AsyncOperationAwaiter(asyncOperation);    
            _result = _asyncOperationAwaiter.Status.IsCompletedSuccessfully() ? asyncOperation.asset : null;
        }
        
        public void OnCompleted(Action continuation) => _asyncOperationAwaiter.OnCompleted(continuation);
        public void UnsafeOnCompleted(Action continuation) => _asyncOperationAwaiter.UnsafeOnCompleted(continuation);
        public AwaiterStatus Status => _asyncOperationAwaiter.Status;
        public bool IsCompleted => _asyncOperationAwaiter.IsCompleted;
        public Object GetResult()
        {
            if (_asyncOperationAwaiter.Status == AwaiterStatus.Succeeded)
            {
                return _result;
            }

            _asyncOperationAwaiter.GetResult();
            _result = _asyncOperation.asset;
            return _result;
        }

        void IAwaiter.GetResult() => GetResult();
    }
}