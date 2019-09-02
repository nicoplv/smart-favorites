using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SmartFavorites
{
    public class Favorite : EditorWindow
    {
        #region Variables

        private static Favorite favoriteEditorWindow;
        private Vector2 scrollViewPosition = Vector2.zero;

        private UnityEngine.Object lastObjectSelected = null;
        private double lastObjectSelectedAt = 0f;
        private static readonly double lastObjectSelectedTickOpen = .5f;
        private static readonly double lastObjectSelectedTickPing = 2f;

        private double nextUpdate = 0f;
        private static readonly double updateTick = 0.1f;

        private static readonly Color selectNoPro = new Color(0.56f, 0.56f, 0.56f); 
        private static readonly Color selectPro = new Color(0.28f, 0.28f, 0.28f);

        private static FavoriteSave favoriteSave;
        private static FavoriteSave FavoriteSave { get { if (!favoriteSave) InitFavoriteSave(); return favoriteSave; } }
        private ReorderableList reorderableList;

        private bool guiStyleDefined = false;
        private GUIStyle reorderableListLabelGuiStyle = null;
        private GUIStyle toolbarIconButtonGuiStyle = null;

        private bool editNameList = false;

        #endregion

        #region Menu Item Methods

        [MenuItem("Window/Favorites", priority = 1100)]
        public static void ShowWindow()
        {
            favoriteEditorWindow = GetWindow<Favorite>("Favorites");
            favoriteEditorWindow.titleContent = new GUIContent("Favorites", EditorGUIUtility.IconContent("d_Favorite").image);
            favoriteEditorWindow.Show();
        }

        [MenuItem("Assets/Add or Remove to Favorites %&F", false, priority = -10)]
        public static void AddRemoveToFavorite()
        {
            if (Selection.activeObject == null)
                return;
            
            // Get Favorite window
            if (!favoriteEditorWindow)
                ShowWindow();
            
            List<UnityEngine.Object> addObjects, removeObjects;
            CheckObject(Selection.objects, out addObjects, out removeObjects);

            if (addObjects.Count > 0)
                FavoriteSave.CurrentList.Add(addObjects);
            else
                FavoriteSave.CurrentList.Remove(removeObjects);

            // Repaint window
            favoriteEditorWindow.Repaint();
        }

        private static void CheckObject(UnityEngine.Object[] _objects, out List<UnityEngine.Object> _addObjects, out List<UnityEngine.Object> _removeObjects)
        {
            _addObjects = new List<UnityEngine.Object>();
            _removeObjects = new List<UnityEngine.Object>();
            foreach (UnityEngine.Object iObject in _objects)
            {
                // Check if object is ok
                string assetPath = AssetDatabase.GetAssetPath(iObject);
                if (assetPath == "" || AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath).GetInstanceID() != iObject.GetInstanceID())
                    return;

                // Add or Remove
                if (!FavoriteSave.CurrentList.Contains(iObject))
                    _addObjects.Add(iObject);
                else
                    _removeObjects.Add(iObject);
            }
        }

        private void AddToFavoriteDrop(UnityEngine.Object[] _objects)
        {
            // Get Favorite window
            if (!favoriteEditorWindow)
                ShowWindow();

            List<UnityEngine.Object> addObjects, removeObjects;
            CheckObject(_objects, out addObjects, out removeObjects);

            if (addObjects.Count > 0)
                FavoriteSave.CurrentList.Add(addObjects);
        }

        [MenuItem("Assets/Add or Remove to Favorites %&F", true, priority = -10)]
        public static bool AddRemoveToFavoriteValidate()
        {
            if (Selection.activeObject == null)
                return false;
            return true;
        }

        public void RemoveFavorite(object _object)
        {
            UnityEngine.Object b_object = (UnityEngine.Object)_object;
            if(b_object && FavoriteSave.CurrentList.Contains(b_object))
                FavoriteSave.CurrentList.Remove(b_object);
        }

        public void ClearFavorite()
        {
            if(EditorUtility.DisplayDialog("Clear the list \"" + FavoriteSave.CurrentList.Name + "\"?", "Are you sure you want delete all the Favorites of the list \"" + FavoriteSave.CurrentList.Name + "\"?", "Yes", "No"))
                FavoriteSave.CurrentList.Clear();
        }

        #endregion

        #region Window Methods

        public void OnEnable()
        {
            reorderableList = new ReorderableList(null, typeof(GameObject), false, false, false, false);
            reorderableList.showDefaultBackground = false;
            reorderableList.headerHeight = 0F;
            reorderableList.footerHeight = 0F;
            reorderableList.drawElementCallback = DrawFavoriteElement;
            InitFavoriteSave();
            guiStyleDefined = false;
        }

        private static void InitFavoriteSave()
        {
            if (!favoriteSave)
            {
                // Load save
                string[] favoriteSaveFind = AssetDatabase.FindAssets("t:FavoriteSave");
                if (favoriteSaveFind.Length > 0)
                    favoriteSave = AssetDatabase.LoadAssetAtPath<FavoriteSave>(AssetDatabase.GUIDToAssetPath(favoriteSaveFind[0]));

                // If no save, create one
                if (!favoriteSave)
                {
                    // Search path
                    string favoriteSavePath = "";
                    string[] favoriteSavePathFind = AssetDatabase.FindAssets("FavoriteSave t:Script");
                    if (favoriteSavePathFind.Length > 0)
                        favoriteSavePath = ((AssetDatabase.GUIDToAssetPath(favoriteSavePathFind[0])).Replace("FavoriteSave.cs", "FavoriteSave.asset"));

                    // In case don't find create at root
                    if(!favoriteSavePath.Contains("FavoriteSave.asset"))
                        favoriteSavePath = "Assets/FavoriteSave.asset";

                    favoriteSave = CreateInstance<FavoriteSave>();
                    AssetDatabase.CreateAsset(favoriteSave, favoriteSavePath);
                    AssetDatabase.SaveAssets();
                }
            }
        }
        
        private void DrawFavoriteElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            UnityEngine.Object currentObject = FavoriteSave.CurrentList.Get(index);
            if (!currentObject)
                return;

            // Image rect
            Rect iconRect = new Rect(rect);
            iconRect.y += 1f;
            iconRect.height -= 4f;
            iconRect.width = iconRect.height;

            // Label rect
            Rect labelRect = new Rect(rect);
            labelRect.y += 2f;
            labelRect.height -= 4f;
            labelRect.x += iconRect.width;
            labelRect.width -= iconRect.width;

            // Background rect
            Rect backgroundRect = new Rect(rect);
            backgroundRect.x = 0;
            backgroundRect.width = position.width;

            //if (objects[index] == Selection.activeObject)
            if (Selection.objects.Contains(currentObject))
                EditorGUI.DrawRect(backgroundRect, (EditorPrefs.GetInt("UserSkin")==1) ? selectPro : selectNoPro);

            // Display icon and name
            GUI.DrawTexture(iconRect, AssetPreview.GetMiniThumbnail(currentObject), ScaleMode.ScaleToFit, true);
            EditorGUI.LabelField(labelRect, currentObject.name, reorderableListLabelGuiStyle);

            // Mouse events
            if (Event.current.isMouse)
            {
                // TODO Add Shift and Ctrl multi-selection

                if (Event.current.type == EventType.MouseUp && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
                {
                    UnityEditor.Selection.activeObject = currentObject;
                    if (lastObjectSelected == currentObject)
                    {
                        if(lastObjectSelectedAt + lastObjectSelectedTickOpen > EditorApplication.timeSinceStartup)
                            AssetDatabase.OpenAsset(currentObject);
                        else if(lastObjectSelectedAt + lastObjectSelectedTickPing > EditorApplication.timeSinceStartup)
                            EditorGUIUtility.PingObject(currentObject);
                    }
                    lastObjectSelected = currentObject;
                    lastObjectSelectedAt = EditorApplication.timeSinceStartup;
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
                {
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.SetGenericData("favorite", currentObject);
                    DragAndDrop.objectReferences = new UnityEngine.Object[] { currentObject };
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseDrag && Event.current.button == 0 && (UnityEngine.Object)(DragAndDrop.GetGenericData("favorite")) == currentObject)
                {
                    DragAndDrop.StartDrag("Drag favorite");
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.ContextClick && rect.Contains(Event.current.mousePosition))
                {
                    //UnityEditor.Selection.activeObject = objects[index];
                    ShowGenericMenu(currentObject);
                    Event.current.Use();
                }
            }
        }
        
        private void ShowGenericMenu(UnityEngine.Object _object = null)
        {
            GenericMenu genericMenu = new GenericMenu();
            if(_object)
                genericMenu.AddItem(new GUIContent("Remove"), false, RemoveFavorite, _object);
            else
                genericMenu.AddDisabledItem(new GUIContent("Remove"));
            genericMenu.AddSeparator("");
            genericMenu.AddItem(new GUIContent("Clear All"), false, ClearFavorite);
            genericMenu.ShowAsContext();
        }

        public void OnLostFocus()
        {
            if (editNameList)
                editNameList = false;
        }
        
        public void Update()
        {
            // Force repaint for DragAndDrop
            if (EditorApplication.timeSinceStartup > nextUpdate)
            {
                nextUpdate = EditorApplication.timeSinceStartup + updateTick;
                Repaint();
            }
        }
        
        public void OnGUI()
        {
            // Manage GUIStyle
            if (!guiStyleDefined)
            {
                guiStyleDefined = true;
                reorderableListLabelGuiStyle = new GUIStyle(EditorStyles.label);
                reorderableListLabelGuiStyle.focused.textColor = reorderableListLabelGuiStyle.normal.textColor;
                toolbarIconButtonGuiStyle = new GUIStyle(EditorStyles.toolbarButton);
                toolbarIconButtonGuiStyle.font = Resources.Load<Font>("FontAwesome");
                toolbarIconButtonGuiStyle.fontSize = 10;
            }

            GUILayout.BeginVertical();

            // Toolbar
            GUILayout.BeginHorizontal();
            
            // Edit list name
            if (GUILayout.Button("", toolbarIconButtonGuiStyle, GUILayout.ExpandWidth(false)))
                ButtonEditFavoriteList();

            // List selection
            if (editNameList)
            {
                GUI.SetNextControlName("EditNameList");
                FavoriteSave.CurrentList.Name = EditorGUILayout.TextField(FavoriteSave.CurrentList.Name, EditorStyles.toolbarTextField, GUILayout.ExpandWidth(true));
                EditorGUI.FocusTextInControl("EditNameList");
            }
            else
                FavoriteSave.CurrentListIndex = EditorGUILayout.Popup(FavoriteSave.CurrentListIndex, FavoriteSave.NameList(), EditorStyles.toolbarPopup);

            if (editNameList && ((Event.current.type == EventType.MouseUp && Event.current.button == 0) || Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Return))
                editNameList = false;

            // Add remove list (disable in edit name list mode)
            EditorGUI.BeginDisabledGroup(editNameList);
            if (GUILayout.Button("", toolbarIconButtonGuiStyle, GUILayout.ExpandWidth(false)))
                ButtonAddFavoriteList();
            EditorGUI.BeginDisabledGroup(FavoriteSave.FavoriteLists.Count <= 1);
            if (GUILayout.Button("", toolbarIconButtonGuiStyle, GUILayout.ExpandWidth(false)))
                ButtonRemoveFavoriteList();
            EditorGUI.EndDisabledGroup();
            EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();

            bool mouseOnWindow = Event.current.mousePosition.x >= 0 && Event.current.mousePosition.x <= position.width && Event.current.mousePosition.y >= 20 && Event.current.mousePosition.y <= position.height;

            if (Event.current.type == EventType.DragUpdated && mouseOnWindow)
            {
                if (DragAndDrop.objectReferences.Length > 0)
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                else
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            }
            else if (Event.current.type == EventType.DragPerform && mouseOnWindow)
            {
                DragAndDrop.AcceptDrag();

                AddToFavoriteDrop(DragAndDrop.objectReferences);

                DragAndDrop.PrepareStartDrag();
                Event.current.Use();
            }

            // Scroll view
            scrollViewPosition = GUILayout.BeginScrollView(scrollViewPosition);

            FavoriteSave.CurrentList.Update();
            reorderableList.list = FavoriteSave.CurrentList.Objects;
            reorderableList.DoLayoutList();

            GUILayout.EndScrollView();

            // Left Click and Right Click at the end if event not used
            if (Event.current.type == EventType.ContextClick)
                ShowGenericMenu();
            else if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
                Selection.activeObject = null;

            GUILayout.EndVertical();
        }

        #endregion

        #region Methods

        public void ButtonAddFavoriteList()
        {
            FavoriteSave.AddList();
        }

        public void ButtonRemoveFavoriteList()
        {
            if (EditorUtility.DisplayDialog("Remove the list \"" + FavoriteSave.CurrentList.Name + "\"?", "Are you sure you want delete the list \"" + FavoriteSave.CurrentList.Name + "\"?", "Yes", "No"))
                FavoriteSave.RemoveList(FavoriteSave.CurrentListIndex);
        }

        public void ButtonEditFavoriteList()
        {
            editNameList = !editNameList;
        }

        #endregion
    }
}