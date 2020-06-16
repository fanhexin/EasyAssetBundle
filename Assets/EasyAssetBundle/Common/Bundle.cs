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
        
#if UNITY_EDITOR
        public static string nameOfGuide = nameof(_guid);
        public static string nameOfName = nameof(_name);
        public static string nameOfType = nameof(_type);
#endif
    }
}