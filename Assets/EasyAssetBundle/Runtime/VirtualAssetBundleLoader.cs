#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EasyAssetBundle.Common;
using UniRx.Async;
using UnityEditor;
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

        public override IEnumerable<Hash128> GetCachedVersions(string abName)
        {
            return Enumerable.Empty<Hash128>();
        }

        public override bool Contains(string abName)
        {
            return AssetDatabase.GetAllAssetBundleNames().Contains(abName);
        }

        public override bool CheckForUpdates(string abName)
        {
            return false;
        }

        public override IEnumerable<string> CheckForUpdates()
        {
            return Enumerable.Empty<string>();
        }
    }
}
#endif