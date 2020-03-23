#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using UnityEngine;
using System.IO;

namespace EasyAssetBundle
{
    public class Config : ScriptableObject
    {
        public const string FILE_NAME = "EasyAssetBundleSettings";
        public static Config instance
        {
            get
            {
                var config = Resources.Load<Config>(FILE_NAME);
#if UNITY_EDITOR
                if (config == null)
                {
                    config = CreateInstance<Config>();
                    string resPath = "Assets/Resources";
                    Directory.CreateDirectory(resPath);
                    AssetDatabase.CreateAsset(config, Path.Combine(resPath, $"{FILE_NAME}.asset"));
                    AssetDatabase.Refresh();
                }
#endif

                return config;
            }
        }
        
        [SerializeField] private int _version;
        [SerializeField] private string _remoteUrl;
        [SerializeField] private Bundle[] _bundles;

        public static string streamingAssetsBundlePath =>
            Path.Combine(Application.streamingAssetsPath, Application.platform.ToGenericName());
        
#if UNITY_EDITOR
        public const string MODE_SAVE_KEY = "easy_asset_bundle_mode";
        public enum Mode
        {
            Virtual,    
            Real
        }
        
        public Mode mode
        {
            get => (Mode) EditorPrefs.GetInt(MODE_SAVE_KEY, 0);
            set => EditorPrefs.SetInt(MODE_SAVE_KEY, (int) value);
        }

        public const string BUILD_OPTIONS_SAVE_KEY = "easy_asset_bundle_build_options";
        public BuildAssetBundleOptions buildOptions
        {
            get => (BuildAssetBundleOptions) EditorPrefs.GetInt(BUILD_OPTIONS_SAVE_KEY,
                    (int) BuildAssetBundleOptions.ChunkBasedCompression);
            set => EditorPrefs.SetInt(BUILD_OPTIONS_SAVE_KEY, (int)value);
        }

        public static string cacheBasePath => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Library",
            "EasyAssetBundleCache");
        
        public static string currentTargetCachePath =>
            Path.Combine(cacheBasePath , EditorUserBuildSettings.activeBuildTarget.ToString());
#endif
        
        [Serializable]
        public class Bundle
        {
            [SerializeField] private string _name;
            [SerializeField] private BundleType _type;

            public string name => _name;
            public BundleType type => _type;
        }
    }
}