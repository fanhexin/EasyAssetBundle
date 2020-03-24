using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyAssetBundle.Editor
{
    public abstract class GuidDrawer : AbstractDrawer
    {
        private SerializedProperty _guid;
        private SerializedProperty _assetName;

        protected override string AbName
        {
            get
            {
                if (Config.instance.guid2Bundle.TryGetValue(_guid.stringValue, out Config.Bundle bundle))
                {
                    return bundle.name;
                }

                return string.Empty;
            }
        }

        protected override string AssetName => _assetName?.stringValue;
        protected override string GetViewText()
        {
            return _assetName == null ? AbName : $"{AbName}->{AssetName}";
        }

        protected override void UpdateValue(Object obj, string abName, string varName)
        {
            var bundle = Config.instance.bundles.FirstOrDefault(x => x.name == abName);
            if (bundle == null)
            {
                var so = new SerializedObject(Config.instance);
                var bundles = Config.instance.GetBundlesSp(so);
                _guid.stringValue = bundles.AddBundle(obj);
                SettingsWindow.instance?.Reload();
            }
            else
            {
                _guid.stringValue = bundle.guid;
            }
            
            if (_assetName != null)
                _assetName.stringValue = obj.name;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _guid = property.FindPropertyRelative(_guidKeyName);
            _assetName = property.FindPropertyRelative(_assetKeyName);
            base.OnGUI(position, property, label);
        }
        
        protected abstract string _guidKeyName { get; }
        protected abstract string _assetKeyName { get; }
    }
}