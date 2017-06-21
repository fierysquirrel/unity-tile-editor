using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Created by Henry Fernández
/// Fiery Squirrel (http://fierysquirrel.com/)
/// May, 2017
/// Version 0.9.0
/// </summary>
namespace Assets.Editor
{
    public class LevelCreatorEditor : EditorWindow
    {
        enum CursorState
        {
            Drag,
            Remove,
            Add
        }

        enum SnapState
        {
            Up,
            Down,
            Left,
            Right,
            Center,
            UpLeft,
            UpRight,
            DownLeft,
            DownRight
        }

        public const string TILE_BOUNDING_NAME = "Bounding";
        public const string TILE_BOUNDING_TAG = "FSBounding";
        public const string TILE_BOUNDING_LAYER = "FSBounding";
        public const string TILE_BOUNDING_CONTAINER_NAME = "Boundings";

        int gridSize;
        float gridX;
        float gridY;
        bool tileButPressed;
        CursorState cursorState = CursorState.Drag;
        Vector3 mouseConvPos;
        Vector3 mousePosition;
        Vector2 mouseGridPos;
        EditorObject dragObject;
        List<EditorObject> selectedObjects;
        Color gridColor = Color.white * 0.8f;
        Color cursorColor = Color.red;
        Color selectColor = Color.blue;
        List<EditorObjectsMenu> menus;
        List<Layer> layers;
        EditorObjectsMenu selMenu;
        public Texture[] initialIcons;
        EditorObjectsMenu objMenu;
        Vector2 gameobjectsScrollPos;
        Vector2 layersScrollPos;
        Layer selectedLayer = null;
        bool loaded;
        bool showGrid = true;
        SnapState snapState = SnapState.Center;

        // Add menu named "My Window" to the Window menu
        [MenuItem("Window/Level Editor")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            LevelCreatorEditor window = (LevelCreatorEditor)EditorWindow.GetWindow(typeof(LevelCreatorEditor));
            window.Show();
        }

        private void Awake()
        {
            //if(!loaded)
            //LoadData();
        }

        public void LoadInitialIcons()
        {
            Object[] prefabs;
            prefabs = Resources.LoadAll("Icons", typeof(Object));
            initialIcons = new Texture[prefabs.Length];
            for (int i = 0; i < prefabs.Length; i++)
                initialIcons[i] = ((GameObject)prefabs[i]).GetComponent<SpriteRenderer>().sprite.texture;
        }
        
        void OnGUI()
        {
            if (menus == null)
                menus = new List<EditorObjectsMenu>();

            if (layers == null)
                layers = new List<Layer>();

            //Header
            BuildMenuHeader();
            //Layers
            BuildMenuLayers();
            //Objects
            BuildMenuObjects();
            //Object Details
            BuildMenuObjectDetails();
            //Actions
            BuildMenuActions();
            //Snapping
            BuildMenuSnapping();
        }

        private void BuildMenuHeader()
        {
            GUILayout.Label("GRID SETTINGS", EditorStyles.boldLabel);
            gridSize = EditorGUILayout.IntField("Grid Size (px)", gridSize);
            gridX = gridSize / 100f;
            gridY = gridSize / 100f;
            gridColor = EditorGUILayout.ColorField("Grid Color", gridColor);
            cursorColor = EditorGUILayout.ColorField("Cursor Color", cursorColor);
            selectColor = EditorGUILayout.ColorField("Select Color", selectColor);
            showGrid = EditorGUILayout.Toggle("Show Grid",showGrid);
        }

