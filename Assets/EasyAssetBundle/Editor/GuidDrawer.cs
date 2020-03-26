using System.Linq;
using EasyAssetBundle.Common;
using EasyAssetBundle.Common.Editor;
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
                if (Settings.instance.runtimeSettings.guid2BundleDic.TryGetValue(_guid.stringValue, out Bundle bundle))
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
            var bundle = Settings.instance.runtimeSettings.bundles.FirstOrDefault(x => x.name == abName);
            if (bundle == null)
            {
                var so = new SerializedObject(Settings.instance);
                var bundles = Settings.GetBundlesSp(so);
                _guid.stringValue = bundles.AddBundle(obj);
                MainWindow.instance?.Reload();
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