using System;
using System.Linq;
using EasyAssetBundle.Common.Editor;
using UnityEditor;
using UnityEngine;

namespace EasyAssetBundle.Editor
{
    [CustomPropertyDrawer(typeof(AssetBundleNameAttribute))]
    public class AbNameDrawer : PropertyDrawer
    {
        private string[] _options;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = attribute as AssetBundleNameAttribute;
            _options = _options ?? Settings.instance.runtimeSettings.bundles
                .Select(x => x.name)
                .Where(x => x.Contains(attr.filter))
                .ToArray();

            int index = Mathf.Max(0, Array.IndexOf(_options, property.stringValue));
            EditorGUI.BeginChangeCheck();
            index = EditorGUI.Popup(position, property.displayName, index, _options);
            if (EditorGUI.EndChangeCheck())
            {
                property.stringValue = _options[index];
                property.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}