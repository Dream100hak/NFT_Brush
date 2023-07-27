using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;

public class LayerWindow : EditorWindow
{
    private bool _isDragging;
    private bool _clickedInsideLayer;

    private KeyValuePair<int, GameLayer> _currentDraggingLayer = new KeyValuePair<int, GameLayer>(); // 현재 드래깅 중인 레이어
    private KeyValuePair<int, GameLayer> _registeredDraggedLayer = new KeyValuePair<int, GameLayer>(); // 위치를 맞바꿀 레이어

    private bool _multiSelected = false; //TODO : 구현 예정
    private DateTime _dragStartTime;

    [MenuItem("Photoshop/Layer")]
    public static void ShowWindow() => GetWindow<LayerWindow>("Layer");

    private void OnEnable()
    {
        LayerInfo.OnClear += LayerInfo.ClearHandler;
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        Undo.undoRedoPerformed += OnUndoRedoPerformed;
        LayerInfo.ED.SelectedLayerIds.Clear();
    }
    private void OnDisable()
    {
        LayerInfo.OnClear -= LayerInfo.ClearHandler;
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
    }

    private void OnUndoRedoPerformed()
    {
        if (focusedWindow != this)
            return;

        Debug.Log("LayerWIndow : " + focusedWindow);

        Utils.UndoExecute();
        LayerInfo.SetLayerHasChanged();
        Repaint();
    }

