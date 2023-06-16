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
    private bool _isDragging;
    private bool _clickedInsideLayer;

    private KeyValuePair<int, Layer> _draggingLayer = new KeyValuePair<int, Layer>(); // 현재 드래깅 중인 레이어
    private KeyValuePair<int, Layer> _insertLayer = new KeyValuePair<int, Layer>();

    private bool _multiSelected = false;
    private DateTime _dragStartTime;

    [MenuItem("Photoshop/Layer")]
    public static void ShowWindow()
    {
        GetWindow<LayerWindow>("Layer");
    }

    private void OnEnable()
    {
        Undo.undoRedoPerformed += OnUndoRedoPerformed;
        LayerInfo.ED.SelectedLayerIds.Clear();
      //  Debug.Log("Subscribed to Undo.undoRedoPerformed");

    }
    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
       // Debug.Log("Unsubscribed from Undo.undoRedoPerformed");
    }

    private void OnUndoRedoPerformed()
    {
    //    Debug.Log("OnUndoRedoPerformed called");

        Utils.UndoExecute();
        LayerInfo.SetLayerHasChanged();
        Repaint();
    }

    public Texture2D CaptureLayerSnapshot(Layer layer)
    {
        if (layer.HasChanged == false && layer.SnapShot != null)
            return layer.SnapShot;

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
        gridQuad.GetComponent<Renderer>().sharedMaterial.mainTexture = Resources.Load<Texture2D>("Textures/Grid");
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
            DrawLayerListGUI();

        Rect seperateRect = GUILayoutUtility.GetLastRect();
        seperateRect.y += seperateRect.height;
        seperateRect.height = 3;

        EditorGUI.DrawRect(seperateRect, new Color(0.3f, 0.3f, 0.3f));

        GUILayout.Space(10);
        GUILayout.BeginHorizontal(GUI.skin.box);
        GUILayout.FlexibleSpace();
        CreateNewLayer();
        DrawCopyButtonListGUI(); // 레이어 복제 버튼 추가
        DeleteSelectedLayer();
  
        GUILayout.EndHorizontal();

    }
    //레이어 리스트 드로잉
    private void DrawLayerListGUI()
    {
        _clickedInsideLayer = false;
        Vector2 scrollPos = Vector2.zero;
        GUILayout.BeginScrollView(scrollPos, false, true);

        var sortedLayerList = LayerInfo.GetSortedCreationTimeLayerList();

        foreach (var layerPair in sortedLayerList)
        {
            int i = layerPair.Key;
            Transform layer = layerPair.Value;

            if (layer == null)
                continue;

            Layer layerData = layer.GetComponent<Layer>();

            Color originBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.clear;
            DrawSelectedLayerGUI(i);
            DrawLayerInfoGUI(layerData);
            DraggedInput(i,layerData);
            GUI.backgroundColor = originBackgroundColor;

            EditorHelper.DrawSeparatorLine(-3, 1.7f, new Color(0.1f, 0.1f, 0.1f, 0.5f));
            GUILayout.Space(-5);
        }

        if(_clickedInsideLayer == false)
         ClearSelectedLayers();

        InsertDraggedLayer();

        LayerInfo.DeleteLayerIds();
        LayerInfo.RestoreLayerIds();
        GUILayout.EndScrollView();
        Repaint();
    }
    //선택된 레이어 드로잉 
    private void DrawSelectedLayerGUI(int id)
    {
        foreach (var selectedId in LayerInfo.ED.SelectedLayerIds)
        {
            if (selectedId == id)
            {
                GUI.backgroundColor = EditorHelper.Gray01;
                break;
            }
        }
    }
    //레이어 정보 박스 드로잉 
    private void DrawLayerInfoGUI(Layer layer)
    {
        GUILayout.BeginHorizontal(EditorHelper.WhiteSkinBoxStyle());
        Texture2D layerSnapshot = CaptureLayerSnapshot(layer); // 스냅샷 관련
        Rect imageRect = GUILayoutUtility.GetRect(50, 50);
        GUI.DrawTexture(imageRect, layerSnapshot, ScaleMode.ScaleToFit);
        ChangeLayerName(layer); // 이름 변경 관련
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }
    //드래그 라인 드로잉 
    private void DrawDraggedSkyLineGUI(Layer layer)
    {
        Rect newLayerRect = GUILayoutUtility.GetLastRect();
        newLayerRect.width = EditorGUIUtility.currentViewWidth;

        if (newLayerRect.x != 0 && newLayerRect.y != 0)
            layer.LayerRect = newLayerRect;

        if (_isDragging && IsContainLayerRect(layer.LayerRect) && _draggingLayer.Value != null)
            EditorGUI.DrawRect(new Rect(layer.LayerRect.x, layer.LayerRect.y + layer.LayerRect.height, layer.LayerRect.width, 1), EditorHelper.Sky);
    }

    private void DraggedInput(int id, Layer layer)
    {
        Rect newLayerRect = GUILayoutUtility.GetLastRect();

        switch (Event.current.type)
        { 
           case EventType.MouseDown:
                DraggedMouseDown(id, layer, newLayerRect);
                break;
            case EventType.MouseDrag:
                DraggedMouseDrag(layer, newLayerRect);
                break;
            case EventType.MouseUp:
                DraggedMouseUp(id);
                break;
        }

        DrawDraggedSkyLineGUI(layer);
    }

    private void DraggedMouseDown(int id, Layer layer, Rect newLayerRect)
    {
        if (IsContainLayerRect(newLayerRect) && Event.current.button == 0)
        {
            _draggingLayer = new KeyValuePair<int, Layer>(id, layer);
            _dragStartTime = DateTime.Now;
            _clickedInsideLayer = true;

            if (Event.current.shift)
                InsertShiftLayerId(id);
            else
                InsertControlLayerId(id);

            LayerInfo.SelectLayerObjects();
            Event.current.Use();
        }
    }

    private void DraggedMouseDrag(Layer layer, Rect newLayerRect)
    {
        if (IsContainLayerRect(newLayerRect) && Event.current.button == 0)
        {
            if (LayerInfo.ED.SelectedLayerIds.Count > 0 && IsContainLayerRect(layer.LayerRect))
            {
                TimeSpan dragDuration = DateTime.Now - _dragStartTime;

                if (dragDuration.TotalSeconds >= 0.15f)
                    _isDragging = true;
            }

            Event.current.Use();
        }
    }
    private void DraggedMouseUp(int id)
    {
        _isDragging = false;

        if(_insertLayer.Value != null)
        {
            ApplyDraggedLayer();
        }
        else
        {
            if(LayerInfo.ED.SelectedLayerIds.Count > 0)
            {
                Utils.AddUndo("Add layerId by click", () =>
                {
                    if (LayerInfo.ED.SelectedLayerIds.Contains(id))
                        LayerInfo.ED.SelectedLayerIds.Remove(id);
                });
            }
            else
            {
                List<int> prevSelectedLayerIds = new List<int>(LayerInfo.ED.SelectedLayerIds);

                Utils.AddUndo("Clear selected layers", () =>
                {
                    foreach (int prevId in prevSelectedLayerIds)
                    {
                        if (!LayerInfo.ED.SelectedLayerIds.Contains(prevId))
                            LayerInfo.ED.SelectedLayerIds.Add(prevId);
                    }
                });
            }
        }
         
        Event.current.Use();
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
            Undo.RecordObject(LayerInfo.ED,  "No Control Layer Selection");

            LayerInfo.ED.SelectedLayerIds.Clear();
            LayerInfo.ED.SelectedLayerIds.Add(id);   
        }
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

            Utils.AddUndo("Add layerId by shift key", () =>
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
        } 
    }
    private void InsertDraggedLayer()
    {
        List<Layer> layerDatas = LayerInfo.GetLayerList();

        if (_isDragging && _draggingLayer.Value != null)
        {
            Color originBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = EditorHelper.Gray01;
            DrawDraggedLayerAtMouseGUI(_draggingLayer.Value, Event.current.mousePosition);
            GUI.backgroundColor = originBackgroundColor;

            foreach (var layerData in layerDatas)
            {
                if (_draggingLayer.Value == layerData)
                    continue;

                if (IsContainLayerRect(layerData.LayerRect))
                {
                    _insertLayer = new KeyValuePair<int, Layer>(layerData.Id, layerData);
                    break;
                }
            }
        }
    }
    private void ApplyDraggedLayer()
    {
        var prevLayerIndexs = new Dictionary<int, int>();

        Layer A = _draggingLayer.Value;
        Layer B = _insertLayer.Value;

        UnityEngine.Object[] recordObjs = new UnityEngine.Object[] { A, B };
        Undo.RecordObjects(recordObjs, "Dragged Layer");

        int bIndex = B.transform.GetSiblingIndex();
        long bTime = B.CreationTimestamp;
  
        int AIndex = A.transform.GetSiblingIndex();

        // Swap
        B.CreationTimestamp = A.CreationTimestamp;
        A.CreationTimestamp = bTime;

        B.transform.SetSiblingIndex(AIndex);
        A.transform.SetSiblingIndex(bIndex);

        prevLayerIndexs.Add(A.Id, AIndex);
        prevLayerIndexs.Add(B.Id, bIndex);

        GUIUtility.keyboardControl = 0;

        _insertLayer = new KeyValuePair<int, Layer>();
        _draggingLayer = new KeyValuePair<int, Layer>();

        Utils.AddUndo("Apply draggedLayer" , () =>
        {
            var layers = LayerInfo.GetDictinaryLayers();

            foreach (var layer in layers)
            {
                if (prevLayerIndexs.ContainsKey(layer.Key))
                {
                    layer.Value.transform.SetSiblingIndex(prevLayerIndexs[layer.Key]);
                }
            }
        });
    }

    private void DrawDraggedLayerAtMouseGUI(Layer layer, Vector2 mousePos)
    {
        GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, 0.5f);
        float width = EditorGUIUtility.currentViewWidth / 1.5f;
        float height = 200;
        GUILayout.BeginArea(new Rect(mousePos.x - width / 2, mousePos.y - height / 8, width, height));
        DrawLayerInfoGUI(layer);
        GUILayout.EndArea();
        GUI.color = Color.white;
    }
    private void ClearSelectedLayers()
    {
        if (Event.current.type == EventType.MouseDown)
        {
            LayerInfo.ED.SelectedLayerIds.Clear();
        }
    }
    private void DrawCopyButtonListGUI()
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

        if (GUILayout.Button("D", GUILayout.Width(40), GUILayout.Height(40)))
            TempAllDelete();
    }
    
    void TempAllDelete()
    {
        Undo.ClearAll();
        Utils.ClearUndoRedo();

        LayerInfo.Clear();

        Layer[] layersInScene = UnityEngine.Object.FindObjectsOfType<Layer>();
        foreach (Layer layerData in layersInScene)
        {
            DestroyImmediate(layerData.gameObject);
        }
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
            foreach (int id in LayerInfo.ED.SelectedLayerIds)
            {
                Transform layer = LayerInfo.LayerObjects[id];
                LayerInfo.ToDeleteLayerIds.Add(id);
                LayerInfo.EmptyLayerIds.Add(id);

                Undo.DestroyObjectImmediate(layer.gameObject);
            }

            LayerInfo.SearchTopLayerId();

            GUIUtility.keyboardControl = 0;
            _isDragging = false;

            Utils.AddUndo("Delete selected layer" ,() =>
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
            _draggingLayer = new KeyValuePair<int, Layer>();
        }
    }

    private bool IsEditingLayerName(Layer layerData) => LayerInfo.ED.SelectedLayerIds.Contains(layerData.Id);
    private bool IsContainLayerRect(Rect layerRect) => layerRect.Contains(Event.current.mousePosition);
}
