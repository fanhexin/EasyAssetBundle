using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EasyAssetBundle.Editor
{
    public abstract class AbstractDrawer : PropertyDrawer
    {
        protected abstract string AbName { get; }
        protected abstract string AssetName { get; }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            EditorGUI.HelpBox(position, GetValue(), MessageType.None);
            
            var dragArea = position;

            var ev = Event.current;
            
            switch (ev.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform when dragArea.Contains(ev.mousePosition):
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (ev.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (var obj in DragAndDrop.objectReferences)
                        {
                            if (obj == null)
                            {
                                break;
                            }

                            string assetPath = AssetDatabase.GetAssetPath(obj);
                            string abName = AssetDatabase.GetImplicitAssetBundleName(assetPath);
                            string varName = AssetDatabase.GetImplicitAssetBundleVariantName(assetPath);
                            if (string.IsNullOrEmpty(abName))
                            {
                                continue;
                            }

                            UpdateValue(obj, abName, varName);
                            property.serializedObject.ApplyModifiedProperties();
                        }
                    }
                    ev.Use();
                    break;
                }
                case EventType.MouseDown when !string.IsNullOrEmpty(AbName) && dragArea.Contains(ev.mousePosition):
                {
                    string[] paths = string.IsNullOrEmpty(AssetName)
                        ? AssetDatabase.GetAssetPathsFromAssetBundle(AbName)
                        : AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(AbName, AssetName);
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(paths.First()));
                    ev.Use();
                    break;
                }
            }
            
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();           
        }

        protected abstract string GetValue();

        protected abstract void UpdateValue(Object obj, string abName, string varName);
    }
}