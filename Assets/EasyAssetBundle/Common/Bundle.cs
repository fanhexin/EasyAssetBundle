using System;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly:InternalsVisibleTo("Tests")]
namespace EasyAssetBundle.Common
{
    [Serializable]
    public class Bundle
    {
        [SerializeField] string _guid;
        [SerializeField] string _name;
        [SerializeField] BundleType _type;

        public string guid
        {
            get => _guid;
            internal set => _guid = value;
        }

        public string name
        {
            get => _name;
            internal set => _name = value;
        }

        public BundleType type
        {
            get => _type;
            internal set => _type = value;
        }

#if UNITY_EDITOR
        public static string nameOfGuide = nameof(_guid);
        public static string nameOfName = nameof(_name);
        public static string nameOfType = nameof(_type);
#endif
    }
}