        private void BuildMenuLayers()
        {
            List<Layer> removedLayers;
            Layer newLayer;

            GUILayout.Label("LAYERS", EditorStyles.boldLabel);
            //Add menu button
            if (GUILayout.Button("Add Layer"))
            {
                newLayer = new Layer();
                layers.Add(newLayer);
                if (selectedLayer == null)
                    selectedLayer = newLayer;
            }

            removedLayers = new List<Layer>();
            layersScrollPos = GUILayout.BeginScrollView(layersScrollPos, false, true);
            //Layers
            foreach (Layer l in layers)
            {
                //Actions
                EditorGUILayout.BeginHorizontal();
                l.name = EditorGUILayout.TextField("Name: ", l.name);
                l.UpdateContainerName();

                if (selectedLayer == null)
                    selectedLayer = l;

                if (GUILayout.Toggle(selectedLayer.GetID() == l.GetID(), "Select", "Button"))
                {
                    selectedLayer = l;
                }

                //Remove menu
                if (GUILayout.Button("Remove"))
                    removedLayers.Add(l);

                EditorGUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            //Clean layers
            foreach (Layer l in removedLayers)
            {
                l.Destroy();
                layers.Remove(l);
                if (l == selectedLayer)
                {
                    if (layers.Count > 0)
                        selectedLayer = layers[0];
                    else
                        selectedLayer = null;
                }
            }
        }

        private void BuildMenuObjects()
        {
            int curSel;
            List<EditorObjectsMenu> removedMenus;

            EditorGUI.BeginDisabledGroup(layers.Count <= 0);
            GUILayout.Label("GAME OBJECTS", EditorStyles.boldLabel);
            //Add menu button
            if (GUILayout.Button("Add Menu"))
                menus.Add(new EditorObjectsMenu());

            removedMenus = new List<EditorObjectsMenu>();

            //GUILayout.BeginVertical();
            gameobjectsScrollPos = GUILayout.BeginScrollView(gameobjectsScrollPos, false, true);
            //Menus
            foreach (EditorObjectsMenu m in menus)
            {
                //Title
                GUILayout.Label("Menu: " + m.folder, EditorStyles.boldLabel);
                //m.name = EditorGUILayout.TextField("Title", m.name);
                //Columns
                m.columns = EditorGUILayout.IntField("Columns", m.columns);
                //Folder's location
                EditorGUILayout.BeginHorizontal();
                m.folder = EditorGUILayout.TextField("Folder Name", m.folder);
                if (GUILayout.Button("Load"))
                    m.LoadPrefabs();
                EditorGUILayout.EndHorizontal();
                //Load elements
                if (m.tiles != null && m.tiles.Length > 0 && m.columns > 0)
                {
                    curSel = GUILayout.SelectionGrid(m.selGridInt, m.tiles, m.columns, GUILayout.Width(position.width - 20),GUILayout.Height(100));
                    if (curSel != m.selGridInt)
                    {
                        m.selGridInt = curSel;
                        objMenu = m;
                        cursorState = CursorState.Add;
                        DeactivateMenus();
                    }
                }

                //Remove menu
                if (GUILayout.Button("Remove"))
                    removedMenus.Add(m);
            }
            GUILayout.EndScrollView();
            EditorGUI.EndDisabledGroup();

            //Clean menus
            foreach (EditorObjectsMenu m in removedMenus)
                menus.Remove(m);
        }

        private void BuildMenuObjectDetails()
        {
            float selL, selR, selT, selD, selSizeX, selSizeY;
            int objLayIdx;
            GameObject tileBounding, tileBoundingContainer;
            EditorObject selectedObject;


            EditorGUI.BeginDisabledGroup(layers.Count <= 0 || menus.Count <= 0);
            GUILayout.Label("SELECTED OBJECT", EditorStyles.boldLabel);
            //Actions
            EditorGUILayout.BeginHorizontal();

            selectedObject = null;
            if (selectedObjects != null)
            {
                if (selectedObjects.Count > 0)
                    selectedObject = selectedObjects[0];
            }

            //Add selected object info here
            if (selectedObject != null)
            {
                if (selectedObject.reference != null)
                {
                    GUILayout.Box(selectedObject.reference.GetComponent<SpriteRenderer>().sprite.texture, GUILayout.Width(50), GUILayout.Height(50));
                    GUILayout.Label(selectedObject.i + "," + selectedObject.j);
                    objLayIdx = EditorGUILayout.Popup(layers.IndexOf(selectedObject.layer), GetLayersStrings(selectedObject));
                    if (selectedObject.layer != layers[objLayIdx])
                    {
                        selectedObject.layer.GetSceneElements().Remove(selectedObject);
                        selectedObject.layer = layers[objLayIdx];
                        selectedObject.layer.GetSceneElements().Add(selectedObject);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            //Transformations
            if (GUILayout.Button("Rotate Right"))
            {
                foreach (EditorObject obj in selectedObjects)
                    obj.reference.transform.Rotate(Vector3.forward * -90);
            }
            if (GUILayout.Button("Rotate Left"))
            {
                foreach (EditorObject obj in selectedObjects)
                    obj.reference.transform.Rotate(Vector3.forward * 90);
            }
            if (GUILayout.Button("Flip Hor"))
            {
                foreach (EditorObject obj in selectedObjects)
                    obj.reference.transform.localScale = new Vector3(-obj.reference.transform.localScale.x, obj.reference.transform.localScale.y, obj.reference.transform.localScale.z);
            }
            if (GUILayout.Button("Flip ver"))
            {
                foreach (EditorObject obj in selectedObjects)
                    obj.reference.transform.localScale = new Vector3(obj.reference.transform.localScale.x, -obj.reference.transform.localScale.y, obj.reference.transform.localScale.z);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Bounding") && selectedObjects.Count > 0)
            {
                selL = float.PositiveInfinity;
                selR = float.NegativeInfinity;
                selT = float.NegativeInfinity;
                selD = float.PositiveInfinity;

                selSizeX = 0;
                selSizeY = 0;
                foreach (EditorObject obj in selectedObjects)
                {
                    selSizeX = obj.reference.GetComponent<SpriteRenderer>().bounds.size.x;
                    selSizeY = obj.reference.GetComponent<SpriteRenderer>().bounds.size.y;
                    if (obj.reference.transform.position.x < selL)
                        selL = obj.reference.transform.position.x;

                    if (obj.reference.transform.position.x > selR)
                        selR = obj.reference.transform.position.x;

                    if (obj.reference.transform.position.y < selD)
                        selD = obj.reference.transform.position.y;

                    if (obj.reference.transform.position.y > selT)
                        selT = obj.reference.transform.position.y;
                }
                
                tileBounding = new GameObject();
                tileBounding.name = TILE_BOUNDING_NAME;
                tileBounding.AddComponent(typeof(BoxCollider2D));
                tileBounding.GetComponent<BoxCollider2D>().size = new Vector2(Mathf.Abs(selR - selL) + selSizeX, Mathf.Abs(selD - selT) + selSizeY);
                tileBounding.transform.position = new Vector3(selL + tileBounding.GetComponent<BoxCollider2D>().size.x / 2 - selSizeX / 2, selT - tileBounding.GetComponent<BoxCollider2D>().size.y / 2 + selSizeY / 2);
                tileBoundingContainer = GameObject.Find(TILE_BOUNDING_CONTAINER_NAME);
                //Doesn't exist, add it
                if (tileBoundingContainer == null)
                {
                    tileBoundingContainer = new GameObject();
                    tileBoundingContainer.name = TILE_BOUNDING_CONTAINER_NAME;
                    
                    //Add tag if it doesn't exist
                    if (!Helper.TagExists(TILE_BOUNDING_TAG))
                        Helper.AddNewTag(TILE_BOUNDING_TAG);

                    tileBoundingContainer.tag = TILE_BOUNDING_TAG;

                    //Add layer if it doesn't exist
                    if (!Helper.LayerExists(TILE_BOUNDING_LAYER))
                        Helper.AddNewLayer(TILE_BOUNDING_LAYER);

                    //tileBoundingContainer.layer = Helper.GetLayerIndex(TILE_BOUNDING_LAYER);
                }

                //Set parent
                tileBounding.transform.parent = tileBoundingContainer.transform;
                tileBounding.layer = Helper.GetLayerIndex(TILE_BOUNDING_LAYER);

                //Add logically to each tile that includes the bounding
                foreach (EditorObject obj in selectedObjects)
                {
                    //tileBounding.GetComponent<TileBounding>().AddEditorObject(obj);
                    obj.AddTileBounding(tileBounding);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
        }

        private void BuildMenuActions()
        {
            EditorGUI.BeginDisabledGroup(menus.Count <= 0 || layers.Count <= 0);
            GUILayout.Label("Actions", EditorStyles.boldLabel);
            //Actions
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Toggle(cursorState == CursorState.Remove, initialIcons[1], "Button"))
            {
                if (cursorState != CursorState.Remove)
                {
                    cursorState = CursorState.Remove;
                    objMenu = null;
                    DeactivateMenus();
                }
            }

            if (GUILayout.Toggle(cursorState == CursorState.Drag, initialIcons[0], "Button"))
            {
                if (cursorState != CursorState.Drag)
                {
                    cursorState = CursorState.Drag;
                    objMenu = null;
                    DeactivateMenus();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
        }

        private void BuildMenuSnapping()
        {
            EditorGUI.BeginDisabledGroup(menus.Count <= 0 || layers.Count <= 0);
            GUILayout.Label("Snapping", EditorStyles.boldLabel);
            //Actions
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Toggle(snapState == SnapState.Center, "Center", "Button"))
            {
                snapState = SnapState.Center;
            }

            if (GUILayout.Toggle(snapState == SnapState.Down, "Down", "Button"))
            {
                snapState = SnapState.Down;
            }

            if (GUILayout.Toggle(snapState == SnapState.Up, "Up", "Button"))
            {
                snapState = SnapState.Up;
            }

            if (GUILayout.Toggle(snapState == SnapState.Left, "Left", "Button"))
            {
                snapState = SnapState.Left;
            }

            if (GUILayout.Toggle(snapState == SnapState.Right, "Right", "Button"))
            {
                snapState = SnapState.Right;
            }

            if (GUILayout.Toggle(snapState == SnapState.UpLeft, "Up Left", "Button"))
            {
                snapState = SnapState.UpLeft;
            }

            if (GUILayout.Toggle(snapState == SnapState.UpRight, "Up Right", "Button"))
            {
                snapState = SnapState.UpRight;
            }

            if (GUILayout.Toggle(snapState == SnapState.DownLeft, "Down Left", "Button"))
            {
                snapState = SnapState.DownLeft;
            }

            if (GUILayout.Toggle(snapState == SnapState.DownRight, "Down Right", "Button"))
            {
                snapState = SnapState.DownRight;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
        }

        private string[] GetLayersStrings(EditorObject obj)
        {
            string[] options = new string[layers.Count];

            for (int i = 0; i < layers.Count; i++)
                options[i] = layers[i].name;

            return options;
        }

        void DeactivateMenus()
        {
            foreach (EditorObjectsMenu m in menus)
            {
                if (objMenu == null)
                    m.selGridInt = -1;
                else
                {
                    if (objMenu != m)
                        m.selGridInt = -1;
                }
            }
        }

        void OnSceneGUI(SceneView sceneView)
        {
            int numberX, numberY;
            float lineSizeX, lineSizeY;
            Vector3 initialPos, refSize, selObjPos,selObjSize;
            EditorObject selectedObject;

            mousePosition = Event.current.mousePosition;

            if (sceneView != null)
            {
                mousePosition.y = sceneView.camera.pixelHeight - mousePosition.y;
                mousePosition = sceneView.camera.ScreenToWorldPoint(mousePosition);
                mousePosition.y = -mousePosition.y;
                //cameraSize = sceneView.camera.ScreenToWorldPoint(new Vector3(sceneView.camera.pixelRect.width, sceneView.camera.pixelRect.height));
                mouseConvPos = Helper.ConvertVectorToGrid(mousePosition, gridX, gridY);
                mouseConvPos = new Vector3(mouseConvPos.x * gridX, -(mouseConvPos.y + 1) * gridY);
                mouseGridPos = Helper.ConvertVectorToGrid(mousePosition, gridX, gridY);
                //TODO: we have to adapt this to the size of the camera
                lineSizeX = 40;
                lineSizeY = 30;
                numberX = Mathf.RoundToInt(lineSizeX / gridX) % 2 == 0 ? Mathf.RoundToInt(lineSizeX / gridX) : Mathf.RoundToInt(lineSizeX / gridX) + 1;
                numberY = Mathf.RoundToInt(lineSizeY / gridY) % 2 == 0 ? Mathf.RoundToInt(lineSizeY / gridY) : Mathf.RoundToInt(lineSizeY / gridY) + 1;
                initialPos = -new Vector2(gridX * numberX, gridY * numberY) / 2 + new Vector2(Helper.ConvertVectorToGrid(sceneView.pivot, gridX, gridY).x * gridX, Helper.ConvertVectorToGrid(sceneView.pivot, gridX, gridY).y * gridY);

                //Grid
                if (showGrid)
                {
                    //Vertical Lines
                    for (int i = 0; i <= numberX; i++)
                    {
                        Handles.color = gridColor;
                        Handles.DrawLine(initialPos + new Vector3(i * gridX, 0), initialPos + new Vector3(i * gridX, gridX * numberY));
                    }

                    //Horizontal lines
                    for (int j = 0; j <= numberY; j++)
                    {
                        Handles.color = gridColor;
                        Handles.DrawLine(initialPos + new Vector3(0, j * gridY), initialPos + new Vector3(gridY * numberX, j * gridY));
                    }
                }

                //Draw Cursor
                Handles.color = cursorColor;
                Handles.DrawLine(mouseConvPos, mouseConvPos + new Vector3(gridX, 0));                               //Top
                Handles.DrawLine(mouseConvPos + new Vector3(gridX, 0), mouseConvPos + new Vector3(gridX, gridY));   //Right
                Handles.DrawLine(mouseConvPos + new Vector3(gridX, gridY), mouseConvPos + new Vector3(0, gridY));   //Down
                Handles.DrawLine(mouseConvPos + new Vector3(0, gridY), mouseConvPos);                               //Left

                //Draw selected objects
                if (selectedObjects != null)
                {
                    foreach (EditorObject obj in selectedObjects)
                    {
                        selObjPos = obj.reference.transform.position;
                        selObjSize = obj.reference.GetComponent<SpriteRenderer>().bounds.size;
                        Handles.color = selectColor;
                        Handles.DrawLine(selObjPos + new Vector3(-selObjSize.x, selObjSize.y) / 2, selObjPos + new Vector3(-selObjSize.x, selObjSize.y) / 2 + new Vector3(selObjSize.x, 0));                                                //Top
                        Handles.DrawLine(selObjPos + new Vector3(-selObjSize.x, selObjSize.y) / 2 + new Vector3(selObjSize.x, 0), selObjPos + new Vector3(-selObjSize.x, selObjSize.y) / 2 + new Vector3(selObjSize.x,-selObjSize.y));      //Right
                        Handles.DrawLine(selObjPos + new Vector3(-selObjSize.x, selObjSize.y) / 2 + new Vector3(selObjSize.x, -selObjSize.y), selObjPos + new Vector3(-selObjSize.x, -selObjSize.y) / 2);                                   //Down
                        Handles.DrawLine(selObjPos + new Vector3(-selObjSize.x, -selObjSize.y) / 2, selObjPos + new Vector3(-selObjSize.x, selObjSize.y) / 2);                                                                              //Left
                    }
                }

                Event e = Event.current;
                int controlID = GUIUtility.GetControlID(FocusType.Passive);
                switch (e.GetTypeForControl(controlID))
                {
                    case EventType.MouseDown:

                        if (e.button == 0)
                        {
                            GUIUtility.hotControl = controlID;

                            if (cursorState == CursorState.Add)
                                AddObject();

                            if (cursorState == CursorState.Remove)
                                RemoveObject();

                            if (cursorState == CursorState.Drag)
                                DragObject();

                            //Selected Object (priority to the selected layer)
                            selectedObject = FindMouseOverObject(selectedLayer);
                            if (selectedObject == null)
                            {
                                foreach (Layer l in layers)
                                {
                                    selectedObject = FindMouseOverObject(l);
                                    if (selectedObject != null)
                                        break;
                                }
                            }

                            //If it's only one element;
                            if (!e.shift)
                                selectedObjects.Clear();

                            if (selectedObject != null)
                            {
                                //Boundings
                                if (!e.shift)
                                    selectedObject.HighlightTileBoundings();

                                if (selectedObjects.Contains(selectedObject))
                                    selectedObjects.Remove(selectedObject);
                                else
                                    selectedObjects.Add(selectedObject);
                            }
                            
                            e.Use();
                        }
                        break;
                    case EventType.MouseUp:
                        if (e.button == 0)
                        {
                            GUIUtility.hotControl = 0;

                            if (cursorState == CursorState.Drag && dragObject != null)
                                dragObject = null;

                            e.Use();
                        }
                        break;
                    case EventType.MouseDrag:
                        if (e.button == 0)
                        {
                            GUIUtility.hotControl = controlID;
                            if (cursorState == CursorState.Add)
                                AddObject();

                            if (cursorState == CursorState.Remove)
                                RemoveObject();

                            if (cursorState == CursorState.Drag && dragObject != null)
                            {
                                dragObject.reference.transform.position = GetObjSnappedPosition(dragObject.reference);
                                dragObject.i = (int)mouseGridPos.x;
                                dragObject.j = (int)mouseGridPos.y;
                            }

                            e.Use();
                        }
                        break;
                    case EventType.KeyDown:
                        //Shortcut for removing
                        if (e.keyCode == KeyCode.R)
                        {
                            cursorState = CursorState.Remove;
                            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                        }
                        //Shortcut for selecting
                        if (e.keyCode == KeyCode.S)
                        {
                            cursorState = CursorState.Drag;
                            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                        }
                        
                        break;
                }

                Handles.EndGUI();

            }
        }

        public void AddObject()
        {
            GameObject obj;


            if (selectedLayer != null)
            {
                if (!selectedLayer.GetSceneElements().Exists(t => t.CompareIndexes((int)mouseGridPos.x, (int)mouseGridPos.y)) && selectedLayer != null)
                {
                    if (objMenu != null)
                    {
                        obj = (GameObject)Instantiate(objMenu.GetCurrentSelection(), mouseConvPos + new Vector3(gridX, gridY) / 2, Quaternion.identity);
                        obj.transform.position = GetObjSnappedPosition(obj);
                        selectedLayer.AddSceneElement(new EditorObject(obj, (int)mouseGridPos.x, (int)mouseGridPos.y, selectedLayer));
                    }
                }
            }
        }

        private Vector3 GetObjSnappedPosition(GameObject obj)
        {
            Vector3 refSize, position;

            refSize = obj.GetComponent<SpriteRenderer>().bounds.size;
            switch (snapState)
            {
                case SnapState.Center:
                    position = mouseConvPos + new Vector3(gridX, gridY) / 2;
                    break;
                case SnapState.Down:
                    position = mouseConvPos + new Vector3(gridX, gridY) / 2 + new Vector3(0, (-gridY + refSize.y) / 2, 0);
                    break;
                case SnapState.Left:
                    position = mouseConvPos + new Vector3(gridX, gridY) / 2 + new Vector3((-gridX + refSize.x) / 2, 0, 0);
                    break;
                case SnapState.Right:
                    position = mouseConvPos + new Vector3(gridX, gridY) / 2 + new Vector3((gridX - refSize.x) / 2, 0, 0);
                    break;
                case SnapState.Up:
                    position = mouseConvPos + new Vector3(gridX, gridY) / 2 + new Vector3(0, (gridY - refSize.y) / 2, 0); ;
                    break;
                case SnapState.UpLeft:
                    position = mouseConvPos + new Vector3(gridX, gridY) / 2 + new Vector3((-gridX + refSize.x), (gridY - refSize.y), 0)/2;
                    break;
                case SnapState.UpRight:
                    position = mouseConvPos + new Vector3(gridX, gridY) / 2 + new Vector3((gridX - refSize.x), (gridY - refSize.y), 0) / 2;
                    break;
                case SnapState.DownLeft:
                    position = mouseConvPos + new Vector3(gridX, gridY) / 2 + new Vector3((-gridX + refSize.x), (-gridY + refSize.y), 0)/2;
                    break;
                case SnapState.DownRight:
                    position = mouseConvPos + new Vector3(gridX, gridY) / 2 + new Vector3((gridX - refSize.x), (-gridY + refSize.y), 0) / 2;
                    break;
                default:
                    position = mouseConvPos + new Vector3(gridX, gridY) / 2;
                    break;
            }

            return position;
        }

        public void RemoveObject()
        {
            EditorObject obj;

            if (selectedLayer != null)
            {
                obj = FindMouseOverObject(selectedLayer);
                if (obj != null)
                {
                    selectedLayer.GetSceneElements().Remove(obj);
                    DestroyImmediate(obj.reference);
                }
            }
        }

        public void DragObject()
        {
            EditorObject obj;

            if (selectedLayer != null)
            {
                obj = FindMouseOverObject(selectedLayer);
                
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                if (obj != null && dragObject == null)
                {
                    dragObject = obj;
                }
            }
        }

        public EditorObject FindMouseOverObject(Layer layer)
        {
            EditorObject obj;
            Vector3 objSize, newMousePos;

            newMousePos = new Vector3(mousePosition.x, -mousePosition.y, mousePosition.z);
            obj = null;
            foreach (EditorObject o in layer.GetSceneElements())
            {
                objSize = o.reference.GetComponent<SpriteRenderer>().bounds.size;
                if (mousePosition.x > o.reference.transform.position.x - objSize.x / 2 && mousePosition.x < o.reference.transform.position.x + objSize.x / 2)
                {
                    if (-mousePosition.y > o.reference.transform.position.y - objSize.y / 2 && -mousePosition.y < o.reference.transform.position.y + objSize.y / 2)
                        obj = o;
                }
            }

            return obj;
        }

        void OnFocus()
        {
            //Debug.Log("focus");
           // if (!loaded)
             //   LoadData();
        }

        void OnLostFocus()
        {
            //Debug.Log("lost");
            //SaveData();
        }

        void OnEnable()
        {
            //Debug.Log("enable");
            
            //if (!loaded)
            LoadData();
            SceneView.onSceneGUIDelegate += this.OnSceneGUI;
        }

        void OnDisable()
        {
            //Debug.Log("disable");
            SaveData();
            SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
        }

        void OnDestroy()
        {
            //Debug.Log("destroy");
            SaveData();
        }

        public void SaveData()
        {
            string serializedMenu;

            EditorPrefs.SetInt(PlayerSettings.productName + "-GridSize", gridSize);
            serializedMenu = "";
            
            //Serialize menus
            for (int i = 0; i < menus.Count; i++)
            {
                serializedMenu += menus[i].Serialize();
                if (i < menus.Count - 1)
                    serializedMenu += ";";
            }

            EditorPrefs.SetString(PlayerSettings.productName + "-Menus", serializedMenu);

            EditorPrefs.SetString(PlayerSettings.productName + "-CursorColor", FromColorToString(cursorColor));

            EditorPrefs.SetString(PlayerSettings.productName + "-GridColor", FromColorToString(gridColor));

            EditorPrefs.SetString(PlayerSettings.productName + "-SelectColor", FromColorToString(selectColor));

            EditorPrefs.SetBool(PlayerSettings.productName + "-ShowGrid", showGrid);

            EditorPrefs.SetString(PlayerSettings.productName + "-SnapState", snapState.ToString());
        }

        public string FromColorToString(Color color)
        {
            return color.r + "," + color.g + "," + color.b + "," + color.a;
        }

        public Color FromStringToColor(string color)
        {
            string[] desColor;
            desColor = color.Split(',');

            return new Color(float.Parse(desColor[0]), float.Parse(desColor[1]), float.Parse(desColor[2]), float.Parse(desColor[3]));
        }

        public void LoadData()
        {
            GameObject[] layers, boundings;
            string serializedMenus;
            string[] stringMenus;
            EditorObjectsMenu obj;
            Layer newLayer;
            Vector2 eleGridPos;
            GameObject child;

            if (selectedObjects == null)
                selectedObjects = new List<EditorObject>();

            if (EditorPrefs.HasKey(PlayerSettings.productName + "-GridSize"))
                gridSize = EditorPrefs.GetInt(PlayerSettings.productName + "-GridSize");

            if (EditorPrefs.HasKey(PlayerSettings.productName + "-ShowGrid"))
                showGrid = EditorPrefs.GetBool(PlayerSettings.productName + "-ShowGrid");

            //Deserialize layers
            layers = GameObject.FindGameObjectsWithTag(Layer.LAYER_TAG);
            //if (this.layers == null)
            this.layers = new List<Layer>();

            foreach (GameObject l in layers)
            {
                newLayer = new Layer(l.name, l);
                for (int i = 0; i < l.transform.childCount; i++)
                {
                    child = l.transform.GetChild(i).gameObject;
                    eleGridPos = Helper.ConvertVectorToGrid(new Vector2(child.transform.position.x, -child.transform.position.y), gridSize/100f, gridSize / 100f);
                    newLayer.AddSceneElement(new EditorObject(child, (int)eleGridPos.x, (int)eleGridPos.y, newLayer));
                }

                this.layers.Add(newLayer);
            }

            //Deserialize menus
            if (EditorPrefs.HasKey(PlayerSettings.productName + "-Menus"))
            {
                //if (menus == null)
                menus = new List<EditorObjectsMenu>();

                serializedMenus = EditorPrefs.GetString(PlayerSettings.productName + "-Menus");
                stringMenus = serializedMenus.Split(';');
                foreach (string s in stringMenus)
                {
                    if (s != "")
                    {
                        obj = new EditorObjectsMenu();
                        obj.Deserialize(s);
                        obj.LoadPrefabs();
                        menus.Add(obj);
                    }
                }
            }

            if (EditorPrefs.HasKey(PlayerSettings.productName + "-CursorColor"))
                cursorColor = FromStringToColor(EditorPrefs.GetString(PlayerSettings.productName + "-CursorColor"));

            if (EditorPrefs.HasKey(PlayerSettings.productName + "-GridColor"))
                gridColor = FromStringToColor(EditorPrefs.GetString(PlayerSettings.productName + "-GridColor"));

            if (EditorPrefs.HasKey(PlayerSettings.productName + "-SelectColor"))
                selectColor = FromStringToColor(EditorPrefs.GetString(PlayerSettings.productName + "-SelectColor"));

            if (EditorPrefs.HasKey(PlayerSettings.productName + "-SnapState"))
                snapState = (SnapState)System.Enum.Parse(typeof(SnapState),EditorPrefs.GetString(PlayerSettings.productName + "-SnapState"));

            LoadInitialIcons();
            
            //Boundings
            //boundings = GameObject.Find(TILE_BOUNDING_CONTAINER_NAME);
            //foreach (GameObject o in boundings)
              //  o.GetComponent<TileBounding>().LinkAssociatedObjects();

            loaded = true;
        }
    }
}