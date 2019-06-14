using UnityEditor;
using UnityEngine;

namespace EasyAssetBundle.Editor
{
    [CustomPropertyDrawer(typeof(AssetBundleReference))]
    public class AssetBundleReferenceDrawer : AbstractDrawer
    {
        SerializedProperty _name;

        protected override string AbName => _name.stringValue;
        protected override string AssetName => string.Empty;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _name = property.FindPropertyRelative("_name");
            base.OnGUI(position, property, label);
        }

        protected override string GetValue()
        {
            return _name.stringValue;
        }

        protected override void UpdateValue(Object obj, string abName, string varName)
        {
            _name.stringValue = string.IsNullOrEmpty(varName) ? abName : $"{abName}.{varName}";
        }
    }
}