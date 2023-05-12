using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.UI;
using static UnityEditor.Experimental.GraphView.GraphView;

public class LayerWindow : EditorWindow
{
    private Texture2D _gridTexture;
    private Vector2 _scrollPosition = Vector2.zero; 

    public static int s_selectedLayerIndex = -1;
    private bool _scrollToNewLayer = false;
    private float _layersTotalHeight;

    private KeyValuePair<int, LayerData> _draggingLayer = new KeyValuePair<int, LayerData>();
    private KeyValuePair<int, LayerData> _insertLayer = new KeyValuePair<int, LayerData>();

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
        Debug.Log("Subscribed to Undo.undoRedoPerformed");

    }
    private void OnDisable()
    {
        BrushEditor.DisablePlacing();
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
         Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        Debug.Log("Unsubscribed from Undo.undoRedoPerformed");
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
        }

        var destroyedLayers = BrushEditor.LayerObjects.Where(x => x.Value == null).Select(x => x.Key).ToList();
        var createdLayers = layers.Keys.Except(BrushEditor.LayerObjects.Keys).ToList();

        if (destroyedLayers.Count > 0)
        {
            foreach (int id in destroyedLayers)
            {
                BrushEditor.ToDeleteLayerIds.Add(id);
                BrushEditor.EmptyLayerIds.Add(id);
            }
        }
        if (createdLayers.Count > 0)
        {
            foreach (int id in createdLayers)
            {
                if (BrushEditor.ToRestoreLayerIds.ContainsKey(id) == false)
                    BrushEditor.ToRestoreLayerIds.Add(id, layers[id]);
                BrushEditor.EmptyLayerIds.Remove(id);
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
        // 원래 레이어 상태로 복구
        for (int i = 0; i < cubeParent.childCount; i++)
        {
            Transform childLayer = cubeParent.GetChild(i);
            childLayer.gameObject.SetActive(layerStates[i]);
        }
        return snapshot;
    }

    public void OnGUI()
    {
        var cubePlacer = FindObjectOfType<CubePlacer>();
        if (cubePlacer == null)
        {
            CreateCanvas();
            return;
        }

        Transform cubeParent = BrushEditor.GetCubeParent();

        if (cubeParent != null)
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
            BrushEditor.CreateNewLayer();
            GUIUtility.keyboardControl = 0;
            _scrollToNewLayer = true;
        }
    }
    bool _isDragging = false; // 새로운 변수 추가
    private void MakeLayerList()
    {
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true);
        _layersTotalHeight = 0;

        _layerIndexList.Clear();
        foreach (var layerPair in BrushEditor.LayerObjects)
            _layerIndexList.Add(layerPair.Key);

        var sortedLayerObjects = _layerIndexList
              .Select(x => new KeyValuePair<int, Transform>(x, BrushEditor.LayerObjects[x]))
              .Where(x => x.Value != null)
              .OrderByDescending(x => x.Value.GetComponent<LayerData>().CreationTimestamp)
              .ToList();

        List<int> tempLayerIndexList = new List<int>(_layerIndexList);

        List<LayerData> layerDatas = BrushEditor.LayerObjects.Select(x => x.Value.GetComponent<LayerData>()).ToList();

        foreach (var layerPair in sortedLayerObjects)
        {
            int i = layerPair.Key;
            Transform layer = layerPair.Value;

            if (layer == null)
                continue;

            LayerData layerData = layer.GetComponent<LayerData>();

            Color originBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = (i == s_selectedLayerIndex) ? Color.gray : Color.clear;

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

            if(layerRect.x != 0  && layerRect.y != 0 )
                layerData.LayerRect = layerRect;

            if (_isDragging &&  layerData.LayerRect.Contains(Event.current.mousePosition))
                EditorGUI.DrawRect(new Rect(layerData.LayerRect.x, layerData.LayerRect.y + layerData.LayerRect.height, layerData.LayerRect.width, 1),new Color32(51, 243, 255, 255));
        
            if (Event.current.type == EventType.MouseDown && layerRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.button == 0)
                {
                    _draggingLayer = new KeyValuePair<int, LayerData>(i, layerData);
                    s_selectedLayerIndex = i;

                    _dragStartTime = DateTime.Now; // 드래그를 시작한 시간을 저장합니다.
                }
                Event.current.Use();
            }

            if (Event.current.type == EventType.MouseDrag)
            {
                TimeSpan dragDuration = DateTime.Now - _dragStartTime;

                if (dragDuration.TotalSeconds >= 0.1f)
                    _isDragging = true;
            }
            

            if (Event.current.type == EventType.MouseUp)
                _isDragging = false; 

            GUI.backgroundColor = originBackgroundColor;
            _layersTotalHeight += layerRect.height + GUI.skin.box.margin.vertical;
        }

        if (_isDragging && _draggingLayer.Value != null)
            DrawLayerAtMouse(_draggingLayer.Value, Event.current.mousePosition);

        if (_isDragging && _draggingLayer.Value != null)
        {
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
            LayerData draggedLayerData = _draggingLayer.Value;
            LayerData insertLayerData = _insertLayer.Value;

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

            GUIUtility.keyboardControl = 0;
            Event.current.Use();

        }

        Repaint();

        BrushEditor.DeleteLayerIds();
        BrushEditor.RestoreLayerIds();
        GUILayout.EndScrollView();
        UpdateScrollView();
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
            int layerIndex = BrushEditor.LayerObjects.FirstOrDefault(x => x.Value == layer).Key;
            BrushEditor.ToDeleteLayerIds.Add(layerIndex);
            BrushEditor.EmptyLayerIds.Add(layerIndex);

            GUIUtility.keyboardControl = 0;
            s_selectedLayerIndex = -1;
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
            s_selectedLayerIndex = -1;
            BrushEditor.Clear();
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
    private void UpdateScrollView()
    {
        if (_scrollToNewLayer)
        {
            Transform cubeParent = BrushEditor.GetCubeParent();

            int selectedIndex = cubeParent.childCount - 1 - s_selectedLayerIndex;

            if (selectedIndex >= 0)
            {
                float elementHeight = 70;
                float totalHeight = elementHeight * selectedIndex;
                float halfWindowHeight = position.height * 0.5f;

                _scrollPosition.y = Mathf.Clamp(totalHeight - halfWindowHeight, 0, Mathf.Max(0, _layersTotalHeight - position.height));
            }
            _scrollToNewLayer = false;
            _isDragging = false;
            Repaint();
        }
    }
}
