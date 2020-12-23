using UnityEditor;
using UnityEngine;

namespace EncryptionProcessor.Editor
{
    [CustomEditor(typeof(EncryptionBuildProcessor))]
    public class EncryptionBuildProcessorInspector : UnityEditor.Editor
    {
        EncryptionBuildProcessor _target;

        void OnEnable()
        {
            _target = target as EncryptionBuildProcessor;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button(nameof(_target.OnBeforeBuild)))
            {
                _target.OnBeforeBuild();    
            }

            if (GUILayout.Button(nameof(_target.OnAfterBuild)))
            {
                _target.OnAfterBuild();    
            }

            if (GUILayout.Button(nameof(_target.OnCancelBuild)))
            {
                _target.OnCancelBuild(); 
            }
        }
    }
}