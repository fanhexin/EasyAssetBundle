#if UNITY_EDITOR
using System;
using System.Threading;
using EasyAssetBundle.Common;
using UniRx.Async;
using UnityEngine;

namespace EasyAssetBundle
{
    internal class VirtualAssetBundleLoader : BaseAssetBundleLoader
    {
        public VirtualAssetBundleLoader(RuntimeSettings runtimeSettings)
            : base(runtimeSettings)
        {
        }

        public override UniTask InitAsync()
        {
            return UniTask.CompletedTask;
        }

        public override async UniTask<IAssetBundle> LoadAsync(string name, IProgress<float> progress, CancellationToken token)
        {
            progress?.Report(1);
            return new VirtualAssetBundle(name);    
        }

        public override Hash128? GetCachedVersionRecently(string abName)
        {
            return new Hash128();
        }
    }
}
#endif