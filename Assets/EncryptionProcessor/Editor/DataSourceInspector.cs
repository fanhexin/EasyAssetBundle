using UnityEditor;
using UnityEngine;

namespace EncryptionProcessor.Editor
{
    [CustomEditor(typeof(DataSource), true)]
    public class DataSourceInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            if (GUILayout.Button("Print Data"))
            {
                foreach (string path in target as DataSource)
                {
                    Debug.Log(path);    
                }
            }
        }
    }
}