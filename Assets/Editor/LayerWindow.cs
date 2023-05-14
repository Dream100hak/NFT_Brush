using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.UI;

public class LayerWindow : EditorWindow
{
    private Texture2D _gridTexture;
    private Vector2 _scrollPosition = Vector2.zero;

    private bool _isDragging = false;
    private KeyValuePair<int, LayerData> _draggingLayer = new KeyValuePair<int, LayerData>();
    private KeyValuePair<int, LayerData> _insertLayer = new KeyValuePair<int, LayerData>();

    private Dictionary<int, int> _prevLayerIndexs = new Dictionary<int, int>();

    private List<int> _layerIndexList = new List<int>();

    private DateTime _dragStartTime;

    [MenuItem("Photoshop/Layer")]
    public static void ShowWindow()
    {
        GetWindow<LayerWindow>("Layer");
    }

    private void OnEnable()
    {
        _gridTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Textures/Grid.png");

        BrushEditor.EnablePlacing();
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        Undo.undoRedoPerformed += OnUndoRedoPerformed;
        LayerEditor.ED.SelectedLayerIds.Clear();
        Debug.Log("Subscribed to Undo.undoRedoPerformed");

    }
    private void OnDisable()
    {
        BrushEditor.DisablePlacing();
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        Debug.Log("Unsubscribed from Undo.undoRedoPerformed");

        Debug.Log("해제");
    }

