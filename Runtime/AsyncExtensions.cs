using System;
using UnityEngine;

namespace EasyAssetBundle
{
    public static class AsyncExtensions
    {
        public static AssetBundleRequestAwaiter GetAwaiter(this AssetBundleRequest asyncOperation)
        {
            if (asyncOperation == null)
            {
                throw new NullReferenceException();
            }
            return new AssetBundleRequestAwaiter(asyncOperation);
        }
    }
}