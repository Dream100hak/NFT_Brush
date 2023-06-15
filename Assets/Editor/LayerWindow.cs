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
    private KeyValuePair<int, Layer> _draggingLayer = new KeyValuePair<int, Layer>();
    private KeyValuePair<int, Layer> _insertLayer = new KeyValuePair<int, Layer>();

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

        Undo.undoRedoPerformed += OnUndoRedoPerformed;
        LayerInfo.ED.SelectedLayerIds.Clear();
        Debug.Log("Subscribed to Undo.undoRedoPerformed");

    }
    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        Debug.Log("Unsubscribed from Undo.undoRedoPerformed");
    }

    private void OnUndoRedoPerformed()
    {
        Debug.Log("OnUndoRedoPerformed called");

        Utils.UndoExecute();
        LayerInfo.SetLayerChanged();

        Repaint();
    }

    public Texture2D CaptureLayerSnapshot(Layer layer)
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

        Transform cubeParent = BrushInfo.GetBrushParent();
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
        if (FindObjectOfType<BrushPlacer>() == null)
        {
            CreateCanvas();
            return;
        }

        if (BrushInfo.GetBrushParent() != null)
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
            canvas.AddComponent<BrushPlacer>();
            canvas.GetComponent<BrushPlacer>().BrushPrefab = Resources.Load<GameObject>("Prefab/Cube");
            collider.AddComponent<BoxCollider>();
            collider.GetComponent<BoxCollider>().isTrigger = true;
            collider.GetComponent<BoxCollider>().size = new Vector3(100, 100, 0.2f);

            SpriteRenderer circle = new GameObject("Circle").AddComponent<SpriteRenderer>();
        }
    }
    private void CreateNewLayer()
    {
        if (GUILayout.Button(Utils.GetIconContent("Collab.FileAdded"), GUILayout.Width(40), GUILayout.Height(40))) 
        {
            LayerInfo.ED.SelectedLayerIds.Clear();
            LayerInfo.CreateNewLayer();
            GUIUtility.keyboardControl = 0;
        }
    }

    private void MakeLayerList()
    {
        bool clickedInsideLayer = false;
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true);

        _layerIndexList.Clear();
        foreach (var layerPair in LayerInfo.LayerObjects)
            _layerIndexList.Add(layerPair.Key);

        var sortedLayerObjects = _layerIndexList
              .Select(x => new KeyValuePair<int, Transform>(x, LayerInfo.LayerObjects[x]))
              .Where(x => x.Value != null)
              .OrderByDescending(x => x.Value.GetComponent<Layer>().CreationTimestamp)
              .ToList();

        List<int> tempLayerIndexList = new List<int>(_layerIndexList);
        List<Layer> layerDatas = LayerInfo.GetLayerDatas();

        foreach (var layerPair in sortedLayerObjects)
        {
            int i = layerPair.Key;
            Transform layer = layerPair.Value;

            if (layer == null)
                continue;

            Layer layerData = layer.GetComponent<Layer>();
            Color originBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.clear;

            foreach (var id in LayerInfo.ED.SelectedLayerIds)
            {
                if(id == i)
                {
                    GUI.backgroundColor = new Color32(100,100,100,255);
                    break;
                }     
            }

            GUILayout.BeginHorizontal(EditorHelper.WhiteSkinBoxStyle());

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
                    _draggingLayer = new KeyValuePair<int, Layer>(i, layerData);
                    _dragStartTime = DateTime.Now;
                    clickedInsideLayer = true;

                    if (Event.current.shift)
                        InsertShiftLayerId(i);            
                    else
                        InsertControlLayerId(i);

                    LayerInfo.SelectLayerObjects();
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
            EditorHelper.DrawSeparatorLine(-3, 1.7f, new Color(0.1f, 0.1f, 0.1f, 0.5f));

            GUILayout.Space(-5);
        }


        //레이어가 클릭되지 않은 경우 
        if (Event.current.type == EventType.MouseDown && !clickedInsideLayer)
        {
            List<int> prevSelectedLayerIds = new List<int>(LayerInfo.ED.SelectedLayerIds);

            LayerInfo.ED.SelectedLayerIds.Clear();
            Selection.activeObject = null;

            Utils.UndoStack.Push(() => 
                {
                    foreach (int prevId in prevSelectedLayerIds)
                    {
                        if (!LayerInfo.ED.SelectedLayerIds.Contains(prevId))
                            LayerInfo.ED.SelectedLayerIds.Add(prevId);
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
                    _insertLayer = new KeyValuePair<int, Layer>(layerData.Id, layerData);
                    break;
                }
            }
        }

        if (Event.current.type == EventType.MouseUp && _insertLayer.Value != null)
            MoveSelectedLayer();
  
        LayerInfo.DeleteLayerIds();
        LayerInfo.RestoreLayerIds();
        GUILayout.EndScrollView();
        Repaint();
    }
    
    private void InsertControlLayerId(int id)
    {
        if(Event.current.control || Event.current.command)
        {
            Undo.RecordObject(LayerInfo.ED, "Control Layer Selection");

            if (LayerInfo.ED.SelectedLayerIds.Contains(id))
                LayerInfo.ED.SelectedLayerIds.Remove(id);
            else
                LayerInfo.ED.SelectedLayerIds.Add(id);

            _multiSelected = true;
        }
        else
        {
            Undo.RecordObject(LayerInfo.ED,  "Layer Selection");

            LayerInfo.ED.SelectedLayerIds.Clear();
            LayerInfo.ED.SelectedLayerIds.Add(id);   
        }

        Utils.UndoStack.Push(() => 
        {
            if(LayerInfo.ED.SelectedLayerIds.Contains(id))
                LayerInfo.ED.SelectedLayerIds.Remove(id);
        });
    }
    private void InsertShiftLayerId(int id)
    {
        List<int> prevSelectedLayerIds = new List<int>(LayerInfo.ED.SelectedLayerIds);

        Undo.RecordObject(LayerInfo.ED, "Shift Layer Selection");

        if (LayerInfo.ED.SelectedLayerIds.Any())
        {
            List<Layer> layerDatas = LayerInfo.GetLayerOrders();
            int firstSelectedIndex = layerDatas.FindIndex(layerData => LayerInfo.ED.SelectedLayerIds.Contains(layerData.Id));
            int currentLayerIndex = layerDatas.FindIndex(layerData => layerData.Id == id);
            int startIndex = Math.Min(firstSelectedIndex, currentLayerIndex);
            int endIndex = Math.Max(firstSelectedIndex, currentLayerIndex);

            LayerInfo.ED.SelectedLayerIds.Clear();

            for (int i = startIndex; i <= endIndex; i++)
                LayerInfo.ED.SelectedLayerIds.Add(layerDatas[i].Id);

            Utils.UndoStack.Push(() =>
            {
                for (int i = startIndex; i <= endIndex; i++)
                {
                    if (LayerInfo.ED.SelectedLayerIds.Contains(layerDatas[i].Id))
                    LayerInfo.ED.SelectedLayerIds.Remove(layerDatas[i].Id);
                }

                foreach (int prevId in prevSelectedLayerIds)
                {
                    if (!LayerInfo.ED.SelectedLayerIds.Contains(prevId))
                        LayerInfo.ED.SelectedLayerIds.Add(prevId);
                }
            });
        }
        else
        {
            LayerInfo.ED.SelectedLayerIds.Add(id);

            Utils.UndoStack.Push(() =>
            {
                if (LayerInfo.ED.SelectedLayerIds.Contains(id))
                    LayerInfo.ED.SelectedLayerIds.Remove(id);
            });
        } 
    }

    private void DrawLayerAtMouse(Layer layerData, Vector2 mousePosition)
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
     
        GUI.enabled = LayerInfo.ED.SelectedLayerIds.Count > 0;

        if (GUILayout.Button(Utils.GetIconContent("d_Collab.FileUpdated"), GUILayout.Width(40), GUILayout.Height(40)))
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
        List<int> selectedIdList = new List<int>(LayerInfo.ED.SelectedLayerIds);

        foreach (int id in selectedIdList)
            LayerInfo.CreateCloneLayer(id, direction);

        foreach (int id in selectedIdList)
            LayerInfo.ED.SelectedLayerIds.Remove(id);

        GUIUtility.keyboardControl = 0;
    }
    private void DeleteSelectedLayer()
    {
        Texture2D icon = Resources.Load<Texture2D>("Textures/Icon/DeleteIcon"); 
        GUIContent btnContent = new GUIContent(icon); 

        GUI.enabled = LayerInfo.ED.SelectedLayerIds.Count > 0;

        if (GUILayout.Button(btnContent, GUILayout.Width(40), GUILayout.Height(40)))
        {
            foreach(int id in LayerInfo.ED.SelectedLayerIds)
            {
                Transform layer = LayerInfo.LayerObjects[id];
                LayerInfo.ToDeleteLayerIds.Add(id);
                LayerInfo.EmptyLayerIds.Add(id);

                Undo.DestroyObjectImmediate(layer.gameObject);
            }

            LayerInfo.SearchTopLayerId();

            GUIUtility.keyboardControl = 0;
            _isDragging = false;

            Utils.UndoStack.Push(() => 
                {
                    var layers = LayerInfo.GetDictinaryLayers();
                    var createdLayers = layers.Keys.Except(LayerInfo.LayerObjects.Keys).ToList();
                    if (createdLayers.Count > 0)
                    {
                        foreach (int id in createdLayers)
                        {
                            if (LayerInfo.ToRestoreLayerIds.ContainsKey(id) == false)
                                LayerInfo.ToRestoreLayerIds.Add(id, layers[id]);

                            LayerInfo.EmptyLayerIds.Remove(id);
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

        Layer draggedLayerData = _draggingLayer.Value;
        Layer insertLayerData = _insertLayer.Value;

        UnityEngine.Object[] recordObjs = new UnityEngine.Object[] { draggedLayerData, insertLayerData };
        Undo.RecordObjects(recordObjs, "Dragged Layer");

        _insertLayer = new KeyValuePair<int, Layer>();
        _draggingLayer = new KeyValuePair<int, Layer>();

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
            var layers = LayerInfo.GetDictinaryLayers();

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

    private void ChangeLayerName(Layer layerData)
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
    private bool IsEditingLayerName(Layer layerData)
    {
        bool isEditing = LayerInfo.ED.SelectedLayerIds.Contains(layerData.Id);
        return isEditing;
    }
}
