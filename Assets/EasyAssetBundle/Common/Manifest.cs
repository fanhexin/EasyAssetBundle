using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyAssetBundle.Common
{
    [Serializable]
    public class Manifest : ISerializationCallbackReceiver
    {
        [SerializeField] private int _version = 1;
        [SerializeField] private string _cdnUrl;
        [SerializeField] private Bundle[] _bundles;

        public int version => _version;
        public string cdnUrl => _cdnUrl;
        public IReadOnlyList<Bundle> bundles => _bundles;
        
        public IReadOnlyDictionary<string, Bundle> guid2BundleDic { get; private set; }
        public IReadOnlyDictionary<string, Bundle> name2BundleDic { get; private set; }
        
        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            guid2BundleDic = _bundles.ToDictionary(x => x.guid);
            name2BundleDic = _bundles.ToDictionary(x => x.name);
        }

        public void Init(Manifest manifest)
        {
            _version = manifest._version;
            _cdnUrl = manifest._cdnUrl;
            _bundles = manifest._bundles;
        }
    }
}