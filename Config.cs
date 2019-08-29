#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace EasyAssetBundle
{
    public class Config : ScriptableObject
    {
#if UNITY_EDITOR
        public static Config instance
        {
            get
            {
                var config = Resources.Load<Config>("EasyAssetBundleSettings");
                if (config == null)
                {
                    config = CreateInstance<Config>();
                    if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    {
                        AssetDatabase.CreateFolder("Assets", "Resources");
                    }
                    AssetDatabase.CreateAsset(config, "Assets/Resources/EasyAssetBundleSettings.asset");
                    AssetDatabase.Refresh();
                }

                return config;
            }
        }
        
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
#endif

        [SerializeField] string _manifestName = "Manifest";

        public string manifestName => _manifestName;
    }
}