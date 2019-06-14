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
        
        public Mode mode => (Mode)EditorPrefs.GetInt(MODE_SAVE_KEY, 0);
#endif

        [SerializeField] string _manifestName = "Manifest";

        public string manifestName => _manifestName;
        protected Config()
        {
            instance = this;
        }
    }
}