using UnityEngine;
using System.IO;
using EasyAssetBundle.Common;

namespace EasyAssetBundle
{
    public class Config : ScriptableObject
    {
        public const string FILE_NAME = "EasyAssetBundleSettings";
        
        private static Config _instance;
        public static Config instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<Config>(FILE_NAME);
                }

                return _instance;
            }
        }

        [SerializeField] private Manifest _manifest;

        public Manifest manifest
        {
            get => _manifest;
#if UNITY_EDITOR
            set => _manifest = value;
#endif
        }

        public static string streamingAssetsBundlePath =>
            Path.Combine(Application.streamingAssetsPath, Application.platform.ToGenericName());
    }
}