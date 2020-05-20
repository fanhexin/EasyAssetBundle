using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EasyAssetBundle.Common;
using EasyAssetBundle.Common.Editor;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = System.Object;

namespace EasyAssetBundle.Editor
{
    // todo 添加对undo的支持
    // todo 添加针对每个不同的bundle设置压缩格式
    public class BundleTreeView : TreeView
    {
        private readonly EditorWindow _parentWindow;
        private const string INSIDE_DRAG_KEY = "inside_drag";
        private readonly SerializedProperty _bundlesSp;
        private readonly Func<TreeViewItem, IComparable>[] _itemFieldGetters;
        private readonly Action<Rect, RowGUIArgs>[] _columnRenders;
        private readonly Dictionary<string, int> _abName2ViewItemId = new Dictionary<string, int>();
        private List<TreeViewItem> _sortedChildren;

        public BundleTreeView(TreeViewState state, SerializedProperty bundlesSp, EditorWindow parentWindow)
            :this(CreateColumnHeader(), state, bundlesSp)
        {
            _parentWindow = parentWindow;
        }

        private BundleTreeView(MultiColumnHeader header, TreeViewState state, SerializedProperty bundlesSp) 
            : base(state, header)
        {
            Undo.undoRedoPerformed += UndoRedoPerformed;
            showAlternatingRowBackgrounds = true;
            header.sortingChanged += HeaderOnsortingChanged;
            int columnNum = header.state.columns.Length;
            _columnRenders = new Action<Rect, RowGUIArgs>[columnNum];
            _columnRenders[1] = TypeCellGUI;
            _columnRenders[2] = SizeCellGUI;
            _columnRenders[3] = PathCellGUI;
            
            _bundlesSp = bundlesSp;
            _itemFieldGetters = new Func<TreeViewItem, IComparable>[columnNum];
            _itemFieldGetters[0] = item => item.displayName;
            _itemFieldGetters[1] = item => _bundlesSp.GetArrayElementAtIndex(item.id - 1)
                .FindPropertyRelative("_type").enumValueIndex;
            _itemFieldGetters[2] = item => new FileInfo(Path.Combine(Settings.currentTargetCachePath, item.displayName)).Length;
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

            _sortedChildren = Sort(header.state.sortedColumns).ToList();
            Reload();
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
            return item.depth == 0;
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            string newName = args.newName.Trim();
            if (!args.acceptedRename || newName == args.originalName)
            {
                return;
            }

            Rename(args.itemID, newName);
            Reload();
        }

        void Rename(int id, string newName)
        {
            if (AssetDatabase.GetAllAssetBundleNames().Contains(newName))
            {
                _parentWindow.ShowNotification(new GUIContent($"Existing name: {newName}!"));
                return;
            }

            var item = FindItem(id, rootItem);
            (item as BundleTreeViewItem).Rename(newName);
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return true;
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem {id = 0, depth = -1, displayName = "Root"};
            if (_sortedChildren != null)
            {
                root.children = _sortedChildren;
                _sortedChildren = null;
                return root;
            }

            _abName2ViewItemId.Clear();
            int j = _bundlesSp.arraySize;
            var allItems = new List<TreeViewItem>();
            for (int i = 0; i < _bundlesSp.arraySize; i++)
            {
                var item = _bundlesSp.GetArrayElementAtIndex(i);
                string name = item.FindPropertyRelative("_name").stringValue;
                
                allItems.Add(new BundleTreeViewItem(_bundlesSp)
                {
                    id = i + 1, 
                    depth = 0, 
                    displayName = name
                });

                _abName2ViewItemId[name] = i + 1;

                string[] paths = AssetDatabase.GetAssetPathsFromAssetBundle(name);
                foreach (string path in paths)
                {
                    allItems.Add(new BundleAssetTreeViewItem
                    {
                        id = ++j, 
                        depth = 1, 
                        displayName = Path.GetFileNameWithoutExtension(path),
                        path = path,
                        icon = GetIcon(path)
                    });
                }
            }
            SetupParentsAndChildrenFromDepths(root, allItems);
            return root;
        }

