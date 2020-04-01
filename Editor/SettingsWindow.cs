using EasyAssetBundle.Common.Editor;
using UnityEditor;
using UnityEngine;

namespace EasyAssetBundle.Editor
{
    public class SettingsWindow : EditorWindow
    {
        private Settings _settings;

        public static SettingsWindow Display()
        {
            var win = GetWindow<SettingsWindow>();
            win.titleContent = new GUIContent("EasyAssetBundleSettings");
            win.Show();
            return win;
        }

        private void OnEnable()
        {
            _settings = Settings.instance;
        }

        private void OnGUI()
        {
            _settings.OnGUI();
        }
    }
}