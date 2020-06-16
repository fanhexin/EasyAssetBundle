using System.Linq;
using EasyAssetBundle.Common;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace EasyAssetBundle.Editor
{
    internal class BundleTreeViewItem : TreeViewItem
    {
        private readonly SerializedProperty _bundlesSp;

        private SerializedProperty model => _bundlesSp.GetArrayElementAtIndex(id - 1);
        public SerializedProperty typeSp => model.FindPropertyRelative(Bundle.nameOfType);

        public BundleType type
        {
            set
            {
                model.FindPropertyRelative(Bundle.nameOfType).enumValueIndex = (int) value;
                _bundlesSp.serializedObject.ApplyModifiedProperties();
            }
        }

        public BundleTreeViewItem(SerializedProperty bundlesSp)
        {
            _bundlesSp = bundlesSp;
        }

        public void Rename(string newName)
        {
            if (displayName == newName)
            {
                return;
            }

            string[] paths = AssetDatabase.GetAssetPathsFromAssetBundle(displayName);
            foreach (string path in paths)
            {
                var importer = AssetImporter.GetAtPath(path);
                importer.assetBundleName = newName;
            }
            
            AssetDatabase.RemoveAssetBundleName(displayName, false);

            var nameSp = model.FindPropertyRelative(Bundle.nameOfName);
            nameSp.stringValue = newName;
            _bundlesSp.serializedObject.ApplyModifiedProperties();
        }

        public void Delete()
        {
            if (children != null)
            {
                foreach (var item in children.Cast<BundleAssetTreeViewItem>())
                {
                    item.Delete();    
                }
            }
            
            _bundlesSp.RemoveBundle(id - 1);
        }
    }
}