        Texture2D GetIcon(string assetPath)
        {
            string extension = Path.GetExtension(assetPath);
            if (extension == ".ogg" ||
                extension == ".mp3")
            {
                return EditorGUIUtility.IconContent("AudioClip Icon").image as Texture2D;
            }

            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            return EditorGUIUtility.ObjectContent(obj, typeof(Object)).image as Texture2D;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            base.RowGUI(args);
            
            for (int i = 1; i < args.GetNumVisibleColumns(); i++)
            {
                Rect rect = args.GetCellRect(i);
                _columnRenders[i](rect, args);
            }
        }

        void TypeCellGUI(Rect rect, RowGUIArgs args)
        {
            if (args.item.depth > 0)
            {
                return;
            }
            
            SerializedProperty typeSp = (args.item as BundleTreeViewItem).typeSp;
            EditorGUI.BeginChangeCheck();
            var t = (BundleType)EditorGUI.EnumPopup(rect, (BundleType)typeSp.enumValueIndex);
            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }
            
            IList<int> selection = GetSelection();
            if (selection.Count > 0 && selection.Contains(args.item.id))
            {
                foreach (var bundleTreeViewItem in FindRows(selection).Cast<BundleTreeViewItem>())
                {
                    bundleTreeViewItem.type = t;
                }
            }
            else
            {
                typeSp.enumValueIndex = (int) t;
            }
        }

        void PathCellGUI(Rect rect, RowGUIArgs args)
        {
            if (args.item.depth != 1)
            {
                return;
            }
            
            string path = (args.item as BundleAssetTreeViewItem).path;
            EditorGUI.LabelField(rect, path, args.selected ? EditorStyles.whiteLabel : EditorStyles.label);
        }

        void SizeCellGUI(Rect rect, RowGUIArgs args)
        {
            if (args.item.depth != 0)
            {
                return;
            }
            
            string cachePath = Path.Combine(Settings.currentTargetCachePath, args.item.displayName);
            if (!File.Exists(cachePath))
            {
                return;
            }
            
            long size = new FileInfo(cachePath).Length;
            EditorGUI.LabelField(rect, SizeSuffix(size), args.selected ? EditorStyles.whiteLabel : EditorStyles.label);
        }
        
