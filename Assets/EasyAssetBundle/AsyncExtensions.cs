using System;
using UniRx.Async;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyAssetBundle
{
    public static class AsyncExtensions
    {
        public static AssetBundleCreateRequestAwaiter GetAwaiter(this AssetBundleCreateRequest asyncOperation)
        {
            if (asyncOperation == null)
            {
                throw new NullReferenceException();
            }
            return new AssetBundleCreateRequestAwaiter(asyncOperation);
        }
        
        public static UniTask<AssetBundle> ToUniTask(this AssetBundleCreateRequest asyncOperation)
        {
            if (asyncOperation == null)
            {
                throw new NullReferenceException();
            }
            return new UniTask<AssetBundle>(new AssetBundleCreateRequestAwaiter(asyncOperation));
        }

        public static AssetBundleRequestAwaiter GetAwaiter(this AssetBundleRequest asyncOperation)
        {
            if (asyncOperation == null)
            {
                throw new NullReferenceException();
            }
            return new AssetBundleRequestAwaiter(asyncOperation);
        }
        
        public static UniTask<Object> ToUniTask(this AssetBundleRequest asyncOperation)
        {
            if (asyncOperation == null)
            {
                throw new NullReferenceException();
            }
            return new UniTask<Object>(new AssetBundleRequestAwaiter(asyncOperation));
        }
    }
}