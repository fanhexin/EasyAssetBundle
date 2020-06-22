using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyAssetBundle.Common
{
    [Serializable]
    public class RuntimeSettings : ISerializationCallbackReceiver
    {
        [SerializeField] int _version = 1;
        [SerializeField] string _cdnUrl;
        [SerializeField] int _timeout = 3;
        [SerializeField] string _encryptKey;
        [SerializeField] WebRequestProcessor _webRequestProcessor;
        [SerializeField, HideInInspector] Bundle[] _bundles;

        public int version => _version;
        public string cdnUrl => _cdnUrl;
        public int timeout => _timeout;
        public IReadOnlyList<Bundle> bundles => _bundles;
        public WebRequestProcessor webRequestProcessor => _webRequestProcessor;
        public string encryptKey => _encryptKey;
        
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

        public void Init(RuntimeSettings runtimeSettings)
        {
            _version = runtimeSettings._version;
            _cdnUrl = runtimeSettings._cdnUrl;
            _timeout = runtimeSettings._timeout;
            _webRequestProcessor = runtimeSettings._webRequestProcessor;
            _bundles = runtimeSettings._bundles;
            _encryptKey = runtimeSettings._encryptKey;
        }
    }
}