using System;
using UnityEditor;
using UnityEngine;

namespace EasyAssetBundle.Editor
{
    public abstract class PopupDrawer : PropertyDrawer
    {
        string[] _displayedOptions;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _displayedOptions = _displayedOptions ?? CreateDisplayedOptions();
            
            int index = Mathf.Max(0, Array.IndexOf(_displayedOptions, property.stringValue));
            using (var ccs = new EditorGUI.ChangeCheckScope())
            {
                index = EditorGUI.Popup(position, property.displayName, index, _displayedOptions);
                if (ccs.changed)
                {
                    property.stringValue = _displayedOptions[index];
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        protected abstract string[] CreateDisplayedOptions();
    }
}