using UnityEditor;
using UnityEngine;

namespace EasyAssetBundle.Editor
{
    [CustomPropertyDrawer(typeof(AssetReference))]
    public class AssetReferenceDrawer : AbstractDrawer
    {
        SerializedProperty _abName;
        SerializedProperty _assetName;

        protected override string AbName => _abName.stringValue;
        protected override string AssetName => _assetName.stringValue;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _abName = property.FindPropertyRelative("_abName");
            _assetName = property.FindPropertyRelative("_assetName");
            base.OnGUI(position, property, label);
        }

        protected override string GetValue()
        {
            return $"{_abName.stringValue}->{_assetName.stringValue}";
        }

        protected override void UpdateValue(Object obj, string abName, string varName)
        {
            _abName.stringValue = string.IsNullOrEmpty(varName) ? abName : $"{abName}.{varName}";
            _assetName.stringValue = obj.name;
        }
    }
}