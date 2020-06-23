using EasyAssetBundle.Common.Editor;
using UnityEditor;
using UnityEngine;

namespace EasyAssetBundle.Editor
{
    public class SettingsWindow : EditorWindow
    {
        Settings _settings;

        public static SettingsWindow Display()
        {
            var win = CreateInstance<SettingsWindow>();
            win.titleContent = new GUIContent("EasyAssetBundleSettings");
            win.ShowUtility();
            return win;
        }

        void OnEnable()
        {
            _settings = Settings.instance;
        }

        void OnGUI()
        {
            _settings.OnGUI();
        }
    }
}