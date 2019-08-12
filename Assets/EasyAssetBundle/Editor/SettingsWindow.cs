using UnityEditor;

namespace EasyAssetBundle.Editor
{
    public class SettingsWindow : EditorWindow
    {
        Config _config;

        [MenuItem("Window/EasyAssetbundle/Settings")]
        static void Init()
        {
            GetWindow<SettingsWindow>().Show();
        }

        void OnEnable()
        {
            _config = Config.instance;
        }

        void OnGUI()
        {
            if (_config == null)
            {
                return;
            }

            var editor = UnityEditor.Editor.CreateEditor(_config);
            editor.OnInspectorGUI();
        }
    }
}