    private void OnUndoRedoPerformed()
    {
        Debug.Log("OnUndoRedoPerformed called");

        Transform cubeParent = BrushEditor.GetCubeParent();
        Dictionary<int, Transform> layers = new Dictionary<int, Transform>();
        
        for (int i = 0; i < cubeParent.childCount; i++)
        {
            Transform childLayer = cubeParent.GetChild(i);

            int id = childLayer.GetComponent<LayerData>().Id;
            string name = childLayer.GetComponent<LayerData>().Name;

            if (!layers.ContainsKey(id))
                layers.Add(id, childLayer);

            childLayer.name = name;

            //Dragged Undo check
            if(_prevLayerIndexs.ContainsKey(id))
            {
                childLayer.SetSiblingIndex(_prevLayerIndexs[id]);
            }
        }

        _prevLayerIndexs.Clear();


        var destroyedLayers = LayerEditor.LayerObjects.Where(x => x.Value == null).Select(x => x.Key).ToList();
        var createdLayers = layers.Keys.Except(LayerEditor.LayerObjects.Keys).ToList();

        if (destroyedLayers.Count > 0)
        {
            foreach (int id in destroyedLayers)
            {
                LayerEditor.ToDeleteLayerIds.Add(id);
                LayerEditor.EmptyLayerIds.Add(id);
            }
        }
        if (createdLayers.Count > 0)
        {
            foreach (int id in createdLayers)
            {
                if (LayerEditor.ToRestoreLayerIds.ContainsKey(id) == false)
                    LayerEditor.ToRestoreLayerIds.Add(id, layers[id]);
                LayerEditor.EmptyLayerIds.Remove(id);
            }
        }
        Repaint();
    }
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Tools.current = Tool.Move;
            BrushEditor.DisablePlacing();
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            BrushEditor.EnablePlacing();
        }
    }
    public Texture2D CaptureLayerSnapshot(Transform layer)
    {
        int width = 64;
        int height = 64;

        Camera tempCamera = new GameObject("TempCamera").AddComponent<Camera>();
        Camera mainCamera = Camera.main;

        tempCamera.transform.position = mainCamera.transform.position;
        tempCamera.transform.rotation = mainCamera.transform.rotation;
        tempCamera.orthographic = true;
        tempCamera.orthographicSize = mainCamera.orthographicSize;
        tempCamera.aspect = mainCamera.aspect;
        tempCamera.clearFlags = CameraClearFlags.SolidColor;
        tempCamera.backgroundColor = Color.clear;

        int canvasLayer = LayerMask.NameToLayer("Canvas");
        tempCamera.cullingMask = 1 << canvasLayer;

        Transform cubeParent = BrushEditor.GetCubeParent();
        List<bool> layerStates = new List<bool>();
        for (int i = 0; i < cubeParent.childCount; i++)
        {
            Transform childLayer = cubeParent.GetChild(i);
            layerStates.Add(childLayer.gameObject.activeSelf);
            if (childLayer != layer)
            {
                childLayer.gameObject.SetActive(false);
            }
        }

        RenderTexture renderTexture = new RenderTexture(width, height, 24);
        GameObject gridQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        gridQuad.layer = LayerMask.NameToLayer("Canvas");
        gridQuad.transform.position = Vector3.zero;
        gridQuad.transform.rotation = tempCamera.transform.rotation;
        gridQuad.transform.localScale = new Vector3(50, 50, 1.0f);
        gridQuad.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/Transparent"));
        gridQuad.GetComponent<Renderer>().sharedMaterial.mainTexture = _gridTexture;
        gridQuad.GetComponent<Renderer>().sharedMaterial.color = Color.gray;

        tempCamera.targetTexture = renderTexture;
        tempCamera.Render();

        RenderTexture.active = renderTexture;
        Texture2D snapshot = new Texture2D(width, height, TextureFormat.RGB24, false);
        snapshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        snapshot.Apply();

        RenderTexture.active = null;
        DestroyImmediate(tempCamera.gameObject);
        DestroyImmediate(renderTexture);
        DestroyImmediate(gridQuad);

        for (int i = 0; i < cubeParent.childCount; i++)
        {
            Transform childLayer = cubeParent.GetChild(i);
            childLayer.gameObject.SetActive(layerStates[i]);
        }
        return snapshot;
    }
    public void OnGUI()
    {
        if (FindObjectOfType<CubePlacer>() == null)
        {
            CreateCanvas();
            return;
        }

        if (BrushEditor.GetCubeParent() != null)
            MakeLayerList();
        
        GUILayout.Space(20);
        DeleteAllLayers();
        GUILayout.Space(20);

        CreateNewLayer();
    }
    private static void CreateCanvas()
    {
        if (GUILayout.Button("캔버스 만들기", GUILayout.Height(100)))
        {
            Camera main = Camera.main;
            main.clearFlags = CameraClearFlags.SolidColor;
            main.backgroundColor = Color.black;
            main.orthographic = true;
            main.orthographicSize = 10.2f;
            main.transform.position = new Vector3(0, 0, -10);

            if (main.GetComponent<FitToScreen>() == null)
                main.AddComponent<FitToScreen>();

            GameObject canvas = new GameObject("Canvas");
            GameObject collider = new GameObject("Collider");
            canvas.AddComponent<CubePlacer>();
            canvas.GetComponent<CubePlacer>().CubePrefab = Resources.Load<GameObject>("Prefab/Cube");
            collider.AddComponent<BoxCollider>();
            collider.GetComponent<BoxCollider>().isTrigger = true;
            collider.GetComponent<BoxCollider>().size = new Vector3(100, 100, 0.2f);

            SpriteRenderer circle = new GameObject("Circle").AddComponent<SpriteRenderer>();
        }
    }
    private void CreateNewLayer()
    {
        if (GUILayout.Button("Create New Layer", GUILayout.Height(60)))
        {
            LayerEditor.ED.SelectedLayerIds.Clear();
            LayerEditor.CreateNewLayer();
            GUIUtility.keyboardControl = 0;
        }
    }

    private void MakeLayerList()
    {
        bool clickedInsideLayer = false;

        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true);

        _layerIndexList.Clear();
        foreach (var layerPair in LayerEditor.LayerObjects)
            _layerIndexList.Add(layerPair.Key);

        var sortedLayerObjects = _layerIndexList
              .Select(x => new KeyValuePair<int, Transform>(x, LayerEditor.LayerObjects[x]))
              .Where(x => x.Value != null)
              .OrderByDescending(x => x.Value.GetComponent<LayerData>().CreationTimestamp)
              .ToList();

        List<int> tempLayerIndexList = new List<int>(_layerIndexList);
        List<LayerData> layerDatas = LayerEditor.GetLayerDatas();

        foreach (var layerPair in sortedLayerObjects)
        {
            int i = layerPair.Key;
            Transform layer = layerPair.Value;

            if (layer == null)
                continue;

            LayerData layerData = layer.GetComponent<LayerData>();
            Color originBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.clear;

            foreach (var id in LayerEditor.ED.SelectedLayerIds)
            {
                if(id == i)
                {
                    GUI.backgroundColor = Color.gray;
                    break;
                }     
            }

            GUILayout.BeginHorizontal(GUI.skin.box);

            Texture2D layerSnapshot = CaptureLayerSnapshot(layer); // 스냅샷 관련
            Rect imageRect = GUILayoutUtility.GetRect(50, 50);
            GUI.DrawTexture(imageRect, layerSnapshot, ScaleMode.ScaleToFit);
            ChangeLayerName(layerData); // 이름 변경 관련
            GUILayout.FlexibleSpace();
            OneDeleteLayer(layer); // 레이어 삭제 관련

            GUILayout.EndHorizontal();

            Rect layerRect = GUILayoutUtility.GetLastRect();
            layerRect.width = EditorGUIUtility.currentViewWidth;

            if (layerRect.x != 0  && layerRect.y != 0 )
                layerData.LayerRect = layerRect;

            if (_isDragging &&  layerData.LayerRect.Contains(Event.current.mousePosition))
                EditorGUI.DrawRect(new Rect(layerData.LayerRect.x, layerData.LayerRect.y + layerData.LayerRect.height, layerData.LayerRect.width, 1),new Color32(51, 243, 255, 255));
        
            if (Event.current.type == EventType.MouseDown && layerRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.button == 0)
                {
                    _draggingLayer = new KeyValuePair<int, LayerData>(i, layerData);
                    _dragStartTime = DateTime.Now;
                    clickedInsideLayer = true;

                    if (Event.current.control)
                        InsertControlLayerId(i);
                    
                    else if (Event.current.shift)
                        InsertShiftLayerId(i);            
                    else
                    {
                        LayerEditor.ED.SelectedLayerIds.Clear();
                        LayerEditor.ED.SelectedLayerIds.Add(i);
                    }
                }
                Event.current.Use();
            }

            if (Event.current.type == EventType.MouseDrag && layerRect.Contains(Event.current.mousePosition))
            {
                TimeSpan dragDuration = DateTime.Now - _dragStartTime;

                if (dragDuration.TotalSeconds >= 0.1f)
                    _isDragging = true;
            }

            if (Event.current.type == EventType.MouseUp)
                _isDragging = false; 

            GUI.backgroundColor = originBackgroundColor;
        }

        if (Event.current.type == EventType.MouseDown && !clickedInsideLayer)
            LayerEditor.ED.SelectedLayerIds.Clear();

        if (_isDragging && _draggingLayer.Value != null)
        {
            DrawLayerAtMouse(_draggingLayer.Value, Event.current.mousePosition);

            foreach (var layerData in layerDatas)
            {
                if (_draggingLayer.Value == layerData)
                    continue;

                if(layerData.LayerRect.Contains(Event.current.mousePosition))
                {
                    _insertLayer = new KeyValuePair<int, LayerData>(layerData.Id, layerData);
                    break;
                }
            }
        }

         if (Event.current.type == EventType.MouseUp && _insertLayer.Value != null)
        {
            _prevLayerIndexs.Clear();

            LayerData draggedLayerData = _draggingLayer.Value;
            LayerData insertLayerData = _insertLayer.Value;

            UnityEngine.Object[] recordObjs = new UnityEngine.Object[] { draggedLayerData, insertLayerData };
            Undo.RecordObjects(recordObjs, "Dragged Layer");

            _insertLayer = new KeyValuePair<int, LayerData>();
            _draggingLayer = new KeyValuePair<int, LayerData>();

            long insertTime = insertLayerData.CreationTimestamp;
            Rect insertRect = insertLayerData.LayerRect;
            int insertIndex = insertLayerData.transform.GetSiblingIndex();
            int draggedIndex = draggedLayerData.transform.GetSiblingIndex();

            insertLayerData.CreationTimestamp = draggedLayerData.CreationTimestamp;
            draggedLayerData.CreationTimestamp = insertTime;

            insertLayerData.LayerRect = draggedLayerData.LayerRect;
            draggedLayerData.LayerRect = insertRect;

            insertLayerData.transform.SetSiblingIndex(draggedIndex);
            draggedLayerData.transform.SetSiblingIndex(insertIndex);

            _prevLayerIndexs.Add(draggedLayerData.Id, draggedIndex);
            _prevLayerIndexs.Add(insertLayerData.Id, insertIndex);
 
            GUIUtility.keyboardControl = 0;
            Event.current.Use();

        }

        LayerEditor.DeleteLayerIds();
        LayerEditor.RestoreLayerIds();
        GUILayout.EndScrollView();

        Repaint();
    }
    
    private void InsertControlLayerId(int id)
    {
        Undo.RecordObject(LayerEditor.ED, "Control Layer Selection");

        if (LayerEditor.ED.SelectedLayerIds.Contains(id))
            LayerEditor.ED.SelectedLayerIds.Remove(id);
        else
            LayerEditor.ED.SelectedLayerIds.Add(id);
    }
    private void InsertShiftLayerId(int id)
    {
        Undo.RecordObject(LayerEditor.ED, "Shift Layer Selection");

        if (LayerEditor.ED.SelectedLayerIds.Any())
        {
            List<LayerData> layerDatas = LayerEditor.GetLayerOrders();
            int firstSelectedIndex = layerDatas.FindIndex(layerData => LayerEditor.ED.SelectedLayerIds.Contains(layerData.Id));
            int currentLayerIndex = layerDatas.FindIndex(layerData => layerData.Id == id);
            int startIndex = Math.Min(firstSelectedIndex, currentLayerIndex);
            int endIndex = Math.Max(firstSelectedIndex, currentLayerIndex);

            LayerEditor.ED.SelectedLayerIds.Clear();

            for (int i = startIndex; i <= endIndex; i++)
                LayerEditor.ED.SelectedLayerIds.Add(layerDatas[i].Id);
   
        }
        else
        {
            LayerEditor.ED.SelectedLayerIds.Add(id);
        }
    }

    private void DrawLayerAtMouse(LayerData layerData, Vector2 mousePosition)
    {
        GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, 0.75f);

        float width = EditorGUIUtility.currentViewWidth / 2;
        float height = 200;
        GUILayout.BeginArea(new Rect(mousePosition.x - width / 2, mousePosition.y - height / 8, width, height));
        GUILayout.BeginHorizontal(GUI.skin.box);

        Texture2D snapshot = CaptureLayerSnapshot(layerData.transform); // 스냅샷 관련

        Rect imageRect = GUILayoutUtility.GetRect(50, 50);
        GUI.DrawTexture(imageRect, snapshot, ScaleMode.ScaleToFit);

        ChangeLayerName(layerData); // 이름 변경 관련

        GUILayout.FlexibleSpace();

        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        GUI.color = Color.white;
    }

    private void OneDeleteLayer(Transform layer)
    {
        if (GUILayout.Button("Delete", GUILayout.Width(50)))
        {
            int layerIndex = LayerEditor.LayerObjects.FirstOrDefault(x => x.Value == layer).Key;
            LayerEditor.ToDeleteLayerIds.Add(layerIndex);
            LayerEditor.EmptyLayerIds.Add(layerIndex);

            GUIUtility.keyboardControl = 0;
            LayerEditor.ED.SelectedLayerIds.Clear();
            _isDragging = false;

            Undo.DestroyObjectImmediate(layer.gameObject);
            Repaint();
        }
    }
    private void DeleteAllLayers()
    {
        if (GUILayout.Button("Delete All Layers"))
        {
            GameObject canvas = GameObject.Find("Canvas");

            if (canvas != null)
            {
                Transform[] children = canvas.GetComponentsInChildren<Transform>();

                List<Transform> targetTransforms = children.Where(child => child.gameObject != canvas && child.gameObject.name != "Cube").ToList();

                foreach (Transform target in targetTransforms)
                    Undo.DestroyObjectImmediate(target.gameObject);
            }

            Debug.Log("Layers has been reset.");
            LayerEditor.Clear();
            LayerEditor.ED.SelectedLayerIds.Clear();
            _isDragging = false;
        }
    }

    private void ChangeLayerName(LayerData layerData)
    {
        string layerName = layerData.Name;
        layerName = Utils.EditPropertyWithUndo(
            "",
            layerName,
            newName => layerData.Name = newName,
            (label, value) => EditorGUILayout.TextField(value, GUILayout.Width(120)),
            layerData
        );
    }

}