        static readonly string[] SizeSuffixes = 
            { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        static string SizeSuffix(Int64 value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + SizeSuffix(-value); } 
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}", 
                adjustedSize, 
                SizeSuffixes[mag]);
        }

        protected override void ContextClickedItem(int id)
        {
            // todo 右键菜单添加展开选择的多个父节点的功能
            var viewItem = FindItem(id, rootItem);
            var menu = new GenericMenu();

            if (viewItem.depth == 1)
            {
                menu.AddItem(new GUIContent("Ping"),
                    false,
                    () => (viewItem as BundleAssetTreeViewItem).Ping());
            }
            
            // todo 删除时需要提醒当前bundle是否被AssetBundleReference或者AssetReference引用
            menu.AddItem(new GUIContent("Delete"), false, () =>
            {
                IList<int> selection = GetSelection();
                IOrderedEnumerable<TreeViewItem> rows = FindRows(selection).OrderByDescending(x => x.id);
                foreach (var item in rows.Where(x => x.depth == 0).Cast<BundleTreeViewItem>())
                {
                    item.Delete();    
                }
                
                foreach (var item in rows.Where(x => x.depth == 1).Cast<BundleAssetTreeViewItem>())
                {
                    item.Delete();    
                }

                Reload();
            });

            if (viewItem.depth == 0)
            {
                menu.AddItem(new GUIContent("Select Dependencies"), false, () =>
                {
                    int selectId = GetSelection().First();
                    var item = FindItem(selectId, rootItem);
                    var dependencies = AssetDatabase.GetAssetBundleDependencies(item.displayName, true)
                        .Select(x => _abName2ViewItemId[x])
                        .ToList();
                    if (dependencies.Count == 0)
                    {
                        return;
                    }
                    SetSelection(dependencies);
                });
            }
            menu.ShowAsContext();
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            return FindRows(args.draggedItemIDs).All(x => x.depth != 0);
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();
            string[] paths = FindRows(args.draggedItemIDs)
                .Cast<BundleAssetTreeViewItem>()
                .Select(x => x.path)
                .ToArray();
            
            DragAndDrop.paths = paths;
            DragAndDrop.SetGenericData(INSIDE_DRAG_KEY, true);
            DragAndDrop.StartDrag("move");
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            return DragAndDrop.GetGenericData(INSIDE_DRAG_KEY) == null
                ? DragAndDropFromOutside(args)
                : DragAndDropFromInside(args);
        }

        private DragAndDropVisualMode DragAndDropFromInside(DragAndDropArgs args)
        {
            if (args.parentItem?.depth != 0)
            {
                return DragAndDropVisualMode.None;
            }
            
            if (!args.performDrop)
            {
                return DragAndDropVisualMode.Move;
            }

            bool changed = false;
            foreach (string path in DragAndDrop.paths)
            {
                var importer = AssetImporter.GetAtPath(path);
                if (importer.assetBundleName == args.parentItem.displayName)
                {
                    continue;
                }
                importer.assetBundleName = args.parentItem.displayName;
                importer.SaveAndReimport();
                changed = true;
            }

            if (!changed)
            {
                return DragAndDropVisualMode.None;
            }

            Reload();
            return DragAndDropVisualMode.Move;
        }

        private DragAndDropVisualMode DragAndDropFromOutside(DragAndDropArgs args)
        {
            if (args.parentItem?.depth == 1)
            {
                return DragAndDropVisualMode.None;
            }
            
            if (!args.performDrop)
            {
                return DragAndDropVisualMode.Copy;
            }

            if (args.parentItem?.depth == 0)
            {
                foreach (string path in DragAndDrop.paths)
                {
                    var importer = AssetImporter.GetAtPath(path);
                    importer.assetBundleName = args.parentItem.displayName;
                    importer.SaveAndReimport();
                }
                Reload();
                return DragAndDropVisualMode.Copy;
            }

            if (DragAndDrop.paths.Length > 1)
            {
                var menu = new GenericMenu();    
                menu.AddItem(new GUIContent("Create Separate Bundles"), false, () =>
                {
                    foreach (string path in DragAndDrop.paths)
                    {
                        _bundlesSp.AddBundle(path);
                    }
                    Reload();
                });

                menu.AddItem(new GUIContent("Create One Bundle"), false, async () =>
                {
                    _parentWindow.Focus();
                    string abName = "new_assetbundle_name";
                    _bundlesSp.AddBundle(abName, BundleType.Static, DragAndDrop.paths);
                    Reload();
                    
                    var newItem = GetRows().Last();
                    SetSelection(new[] {newItem.id});
                    EndRename();
                    BeginRename(newItem);
                });
                
                menu.ShowAsContext();
                return DragAndDropVisualMode.Copy;
            }

            _bundlesSp.AddBundle(DragAndDrop.paths.First());
            Reload();
            return DragAndDropVisualMode.Copy;
        }

        public void Import(params string[] assetBundleNames)
        {
            foreach (string assetBundleName in assetBundleNames)
            {
                _bundlesSp.AddBundleByAbName(assetBundleName);
            }

            Reload();
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
                    headerContent = new GUIContent("Size"),
                    headerTextAlignment = TextAlignment.Left,
                    canSort = true,
                    sortingArrowAlignment = TextAlignment.Right,
                    autoResize = true,
                    allowToggleVisibility = true
                },
                
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Path"),
                    headerTextAlignment = TextAlignment.Left,
                    canSort = false,
                    sortingArrowAlignment = TextAlignment.Right,
                    autoResize = true,
                    allowToggleVisibility = true
                }
            });    
            
            return new MultiColumnHeader(columns);
        }
    }
}