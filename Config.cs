using UnityEditor;
using UnityEngine;

namespace EasyAssetBundle
{
    public class Config : ScriptableObject
    {
        public static Config instance { get; private set; }
        
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
#endif

        [SerializeField] string _manifestName = "Manifest";

        public string manifestName => _manifestName;
        protected Config()
        {
            instance = this;
        }
    }
}