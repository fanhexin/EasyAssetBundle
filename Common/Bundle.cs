using System;
using UnityEngine;

namespace EasyAssetBundle.Common
{
    [Serializable]
    public class Bundle
    {
        [SerializeField] private string _guid;
        [SerializeField] private string _name;
        [SerializeField] private BundleType _type;

        public string guid => _guid;
        public string name => _name;
        public BundleType type => _type;
    }
}