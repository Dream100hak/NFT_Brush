using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using Unity.VisualScripting;
using System.IO;
using static UnityEditor.Experimental.GraphView.GraphView;

public class LayerWindow : EditorWindow
{
    private Texture2D _gridTexture;
    private Vector2 _scrollPosition = Vector2.zero;

    private bool _isDragging = false;
    private KeyValuePair<int, LayerData> _draggingLayer = new KeyValuePair<int, LayerData>();
    private KeyValuePair<int, LayerData> _insertLayer = new KeyValuePair<int, LayerData>();

    private Dictionary<int, int> _prevLayerIndexs = new Dictionary<int, int>();

    private bool _multiSelected = false;

    
    
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

        CustomBrushEditor.EnablePlacing();
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        Undo.undoRedoPerformed += OnUndoRedoPerformed;
        CustomLayerEditor.ED.SelectedLayerIds.Clear();
        Debug.Log("Subscribed to Undo.undoRedoPerformed");

    }
    private void OnDisable()
    {
        CustomBrushEditor.DisablePlacing();
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        Debug.Log("Unsubscribed from Undo.undoRedoPerformed");
    }

    private void OnUndoRedoPerformed()
    {
        Debug.Log("OnUndoRedoPerformed called");

        Utils.UndoExecute();
        CustomLayerEditor.SetLayerChanged();

        Repaint();
    }
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Tools.current = Tool.Move;
            CustomBrushEditor.DisablePlacing();
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            CustomBrushEditor.EnablePlacing();
        }
    }
    public Texture2D CaptureLayerSnapshot(LayerData layer)
    {
        if (layer.HasChanged == false && layer.SnapShot != null)
        {
            return layer.SnapShot;
        }

        int width = 50;
        int height = 50;

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

        Transform cubeParent = CustomBrushEditor.GetCubeParent();
        List<bool> layerStates = new List<bool>();
        for (int i = 0; i < cubeParent.childCount; i++)
        {
            Transform childLayer = cubeParent.GetChild(i);
            layerStates.Add(childLayer.gameObject.activeSelf);
            if (childLayer != layer.transform)
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

        layer.HasChanged = false;
        layer.SnapShot = snapshot;

        return snapshot;
    }
    public void OnGUI()
    {
        if (FindObjectOfType<CubePlacer>() == null)
        {
            CreateCanvas();
            return;
        }

        if (CustomBrushEditor.GetCubeParent() != null)
            MakeLayerList();

        Rect seperateRect = GUILayoutUtility.GetLastRect();
        seperateRect.y += seperateRect.height;
        seperateRect.height = 3;

        EditorGUI.DrawRect(seperateRect, new Color(0.3f, 0.3f, 0.3f));

        GUILayout.Space(10);
        GUILayout.BeginHorizontal(GUI.skin.box);

        GUILayout.FlexibleSpace();
        CreateNewLayer();
        DisplayCopyButtons(); // 레이어 복제 버튼 추가
        DeleteSelectedLayer();
  
        GUILayout.EndHorizontal();

    }
    private static void CreateCanvas()
    {
        if (GUILayout.Button("캔버스 만들기", GUILayout.Height(100)))
        {
            Camera main = Camera.main;
            main.clearFlags = CameraClearFlags.SolidColor;
            main.backgroundColor = Color.clear;
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
        Texture2D icon = Resources.Load<Texture2D>("Textures/Icon/LayerIcon"); // 휴지통 이미지를 불러옵니다. 이 경로는 이미지의 실제 위치에 따라 달라집니다.
        GUIContent btnContent = new GUIContent(icon); // 휴지통 이미지를 버튼의 내용으로 설정합니다.

        if (GUILayout.Button(btnContent, GUILayout.Width(40), GUILayout.Height(40))) // 버튼의 크기를 100x100으로 설정합니다.
        {
            CustomLayerEditor.ED.SelectedLayerIds.Clear();
            CustomLayerEditor.CreateNewLayer();
            GUIUtility.keyboardControl = 0;
        }
    }

    private void MakeLayerList()
    {
        bool clickedInsideLayer = false;
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true);

        _layerIndexList.Clear();
        foreach (var layerPair in CustomLayerEditor.LayerObjects)
            _layerIndexList.Add(layerPair.Key);

        var sortedLayerObjects = _layerIndexList
              .Select(x => new KeyValuePair<int, Transform>(x, CustomLayerEditor.LayerObjects[x]))
              .Where(x => x.Value != null)
              .OrderByDescending(x => x.Value.GetComponent<LayerData>().CreationTimestamp)
              .ToList();

        List<int> tempLayerIndexList = new List<int>(_layerIndexList);
        List<LayerData> layerDatas = CustomLayerEditor.GetLayerDatas();

        foreach (var layerPair in sortedLayerObjects)
        {
            int i = layerPair.Key;
            Transform layer = layerPair.Value;

            if (layer == null)
                continue;

            LayerData layerData = layer.GetComponent<LayerData>();
            Color originBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.clear;

            foreach (var id in CustomLayerEditor.ED.SelectedLayerIds)
            {
                if(id == i)
                {
                    GUI.backgroundColor = new Color32(100,100,100,255);
                    break;
                }     
            }

            GUILayout.BeginHorizontal(CustomLayerStyle.WhiteSkinBoxStyle());

            Texture2D layerSnapshot = CaptureLayerSnapshot(layerData); // 스냅샷 관련
            Rect imageRect = GUILayoutUtility.GetRect(50, 50);
            GUI.DrawTexture(imageRect, layerSnapshot, ScaleMode.ScaleToFit);
            ChangeLayerName(layerData); // 이름 변경 관련
            GUILayout.FlexibleSpace();

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

                    if (Event.current.shift)
                        InsertShiftLayerId(i);            
                    else
                        InsertControlLayerId(i);

                    CustomLayerEditor.SelectLayerObjects();
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
            CustomLayerStyle.DrawSeparatorLine(-3, 1.7f, new Color(0.1f, 0.1f, 0.1f, 0.5f));

            GUILayout.Space(-5);
        }


        //레이어가 클릭되지 않은 경우 
        if (Event.current.type == EventType.MouseDown && !clickedInsideLayer)
        {
            List<int> prevSelectedLayerIds = new List<int>(CustomLayerEditor.ED.SelectedLayerIds);

            CustomLayerEditor.ED.SelectedLayerIds.Clear();
            Selection.activeObject = null;

            Utils.UndoStack.Push(() => 
                {
                    foreach (int prevId in prevSelectedLayerIds)
                    {
                        if (!CustomLayerEditor.ED.SelectedLayerIds.Contains(prevId))
                            CustomLayerEditor.ED.SelectedLayerIds.Add(prevId);
                    }
                });
        }

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
            MoveSelectedLayer();
  
        CustomLayerEditor.DeleteLayerIds();
        CustomLayerEditor.RestoreLayerIds();
        GUILayout.EndScrollView();
        Repaint();
    }
    
    private void InsertControlLayerId(int id)
    {
        if(Event.current.control || Event.current.command)
        {
            Undo.RecordObject(CustomLayerEditor.ED, "Control Layer Selection");

            if (CustomLayerEditor.ED.SelectedLayerIds.Contains(id))
                CustomLayerEditor.ED.SelectedLayerIds.Remove(id);
            else
                CustomLayerEditor.ED.SelectedLayerIds.Add(id);

            _multiSelected = true;
        }
        else
        {
            Undo.RecordObject(CustomLayerEditor.ED,  "Layer Selection");

            CustomLayerEditor.ED.SelectedLayerIds.Clear();
            CustomLayerEditor.ED.SelectedLayerIds.Add(id);   
        }

        Utils.UndoStack.Push(() => 
        {
            if(CustomLayerEditor.ED.SelectedLayerIds.Contains(id))
                CustomLayerEditor.ED.SelectedLayerIds.Remove(id);
        });
    }
    private void InsertShiftLayerId(int id)
    {
        List<int> prevSelectedLayerIds = new List<int>(CustomLayerEditor.ED.SelectedLayerIds);

        Undo.RecordObject(CustomLayerEditor.ED, "Shift Layer Selection");

        if (CustomLayerEditor.ED.SelectedLayerIds.Any())
        {
            List<LayerData> layerDatas = CustomLayerEditor.GetLayerOrders();
            int firstSelectedIndex = layerDatas.FindIndex(layerData => CustomLayerEditor.ED.SelectedLayerIds.Contains(layerData.Id));
            int currentLayerIndex = layerDatas.FindIndex(layerData => layerData.Id == id);
            int startIndex = Math.Min(firstSelectedIndex, currentLayerIndex);
            int endIndex = Math.Max(firstSelectedIndex, currentLayerIndex);

            CustomLayerEditor.ED.SelectedLayerIds.Clear();

            for (int i = startIndex; i <= endIndex; i++)
                CustomLayerEditor.ED.SelectedLayerIds.Add(layerDatas[i].Id);

            Utils.UndoStack.Push(() =>
            {
                for (int i = startIndex; i <= endIndex; i++)
                {
                    if (CustomLayerEditor.ED.SelectedLayerIds.Contains(layerDatas[i].Id))
                    CustomLayerEditor.ED.SelectedLayerIds.Remove(layerDatas[i].Id);
                }

                foreach (int prevId in prevSelectedLayerIds)
                {
                    if (!CustomLayerEditor.ED.SelectedLayerIds.Contains(prevId))
                        CustomLayerEditor.ED.SelectedLayerIds.Add(prevId);
                }
            });
        }
        else
        {
            CustomLayerEditor.ED.SelectedLayerIds.Add(id);

            Utils.UndoStack.Push(() =>
            {
                if (CustomLayerEditor.ED.SelectedLayerIds.Contains(id))
                    CustomLayerEditor.ED.SelectedLayerIds.Remove(id);
            });
        } 
    }

    private void DrawLayerAtMouse(LayerData layerData, Vector2 mousePosition)
    {
        GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, 0.75f);

        float width = EditorGUIUtility.currentViewWidth / 2;
        float height = 200;
        GUILayout.BeginArea(new Rect(mousePosition.x - width / 2, mousePosition.y - height / 8, width, height));
        GUILayout.BeginHorizontal(GUI.skin.box);
        Texture2D snapshot = CaptureLayerSnapshot(layerData); // 스냅샷 관련
        Rect imageRect = GUILayoutUtility.GetRect(50, 50);
        GUI.DrawTexture(imageRect, snapshot, ScaleMode.ScaleToFit);
        ChangeLayerName(layerData); 
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        GUI.color = Color.white;
    }
    private void DisplayCopyButtons()
    {
        Texture2D icon = Resources.Load<Texture2D>("Textures/Icon/CopyIcon");
        GUIContent btnContent = new GUIContent(icon);
        GUI.enabled = CustomLayerEditor.ED.SelectedLayerIds.Count > 0;

        if (GUILayout.Button(btnContent, GUILayout.Width(40), GUILayout.Height(40)))
            CopySelectedLayer(Vector3.zero);

        if (GUILayout.Button("↑", GUILayout.Width(40), GUILayout.Height(40)))
            CopySelectedLayer(Vector3.up);
        
        if (GUILayout.Button("↓", GUILayout.Width(40), GUILayout.Height(40)))
            CopySelectedLayer(Vector3.down);      
        if (GUILayout.Button("←", GUILayout.Width(40), GUILayout.Height(40)))
            CopySelectedLayer(Vector3.left);
        
        if (GUILayout.Button("→", GUILayout.Width(40), GUILayout.Height(40)))
            CopySelectedLayer(Vector3.right);

        GUI.enabled = true;
    }

    private void CopySelectedLayer(Vector3 direction)
    {
        List<int> selectedIdList = new List<int>(CustomLayerEditor.ED.SelectedLayerIds);

        foreach (int id in selectedIdList)
            CustomLayerEditor.CreateCloneLayer(id, direction);

        foreach (int id in selectedIdList)
            CustomLayerEditor.ED.SelectedLayerIds.Remove(id);

        GUIUtility.keyboardControl = 0;
    }
    private void DeleteSelectedLayer()
    {
        Texture2D icon = Resources.Load<Texture2D>("Textures/Icon/DeleteIcon"); 
        GUIContent btnContent = new GUIContent(icon); 

        GUI.enabled = CustomLayerEditor.ED.SelectedLayerIds.Count > 0;

        if (GUILayout.Button(btnContent, GUILayout.Width(40), GUILayout.Height(40)))
        {
            foreach(int id in CustomLayerEditor.ED.SelectedLayerIds)
            {
                Transform layer = CustomLayerEditor.LayerObjects[id];
                CustomLayerEditor.ToDeleteLayerIds.Add(id);
                CustomLayerEditor.EmptyLayerIds.Add(id);

                Undo.DestroyObjectImmediate(layer.gameObject);
            }

            CustomLayerEditor.SearchTopLayerId();

            GUIUtility.keyboardControl = 0;
            _isDragging = false;

            Utils.UndoStack.Push(() => 
                {
                    var layers = CustomLayerEditor.GetDictinaryLayers();
                    var createdLayers = layers.Keys.Except(CustomLayerEditor.LayerObjects.Keys).ToList();
                    if (createdLayers.Count > 0)
                    {
                        foreach (int id in createdLayers)
                        {
                            if (CustomLayerEditor.ToRestoreLayerIds.ContainsKey(id) == false)
                                CustomLayerEditor.ToRestoreLayerIds.Add(id, layers[id]);

                            CustomLayerEditor.EmptyLayerIds.Remove(id);
                        }
                    }
                });

            Repaint();
        }

        GUI.enabled = true;
    }

    private void MoveSelectedLayer()
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

        Utils.UndoStack.Push(() =>
        {
            var layers = CustomLayerEditor.GetDictinaryLayers();

            foreach (var layer in layers)
            {
                if (_prevLayerIndexs.ContainsKey(layer.Key))
                {
                    layer.Value.transform.SetSiblingIndex(_prevLayerIndexs[layer.Key]);
                }
            }

            _prevLayerIndexs.Clear();
        });
    }

    private void ChangeLayerName(LayerData layerData)
    {
        string layerName = layerData.Name;

        if (IsEditingLayerName(layerData))
        {
            GUILayout.Space(1);
            layerName = Utils.EditPropertyWithUndo(
                "",
                layerName,
                newName => layerData.Name = newName,
                (label, value) => EditorGUILayout.TextField(value, GUILayout.Width(200)),
                layerData
            );
        }
        else
        {
            GUILayout.Space(3);
            EditorGUILayout.LabelField(layerName, GUILayout.Width(200));
        }
    }
    private bool IsEditingLayerName(LayerData layerData)
    {
        bool isEditing = CustomLayerEditor.ED.SelectedLayerIds.Contains(layerData.Id);
        return isEditing;
    }
}
