using System;
using UniRx.Async;
using UnityEngine;

namespace EasyAssetBundle
{
    [Serializable]
    public class AssetBundleReference
    {
        [SerializeField]
        string _guid;
        
        public async UniTask<IAssetBundle> LoadAsync()
        {
            return await AssetBundleLoader.instance.LoadByGuidAsync(_guid);
        }
    }
}