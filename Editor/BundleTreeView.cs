using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyAssetBundle.Editor
{
    public class BundleTreeView : TreeView
    {
        private readonly SerializedProperty _bundlesSp;
        private readonly Func<TreeViewItem, IComparable>[] _itemFieldGetters;
        private readonly Action<Rect, TreeViewItem>[] _columnRenders;

        public BundleTreeView(TreeViewState state, SerializedProperty bundlesSp)
            :this(CreateColumnHeader(), state, bundlesSp)
        {
            
        }

        private BundleTreeView(MultiColumnHeader header, TreeViewState state, SerializedProperty bundlesSp) 
            : base(state, header)
        {
            Undo.undoRedoPerformed += UndoRedoPerformed;
            showAlternatingRowBackgrounds = true;
            header.sortingChanged += HeaderOnsortingChanged;
            int columnNum = header.state.columns.Length;
            _columnRenders = new Action<Rect, TreeViewItem>[columnNum];
            _columnRenders[1] = TypeCellGUI;
            _columnRenders[2] = PathCellGUI;
            
            _bundlesSp = bundlesSp;
            _itemFieldGetters = new Func<TreeViewItem, IComparable>[columnNum];
            _itemFieldGetters[0] = item => item.icon.name;
            _itemFieldGetters[1] = item => _bundlesSp.GetArrayElementAtIndex(item.id - 1)
                .FindPropertyRelative("_type").enumValueIndex;
            Reload();
        }

        private void UndoRedoPerformed()
        {
            Debug.Log($"current group name: {Undo.GetCurrentGroupName()}");
        }

        private void HeaderOnsortingChanged(MultiColumnHeader header)
        {
            IList<TreeViewItem> rows = GetRows();
            if (rows.Count <= 1)
            {
                return;
            }

            if (header.sortedColumnIndex < 0)
            {
                return;
            }

            rootItem.children = Sort(header.state.sortedColumns).ToList();  
            TreeToList(rootItem, rows);
            Repaint();
        }
        
        static void TreeToList (TreeViewItem root, IList<TreeViewItem> result)
        {
            if (root == null)
                throw new NullReferenceException("root");
            if (result == null)
                throw new NullReferenceException("result");

            result.Clear();
	
            if (root.children == null)
                return;

            Stack<TreeViewItem> stack = new Stack<TreeViewItem>();
            for (int i = root.children.Count - 1; i >= 0; i--)
                stack.Push(root.children[i]);

            while (stack.Count > 0)
            {
                TreeViewItem current = stack.Pop();
                result.Add(current);

                if (current.hasChildren && current.children[0] != null)
                {
                    for (int i = current.children.Count - 1; i >= 0; i--)
                    {
                        stack.Push(current.children[i]);
                    }
                }
            }
        }

        IOrderedEnumerable<TreeViewItem> Sort(IReadOnlyList<int> sortedColumns, IOrderedEnumerable<TreeViewItem> orderedQuery = null, int index = 0)
        {
            if (index > sortedColumns.Count - 1)
            {
                return orderedQuery;
            }
                
            int columnIndex = sortedColumns[index];
            bool ascending = multiColumnHeader.IsSortedAscending(columnIndex);
            Func<TreeViewItem, IComparable> selector = _itemFieldGetters[columnIndex];
            if (orderedQuery == null)
            {
                var items = GetRows();
                orderedQuery = ascending ? items.OrderBy(selector) : items.OrderByDescending(selector);
            }
            else
            {
                orderedQuery = ascending ? orderedQuery.ThenBy(selector) : orderedQuery.ThenByDescending(selector);
            }
            
            return Sort(sortedColumns, orderedQuery, index + 1);
        }

        protected override bool CanRename(TreeViewItem item)
        {
            return true;
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            if (!args.acceptedRename)
            {
                return;
            }

            AssetBundleRename(args.originalName, args.newName);
            var nameSp = _bundlesSp.GetArrayElementAtIndex(args.itemID - 1)
                .FindPropertyRelative("_name");
            nameSp.stringValue = args.newName;
            _bundlesSp.serializedObject.ApplyModifiedProperties();
            Reload();
        }

        void AssetBundleRename(string originAbName, string newAbName)
        {
            string[] paths = AssetDatabase.GetAssetPathsFromAssetBundle(originAbName);
            if (paths == null || paths.Length == 0)
            {
                throw new Exception("AssetBundle name not found!");
            }

            var importer = AssetImporter.GetAtPath(paths.First());
            importer.assetBundleName = newAbName;
            importer.SaveAndReimport();
            AssetDatabase.RemoveUnusedAssetBundleNames();
        }

        Object LoadAsset(string abName)
        {
            string[] paths = AssetDatabase.GetAssetPathsFromAssetBundle(abName);
            if (paths == null || paths.Length == 0)
            {
                throw new Exception("AssetBundle name not found!");
            }

            return AssetDatabase.LoadAssetAtPath<Object>(paths.First());
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return true;
        }

        protected override TreeViewItem BuildRoot()
        {
            return new TreeViewItem {id = 0, depth = -1, displayName = "Root"};
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = base.BuildRows(root);
            
            for (int i = 0; i < _bundlesSp.arraySize; i++)
            {
                var item = _bundlesSp.GetArrayElementAtIndex(i);
                string name = item.FindPropertyRelative("_name").stringValue;
                Object obj = LoadAsset(name);
                var icon = EditorGUIUtility.ObjectContent(obj, obj.GetType());
                
                rows.Add(new TreeViewItem
                {
                    id = i + 1,
                    depth = 0,
                    displayName = name,
                    icon = (Texture2D) icon.image
                });
            }
            return rows;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            base.RowGUI(args);
            
            for (int i = 1; i < args.GetNumVisibleColumns(); i++)
            {
                Rect rect = args.GetCellRect(i);
                _columnRenders[i](rect, args.item);
            }
        }

        void TypeCellGUI(Rect rect, TreeViewItem item)
        {
            SerializedProperty typeSp = _bundlesSp.GetArrayElementAtIndex(item.id - 1).FindPropertyRelative("_type");
            EditorGUI.BeginChangeCheck();
            var t = (BundleType)EditorGUI.EnumPopup(rect, (BundleType)typeSp.enumValueIndex);
            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }
            
            IList<int> selection = GetSelection();
            if (selection.Count > 0 && selection.Contains(item.id))
            {
                foreach (int id in selection)
                {
                    _bundlesSp.GetArrayElementAtIndex(id - 1)
                        .FindPropertyRelative("_type").enumValueIndex = (int) t;
                }
            }
            else
            {
                typeSp.enumValueIndex = (int) t;
            }
        }

        void PathCellGUI(Rect rect, TreeViewItem item)
        {
            string[] paths = AssetDatabase.GetAssetPathsFromAssetBundle(item.displayName);
            EditorGUI.LabelField(rect, paths.First());
        }

        protected override void ContextClickedItem(int id)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Delete"), false, () =>
            {
                // Undo.RecordObject(_bundlesSp.serializedObject.targetObject, "delete");
                IList<int> selection = GetSelection();
                foreach (int selectId in selection.OrderByDescending(x => x))
                {
                    int index = selectId - 1;
                    string abName = _bundlesSp.GetArrayElementAtIndex(index).FindPropertyRelative("_name").stringValue;
                    AssetBundleRename(abName, String.Empty);
                    _bundlesSp.MoveArrayElement(index, _bundlesSp.arraySize - 1);
                    --_bundlesSp.arraySize;
                    _bundlesSp.serializedObject.ApplyModifiedProperties();
                }
                Reload();
            });
            
            menu.AddItem(new GUIContent("Ping"), false, () =>
            {
                string abName = _bundlesSp.GetArrayElementAtIndex(id - 1).FindPropertyRelative("_name").stringValue;
                EditorGUIUtility.PingObject(LoadAsset(abName));
            });
            menu.ShowAsContext();
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            if (!args.performDrop)
            {
                return DragAndDropVisualMode.Copy;
            }

            bool changed = false;
            for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
            {
                string path = DragAndDrop.paths[i];
                if (!AddBundleByPath(path)) continue;
                changed = true;
            }

            if (!changed)
            {
                return DragAndDropVisualMode.Rejected;
            }

            _bundlesSp.serializedObject.ApplyModifiedProperties();
            Reload();

            return DragAndDropVisualMode.Copy;
        }

        public void Import(params string[] assetBundleNames)
        {
            foreach (string assetBundleName in assetBundleNames)
            {
                AddBundleByName(assetBundleName);
            }

            _bundlesSp.serializedObject.ApplyModifiedProperties();
            Reload();
        }

        private bool AddBundleByName(string abName)
        {
            string[] paths = AssetDatabase.GetAssetPathsFromAssetBundle(abName);
            if (paths == null || paths.Length == 0)
            {
                return false;
            }

            return AddBundleByPath(paths.First());
        }

        private bool AddBundleByPath(string assetPath)
        {
            string abName = AssetDatabase.GetImplicitAssetBundleName(assetPath);
            if (!string.IsNullOrEmpty(abName))
            {
                if (GetRows().Any(x => x.displayName == abName))
                {
                    return false;
                }
            }
            else
            {
                abName = Path.GetFileName(assetPath).ToLower();
            }

            if (GetRows().Any(x => x.displayName == abName))
            {
                Object obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                abName += $"_conflict_{obj.GetHashCode()}";
            }

            var importer = AssetImporter.GetAtPath(assetPath);
            importer.assetBundleName = abName;
            importer.SaveAndReimport();

            var newItem = _bundlesSp.GetArrayElementAtIndex(_bundlesSp.arraySize++);
            newItem.FindPropertyRelative("_guid").stringValue = Guid.NewGuid().ToString();
            newItem.FindPropertyRelative("_name").stringValue = abName;
            newItem.FindPropertyRelative("_type").enumValueIndex = 0;
            return true;
        }

        private static MultiColumnHeader CreateColumnHeader()
        {
            var columns = new MultiColumnHeaderState(new []
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Name"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Right,
                    minWidth = 100,
                    autoResize = true,
                    allowToggleVisibility = false
                },
                
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Type"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 60,
                    minWidth = 60,
                    maxWidth = 100,
                    autoResize = true,
                    allowToggleVisibility = false
                },
                
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Path"),
                    headerTextAlignment = TextAlignment.Left,
                    canSort = false,
                    sortingArrowAlignment = TextAlignment.Right,
                    autoResize = true,
                    allowToggleVisibility = true,
                    
                }
            });    
            
            return new MultiColumnHeader(columns);
        }
    }
}