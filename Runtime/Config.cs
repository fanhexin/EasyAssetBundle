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

        [SerializeField] private RuntimeSettings _runtimeSettings;

        public RuntimeSettings runtimeSettings
        {
            get => _runtimeSettings;
#if UNITY_EDITOR
            set => _runtimeSettings = value;
#endif
        }

        public static string streamingAssetsBundlePath =>
            Path.Combine(Application.streamingAssetsPath, Application.platform.ToGenericName());
    }
}