    public void OnGUI()
    {
        DrawLayerListGUI();

        Rect seperateRect = GUILayoutUtility.GetLastRect();
        seperateRect.y += seperateRect.height;
        seperateRect.height = 3;

        EditorGUI.DrawRect(seperateRect, new Color(0.3f, 0.3f, 0.3f));

        GUILayout.Space(10);
        GUILayout.BeginHorizontal(GUI.skin.box);
        GUILayout.FlexibleSpace();
        DrawCreateNewLayerGUI();
        DrawCopyButtonListGUI();
        DeleteSelectedLayer();
  
        GUILayout.EndHorizontal();

    }
    //레이어 리스트 드로잉
    Vector2 _scrollPos = Vector2.zero;
    private void DrawLayerListGUI()
    {
        _clickedInsideLayer = false;
        _scrollPos = GUILayout.BeginScrollView(_scrollPos, false, true);
        var sortedLayerList = LayerInfo.ED.GetGameLayers(x => x.Value, x => x.CreationTimestamp , true);

        foreach (var layer in sortedLayerList)
        {
            if (layer == null)
                continue;

            Color originBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.clear;
            DrawSelectedLayerGUI(layer.Id);
            DrawLayerInfoGUI(layer);
            DraggedInput(layer.Id, layer);
            GUI.backgroundColor = originBackgroundColor;

            EditorHelper.DrawSeparatorHeightLine(-3, 1.7f, new Color(0.1f, 0.1f, 0.1f, 0.5f));
            GUILayout.Space(-5);
        }

        if(_clickedInsideLayer == false)
         ClearSelectedLayers();

        RegisterDraggedLayer();

        LayerInfo.DeleteLayerIds(LayerInfo.ED.LayerObjects);
        LayerInfo.RestoreLayerIds(LayerInfo.ED.LayerObjects);
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
    private void DrawLayerInfoGUI(GameLayer layer)
    {
        GUILayout.BeginHorizontal(EditorHelper.WhiteSkinBoxStyle());
        Texture2D layerSnapshot = Snapshot.CaptureLayerSnapshot(DrawingInfo.GameCanvas, layer); // 스냅샷 관련
        Rect imageRect = GUILayoutUtility.GetRect(50, 50);
        GUI.DrawTexture(imageRect, layerSnapshot, ScaleMode.ScaleToFit);
        DrawChangeLayerNameGUI(layer); // 이름 변경 관련
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }
    //드래그 라인 드로잉 
    private void DrawDraggedSkyLineGUI(GameLayer layer)
    {
        Rect newLayerRect = GUILayoutUtility.GetLastRect();
        newLayerRect.width = EditorGUIUtility.currentViewWidth;

        if (newLayerRect.x != 0 && newLayerRect.y != 0)
            layer.LayerRect = newLayerRect;

        if (_isDragging && IsContainLayerRect(layer.LayerRect) && _currentDraggingLayer.Value != null)
            EditorGUI.DrawRect(new Rect(layer.LayerRect.x, layer.LayerRect.y + layer.LayerRect.height, layer.LayerRect.width, 1), EditorHelper.Sky);
    }

    private void DraggedInput(int id, GameLayer layer)
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
                DraggedMouseUp();
                break;
        }
        DrawDraggedSkyLineGUI(layer);
    }

    private void DraggedMouseDown(int id, GameLayer layer, Rect newLayerRect)
    {
        if (IsContainLayerRect(newLayerRect) && Event.current.button == 0)
        {
            _currentDraggingLayer = new KeyValuePair<int, GameLayer>(id, layer);
            _dragStartTime = DateTime.Now;
            _clickedInsideLayer = true;

            BrushInfo.ClearAllSelectedBrush();

            if (Event.current.shift)
                InpuLayerIdByShift(id);
      
            else if (Event.current.control)
                InpuLayerIdByControl(id);        
            else
                InpuLayerIdByLeftClick(id);
         
            LayerInfo.SelectLayerObjects();
            Event.current.Use();
        }
    }
    private void DraggedMouseDrag(GameLayer layer, Rect newLayerRect)
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
    private void DraggedMouseUp()
    {
        _isDragging = false;

        if(_registeredDraggedLayer.Value != null)
        {
            Utils.UndoPop();
            ApplyDraggedLayer();
        }
        Event.current.Use();
    }
    private void InpuLayerIdByControl(int id)
    {
        Undo.RecordObject(LayerInfo.ED, "[Key Code Ctrl] Layer Selection");

        if (LayerInfo.ED.SelectedLayerIds.Contains(id))
        {
            LayerInfo.ED.SelectedLayerIds.Remove(id);
            Utils.AddUndo("[Key Code Ctrl] Remove Selection Layer", () => { if (LayerInfo.ED.SelectedLayerIds.Contains(id) == false) LayerInfo.ED.SelectedLayerIds.Add(id); });
        }
        else
        {
            LayerInfo.ED.SelectedLayerIds.Add(id);
            Utils.AddUndo("[Key Code Ctrl] Add Selection Layer", () => { if (LayerInfo.ED.SelectedLayerIds.Contains(id)) LayerInfo.ED.SelectedLayerIds.Remove(id); });
        }
        _multiSelected = true;
    }
    private void InpuLayerIdByShift(int id)
    {
        List<int> prevSelectedLayerIds = new List<int>(LayerInfo.ED.SelectedLayerIds);

        Undo.RecordObject(LayerInfo.ED, "[Key Code Shift] Add Selection Layer");

        if (LayerInfo.ED.SelectedLayerIds.Any())
        {
            List<GameLayer> layerDatas = LayerInfo.ED.GetGameLayers(x => x.Value , x => x.CreationTimestamp );
            int firstSelectedIndex = layerDatas.FindIndex(layerData => LayerInfo.ED.SelectedLayerIds.Contains(layerData.Id));
            int currentLayerIndex = layerDatas.FindIndex(layerData => layerData.Id == id);
            int startIndex = Math.Min(firstSelectedIndex, currentLayerIndex);
            int endIndex = Math.Max(firstSelectedIndex, currentLayerIndex);

            LayerInfo.ED.SelectedLayerIds.Clear();

            for (int i = startIndex; i <= endIndex; i++)
                LayerInfo.ED.SelectedLayerIds.Add(layerDatas[i].Id);

            Utils.AddUndo("[Key Code Shift] Add Selection Layer", () =>
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

        _multiSelected = true;
    }
    public void InpuLayerIdByLeftClick(int id)
    {
        List<int> prevSelectedLayerIds = new List<int>(LayerInfo.ED.SelectedLayerIds);

        Undo.RecordObject(LayerInfo.ED, "[Left Click] Add Selection Layer");
        LayerInfo.ED.SelectedLayerIds.Clear();
        LayerInfo.ED.SelectedLayerIds.Add(id);

        Utils.AddUndo("[Left Click] Add Selection Layer", () =>
        {
            if (LayerInfo.ED.SelectedLayerIds.Contains(id))
                LayerInfo.ED.SelectedLayerIds.Remove(id);

            foreach (var prevId in prevSelectedLayerIds)
                LayerInfo.ED.SelectedLayerIds.Add(prevId);
        });
    }

    private void RegisterDraggedLayer()
    {
        List<GameLayer> layerDatas = LayerInfo.GetHierarchyLayers( x => x, null);

        if (_isDragging && _currentDraggingLayer.Value != null)
        {
            Color originBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = EditorHelper.Gray01;
            DrawDraggedLayerAtMouseGUI(_currentDraggingLayer.Value, Event.current.mousePosition);
            GUI.backgroundColor = originBackgroundColor;

            foreach (var layerData in layerDatas)
            {
                if (_currentDraggingLayer.Value == layerData)
                    continue;

                if (IsContainLayerRect(layerData.LayerRect))
                {
                    _registeredDraggedLayer = new KeyValuePair<int, GameLayer>(layerData.Id, layerData);
                    break;
                }
            }
        }
    }
    private void ApplyDraggedLayer()
    {
        var prevLayerIndexs = new Dictionary<int, int>();

        GameLayer A = _currentDraggingLayer.Value;
        GameLayer B = _registeredDraggedLayer.Value;

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

        _registeredDraggedLayer = new KeyValuePair<int, GameLayer>();
        _currentDraggingLayer = new KeyValuePair<int, GameLayer>();

        Utils.AddUndo("Apply draggedLayer", () =>
        {
            var layers = LayerInfo.GetHierarchyLayers(x => x , null);

            foreach (var layer in layers)
            {
                if (prevLayerIndexs.ContainsKey(layer.Id))
                {
                    layer.transform.SetSiblingIndex(prevLayerIndexs[layer.Id]);
                }
            }
        });
    }

    private void DrawDraggedLayerAtMouseGUI(GameLayer layer, Vector2 mousePos)
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
        if (Event.current.type == EventType.MouseDown && LayerInfo.ED.LayerObjects.Count > 0)
        {
            List<int> prevSelectedLayerIds = new List<int>(LayerInfo.ED.SelectedLayerIds);

            Selection.activeGameObject = null;
            LayerInfo.ED.SelectedLayerIds.Clear();

            Utils.AddUndo("Clear selected layers", () =>
            {
                foreach (int prevId in prevSelectedLayerIds)
                {
                    if (!LayerInfo.ED.SelectedLayerIds.Contains(prevId))
                    {
                        LayerInfo.ED.SelectedLayerIds.Add(prevId);
                        LayerInfo.SelectLayerObjects();
                    }
                }
            });
        }
    }
    private void DrawCopyButtonListGUI()
    {
        GUI.enabled = LayerInfo.ED.SelectedLayerIds.Count > 0;

        if (GUILayout.Button(EditorHelper.GetTrIcon("d_Collab.FileUpdated", "레이어 복사"), GUILayout.Width(40), GUILayout.Height(40)))
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
    
    //TODO : 삭제 예정
    void TempAllDelete()
    {
        Undo.ClearAll();
        Utils.ClearUndoRedo();
        LayerInfo.Clear();
        BrushInfo.Clear();

        GameLayer[] layersInScene = UnityEngine.Object.FindObjectsOfType<GameLayer>();
        foreach (GameLayer layerData in layersInScene)
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
                GameLayer layer = LayerInfo.ED.LayerObjects[id];
                LayerInfo.ToDeleteIds.Add(id);
                LayerInfo.EmptyGenerateIds.Add(id);
                Undo.DestroyObjectImmediate(layer.gameObject);
            }

            LayerInfo.SearchTopLayerId();
            _isDragging = false;

            Utils.AddUndo("Delete selected layer" ,() =>
            {
                LayerInfo.ED.SelectedLayerIds.Clear();
                var layers = LayerInfo.GetHierarchyLayers(x => x, null);
                var createdLayers = layers.Except(LayerInfo.ED.LayerObjects.Values).ToList();
                if (createdLayers.Count > 0)
                {
                    foreach (GameLayer layer in createdLayers)
                    {
                        if (LayerInfo.ToRestoreIds.ContainsKey(layer.Id) == false)
                        {
                            LayerInfo.ToRestoreIds.Add(layer.Id, layer);
                            LayerInfo.ED.SelectedLayerIds.Add(layer.Id);
                        }
                          
                        LayerInfo.EmptyGenerateIds.Remove(layer.Id);
                    }
                }
            });

            Repaint();
        }

        GUI.enabled = true;
    }
    private void DrawChangeLayerNameGUI(GameLayer layerData)
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
    private void DrawCreateNewLayerGUI()
    {
        GUI.enabled = DrawingInfo.CurrentCanvas != null;

        if (GUILayout.Button(EditorHelper.GetTrIcon("Collab.FileAdded" , "레이어 생성"), GUILayout.Width(40), GUILayout.Height(40)))
        {
            LayerInfo.ED.SelectedLayerIds.Clear();
            LayerInfo.CreateNewLayer();
            GUIUtility.keyboardControl = 0;
            _currentDraggingLayer = new KeyValuePair<int, GameLayer>();
        }

        GUI.enabled = true;
    }

    private bool IsEditingLayerName(GameLayer layerData) => LayerInfo.ED.SelectedLayerIds.Contains(layerData.Id);
    private bool IsContainLayerRect(Rect layerRect) => layerRect.Contains(Event.current.mousePosition);
}
