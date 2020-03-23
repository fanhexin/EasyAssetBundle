using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyAssetBundle.Editor
{
    public class SettingsWindow : EditorWindow
    {
        private static readonly GUIContent _contentBuild = new GUIContent("Build");

        Config _config;

        [MenuItem("Window/EasyAssetBundle")]
        static void Init()
        {
            var win = GetWindow<SettingsWindow>();
            win.titleContent = new GUIContent("EasyAssetBundle");
            win.Show();
        }

        void OnEnable()
        {
            _config = Config.instance;
        }

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true));

            _config.buildOptions = (BuildAssetBundleOptions) EditorGUILayout.EnumFlagsField("Build Options",
                _config.buildOptions, EditorStyles.toolbarPopup);

            EnumDropDownButton(new GUIContent($"Mode: {_config.mode}"), _config.mode, mode =>
            {
                if (mode == Config.Mode.Real && !AssetBundleBuilder.hasBuilded)
                {
                    ShowNotification(new GUIContent("Please build assetbundle first!"));
                }
                else
                {
                    _config.mode = mode;
                }
            });

            IEnumerable<AbstractBuildProcessor> processors = AssetBundleBuilder.GetProcessors();
            if (processors.Any())
            {
                DropdownMenuButton(new GUIContent($"Processors({processors.Count()})"), menu =>
                {
                    foreach (var processor in processors)
                    {
                        menu.AddItem(new GUIContent(processor.name), false,
                            item => EditorGUIUtility.PingObject(item as Object), processor);
                    }
                });
            }

            DropdownMenuButton(_contentBuild, menu =>
            {
                menu.AddItem(new GUIContent("Build Content"), false,
                    () => AssetBundleBuilder.Build(_config.buildOptions, processors));
                menu.AddItem(new GUIContent("Try Build"), false,
                    () => AssetBundleBuilder.Build(_config.buildOptions | BuildAssetBundleOptions.DryRunBuild,
                        processors));
                menu.AddItem(new GUIContent("Clear Cache"), false, AssetBundleBuilder.ClearCache);
            });

            EditorGUILayout.EndHorizontal();
        }

        void DropdownMenuButton(GUIContent label, Action<GenericMenu> addMenuItems)
        {
            Rect rect = GUILayoutUtility.GetRect(label, EditorStyles.toolbarDropDown, GUILayout.ExpandWidth(false));
            if (!EditorGUI.DropdownButton(rect, label, FocusType.Passive, EditorStyles.toolbarDropDown))
            {
                return;
            }

            var menu = new GenericMenu();
            addMenuItems?.Invoke(menu);
            menu.DropDown(rect);
        }

        void EnumDropDownButton<T>(GUIContent label, T value, Action<T> onValueChanged) where T : Enum
        {
            Rect rect = GUILayoutUtility.GetRect(label, EditorStyles.toolbarDropDown, GUILayout.ExpandWidth(false));
            if (!EditorGUI.DropdownButton(rect, label, FocusType.Passive, EditorStyles.toolbarDropDown))
            {
                return;
            }

            var menu = new GenericMenu();
            foreach (string name in Enum.GetNames(typeof(T)))
            {
                menu.AddItem(new GUIContent(name), value.ToString() == name, v =>
                {
                    T ret = (T) Enum.Parse(typeof(T), (string) v);
                    if (!value.Equals(ret))
                    {
                        onValueChanged?.Invoke(ret);
                    }
                }, name);
            }

            menu.DropDown(rect);
        }
    }
}