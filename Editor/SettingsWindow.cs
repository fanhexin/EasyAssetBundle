using EasyAssetBundle.Common.Editor;
using UnityEditor;

namespace EasyAssetBundle.Editor
{
    public class SettingsWindow : EditorWindow
    {
        private Settings _settings;

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