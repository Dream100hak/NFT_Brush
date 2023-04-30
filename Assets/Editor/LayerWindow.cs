using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;

public class LayerWindow : EditorWindow
{
    private DateTime _lastUndoRedoCall = DateTime.MinValue;
    //배경이 투명할 때 쓸 격자 이미지
    private Texture2D _gridTexture;
    private Vector2 _scrollPosition = Vector2.zero; // 추가된 코드

    public static int s_selectedLayerIndex = -1;
    private bool _scrollToNewLayer = false;
    private float _layersTotalHeight;

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
        if ((DateTime.Now - _lastUndoRedoCall).TotalMilliseconds > 1000)
        {
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
        var cubePlacer = UnityEngine.Object.FindObjectOfType<CubePlacer>();
        if (cubePlacer == null)
        {
            if (GUILayout.Button("캔버스 만들기", GUILayout.Height(100)))
                CreateCanvas();

            return;
        }

        Transform cubeParent = BrushEditor.GetCubeParent();

        if (cubeParent != null)
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true);
            _layersTotalHeight = 0;

            var sortedLayerObjects = BrushEditor.LayerObjects
                  .Where(x => x.Value != null)
                  .OrderByDescending(x => x.Value.GetComponent<LayerData>().CreationTimestamp)
                  .ToList();

            foreach (var layerPair in sortedLayerObjects)
            {
                int i = layerPair.Key;
                Transform layer = layerPair.Value;

                if (layer == null)
                    continue;

                Color originalBackgroundColor = GUI.backgroundColor;
                GUI.backgroundColor = (i == s_selectedLayerIndex) ? Color.gray : Color.clear;

                GUILayout.BeginHorizontal(GUI.skin.box);

                Texture2D layerSnapshot = CaptureLayerSnapshot(layer);

                Rect imageRect = GUILayoutUtility.GetRect(50, 50);
                GUI.DrawTexture(imageRect, layerSnapshot, ScaleMode.ScaleToFit);

                LayerData layerData = layer.GetComponent<LayerData>();

                //레이어 이름 변경
                string layerName = layerData.Name;
                layerName = Utils.EditPropertyWithUndo(
                    "",
                    layerName,
                    newName => layerData.Name = newName,
                    (label, value) => EditorGUILayout.TextField(value, GUILayout.Width(120)),
                    layerData
                );

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Delete", GUILayout.Width(50)))
                {
                    OneDeleteLayer(layer);
                }

                GUILayout.EndHorizontal();

                Rect layerRect = GUILayoutUtility.GetLastRect();
                layerRect.width = EditorGUIUtility.currentViewWidth;
                if (Event.current.type == EventType.MouseDown && layerRect.Contains(Event.current.mousePosition))
                {
                    s_selectedLayerIndex = layerData.Id;
                    Event.current.Use();
                }

                GUI.backgroundColor = originalBackgroundColor;
                _layersTotalHeight += layerRect.height + GUI.skin.box.margin.vertical;
            }

            BrushEditor.DeleteLayerIds();
            BrushEditor.RestoreLayerIds();
            GUILayout.EndScrollView();
            UpdateScrollView();
        }

        GUILayout.Space(20);

        if (GUILayout.Button("Delete All Layers"))
        {
            DeleteAllLayers();
        }

        GUILayout.Space(20);

        if (GUILayout.Button("Create New Layer", GUILayout.Height(60)))
        {
            BrushEditor.CreateNewLayer();
            GUIUtility.keyboardControl = 0;
            _scrollToNewLayer = true;
        }
    }
    private static void CreateCanvas()
    {
        Camera main = Camera.main;
        main.orthographic = true;
        main.orthographicSize = 10.2f;
        main.transform.position = new Vector3(0, 0, -10);

        GameObject canvas = new GameObject("Canvas");
        GameObject collider = new GameObject("Collider");
        canvas.AddComponent<CubePlacer>();
        canvas.GetComponent<CubePlacer>().CubePrefab = Resources.Load<GameObject>("Prefab/Cube");
        collider.AddComponent<BoxCollider>();
        collider.GetComponent<BoxCollider>().isTrigger = true;
        collider.GetComponent<BoxCollider>().size = new Vector3(100, 100, 0.2f);

        Camera drawCam = new GameObject("DrawCamera").AddComponent<Camera>();
        SpriteRenderer circle = new GameObject("Circle").AddComponent<SpriteRenderer>();
        
    }
    private void OneDeleteLayer(Transform layer)
    {
        int layerIndex = BrushEditor.LayerObjects.FirstOrDefault(x => x.Value == layer).Key;
        BrushEditor.ToDeleteLayerIds.Add(layerIndex);
        BrushEditor.EmptyLayerIds.Add(layerIndex);

        GUIUtility.keyboardControl = 0;
        s_selectedLayerIndex = -1;

        Undo.DestroyObjectImmediate(layer.gameObject);
        Repaint();
    }

    private void DeleteAllLayers()
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
            Repaint();
        }
    }
}
