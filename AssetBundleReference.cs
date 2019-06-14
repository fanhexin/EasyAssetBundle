using System;
using UniRx.Async;
using UnityEngine;

namespace EasyAssetBundle
{
    [Serializable]
    public class AssetBundleReference
    {
        [SerializeField]
        string _name;
        
        public async UniTask<IAssetBundle> LoadAsync()
        {
            return await AssetBundleLoader.instance.LoadAsync(_name);
        }

        public IAssetBundle Load()
        {
            return AssetBundleLoader.instance.Load(_name);
        }
    }
}