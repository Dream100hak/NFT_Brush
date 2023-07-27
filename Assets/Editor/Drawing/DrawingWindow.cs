using NUnit;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class DrawingWindow : EditorWindow
{
    private E_DrawingMode _drawingMode;
    public E_DrawingMode DrawingMode { get => _drawingMode; set { if (_drawingMode != value) _drawingMode = value; } }
    private E_EditMode _editMode;
    public E_EditMode EditMode { get => _editMode; set { if (_editMode != value) _editMode = value;  } }
    
    private GUIContent[] _editGUIContents;

    private static float s_areaX = 0;
    private static float s_areaY = 80;

    private Camera _captureCam;
    private RenderTexture _renderTex;

    private Vector3 _initialMousePos;
    private Vector3 _lastPlacedPos;

    [MenuItem("Photoshop/Drawing")]
    public static void ShowWindow() => GetWindow<DrawingWindow>("Drawing");

    private void OnEnable()
    {
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        Undo.undoRedoPerformed += OnUndoRedoPerformed;
        DrawingInfo.OnClear += DrawingInfo.ClearHandler;
        _editGUIContents = new GUIContent[] { EditorHelper.GetTrIcon("ViewToolMove", "옮기기"), EditorHelper.GetTrIcon("Grid.BoxTool", "선택"), EditorHelper.GetTrIcon("Grid.PaintTool", "그리기"), EditorHelper.GetTrIcon("Grid.EraserTool", "지우기") };
        InitializeCaptureCamera();
        GetPreviewCanvasList();
    }
    private void OnDisable()
    {
        DrawingInfo.OnClear -= DrawingInfo.ClearHandler;
    }
    private void OnUndoRedoPerformed()
    {
        if (focusedWindow != this)
            return;

        Utils.UndoExecute();
        Repaint();
    }
    private void InitializeCaptureCamera()
    {
        GameObject cam = GameObject.Find("DrawCamera") ?? new GameObject("DrawCamera");
        if(cam.GetComponent<Camera>() == null)
            cam.AddComponent<Camera>();

        _captureCam = cam.GetComponent<Camera>();

        Utils.CopyMainCameraComponent(ref _captureCam, DrawingInfo.CreateCanvasSize);

        _renderTex = new RenderTexture((int)DrawingInfo.CreateCanvasSize.x, (int)DrawingInfo.CreateCanvasSize.y, 24, RenderTextureFormat.ARGB32);
        _renderTex.Create();

        _captureCam.targetTexture = _renderTex;
    }

    private void OnGUI()
    {
        switch (DrawingMode)
        {
            case E_DrawingMode.Create:
                DrawCreateCanvasGUI();
                DrawPreviewCanvasGUI();
                break;
            case E_DrawingMode.Edit:
                SceneView.lastActiveSceneView.in2DMode = true;
                DrawEditCanvasGUI();
                break;
        }
    }

    private void DrawCreateCanvasGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawingInfo.CreateCanvasName = EditorGUILayout.TextField("Name : ", DrawingInfo.CreateCanvasName);

        GUILayout.Space(5);

        GUI.enabled = false;
        float width = EditorGUILayout.FloatField("Width : ", DrawingInfo.CreateCanvasSize.x);
        GUILayout.Space(5);
        float height = EditorGUILayout.FloatField("Height : ", DrawingInfo.CreateCanvasSize.y);
        GUILayout.Space(5);
        GUI.enabled = true;

        DrawingInfo.CreateCanvasSize = new Vector2 (width, height);

        if (GUILayout.Button("Create New Canvas", EditorHelper.DrawCreateCanvasButton(), GUILayout.Height(100)))
        {
            if(DrawingInfo.IsNameDoubleCheck(DrawingInfo.CreateCanvasName))
            {
                ShowNotification(new GUIContent("이름이 중복 됨 : " + DrawingInfo.CreateCanvasName), 2);
                DrawingInfo.CreateCanvasName = EditorGUILayout.TextField("Name : ", string.Empty);
            }
         
            else
                CreateCanvas();

            GUI.FocusControl(null);

        }
        if (EditorGUI.EndChangeCheck())
            Repaint();

        GUI.enabled = true;
    }
    private void CreateCanvas()
    {
        Camera main = Camera.main;
        main.clearFlags = CameraClearFlags.SolidColor;
        main.backgroundColor = Color.clear;
        main.orthographic = true;
        main.orthographicSize = 10.2f;
        main.transform.position = new Vector3(0, 0, -10);
        main.GetOrAddComponent<FitToScreen>();

        GameCanvas gameCanvas = FindObjectOfType<GameCanvas>();
        if(gameCanvas != null)
            DestroyImmediate(gameCanvas.gameObject);
       
        gameCanvas = new GameObject("Canvas").AddComponent<GameCanvas>();
        DrawingInfo.CreateCanvas(gameCanvas);
        DrawingMode = E_DrawingMode.Edit;
    }
    private void DrawEditCanvasGUI()
    {
        GUILayout.Space(10);

        DrawTopButtonsGUI();
        Rect canvasRect = GetRenderTextureRect();

        DrawCanvasInfoGUI(canvasRect);
        DrawGridTextureGUI();
        GUI.DrawTexture(canvasRect, _renderTex, ScaleMode.ScaleToFit);
        DrawCameraBorderGUI(Color.black);
        DrawPaintBrushGUI(canvasRect);
        DrawBrushInfoGUI(canvasRect);
        DrawSelectBrushGUI(canvasRect);
        DrawScreenRectBorder(EditorHelper.Sky);
        
        InputCanvasWheel(canvasRect);
        InputCanvasKeyCode();

        Repaint();
    }
    private void DrawTopButtonsGUI()
    {
        GUILayout.BeginHorizontal(GUI.skin.box);

        if (GUILayout.Button(EditorHelper.GetTrIcon("back", "뒤로 가기"), GUILayout.Width(40), GUILayout.Height(30)))
        {
            LayerInfo.Clear();
            GetPreviewCanvasList();

            DrawingInfo.CreateCanvasName = EditorGUILayout.TextField("Name : ", string.Empty);
            GUI.FocusControl(null);

            DrawingMode = E_DrawingMode.Create;
        }
        GUILayout.FlexibleSpace();

        EditMode = (E_EditMode)GUILayout.Toolbar((int)_editMode, _editGUIContents, GUILayout.Width(40 * (int)E_EditMode.End), GUILayout.Height(30));

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Save", GUILayout.Width(100), GUILayout.Height(30)))
        {
            Save();
        }
        GUILayout.EndHorizontal();
    }

    private void GetPreviewCanvasList()
    {
        DrawingInfo.Clear();

        string path = Application.dataPath + "/Resources/Canvas/";
        DirectoryInfo dir = new DirectoryInfo(path);
        DrawingInfo.CanvasFileInfo = dir.GetFiles("*.bin");
        foreach (FileInfo f in DrawingInfo.CanvasFileInfo)
        {
            byte[] bytes = File.ReadAllBytes(f.FullName);
            if(bytes != null)
            {
                GameCanvas gameCanvas = new GameObject("Canvas").AddComponent<GameCanvas>();
                DataLoader.Import(ref gameCanvas , bytes, DrawingInfo.ED, LayerInfo.ED, BrushInfo.ED, true);
                DrawingInfo.ED.CanvasObjects.Add(gameCanvas.Id, gameCanvas);
                DestroyImmediate(gameCanvas.gameObject);     
            }
        }
    }

    private Vector2 _scrollPos;

    private void DrawPreviewCanvasGUI()
    {
        Vector2 previewSlotSize = new Vector2(100, 100);

        if (DrawingInfo.ED.Canvases.Count == 0)
        {
            GUILayout.Space(10);
            EditorHelper.DrawCenterLabel(new GUIContent("데이터 없음"), Color.white, 20, FontStyle.Bold);
            return;
        }

        _scrollPos = EditorHelper.DrawGridPreviewCanvasItems(_scrollPos, 10, DrawingInfo.ED.Canvases.Count, position.width, previewSlotSize, (id) =>
        {
             DrawPriviewCanvasItemGUI(previewSlotSize, DrawingInfo.ED.Canvases[id]);    
        });
    }
    
    private void DrawPriviewCanvasItemGUI(Vector2 slotSize, DataCanvas item)
    {
        var area = GUILayoutUtility.GetRect(slotSize.x, slotSize.y, GUIStyle.none, GUILayout.MaxWidth(slotSize.x), GUILayout.MaxHeight(slotSize.y));
        Texture2D previewTex = item.Snapshot ?? Resources.Load<Texture2D>("Textures/Grid");

        if (GUI.Button(area, ""))
        {
            Load(item);
        }
        var textureRect = new Rect(area.x + area.width * 0.25f, area.y + area.height * 0.25f, area.width * 0.5f, area.height * 0.5f);
        GUI.DrawTexture(textureRect, previewTex);

        var labelRect = new Rect(area.x, area.y + area.height * 0.75f, area.width, area.height * 0.25f);
        GUI.Label(labelRect, item.Name + ".bin", EditorHelper.PreviewCanvasLabel(Color.white));
    }
    private void Save()
    {
        var path = EditorUtility.SaveFilePanel("캔버스 저장", Application.streamingAssetsPath + "Canvas/", DrawingInfo.CurrentCanvas.Name + ".bin", "bin");
        if (string.IsNullOrEmpty(path) == false)
        {
            DrawingInfo.CurrentCanvas.Snapshot = Snapshot.CaptureLayerSnapshot();
            byte[] data = DataLoader.Serialize(DrawingInfo.CurrentCanvas , DrawingInfo.ED, LayerInfo.ED , BrushInfo.ED);
            File.WriteAllBytes(path, data);
            ShowNotification(new GUIContent("저장 성공!!"), 2);
        }
    }
    private void Load(DataCanvas canvas)
    {
        Debug.Log("Load : " + canvas.Name);
        FileInfo file = DrawingInfo.GetPreviewCanvasFile(canvas.Name);
        if (file == null)
            return;

        byte[] bytes = File.ReadAllBytes(file.FullName);

        if (bytes != null)
        {
            LayerInfo.Clear();
            BrushInfo.Clear();

            GameCanvas gameCanvas = new GameObject("Canvas").AddComponent<GameCanvas>();
            DataLoader.Import(ref gameCanvas, bytes, DrawingInfo.ED, LayerInfo.ED, BrushInfo.ED);

            DrawingInfo.CreateCanvasName = canvas.Name;

            DrawingInfo.LoadCanvas(gameCanvas);
            LayerInfo.LoadLayer();
            BrushInfo.LoadBrush();
         
            DrawingMode = E_DrawingMode.Edit;
        }
    }
    private void DrawCanvasInfoGUI(Rect canvasRect)
    {
        GUILayout.BeginHorizontal(GUI.skin.box);

        int orthographicSize = (int)(_captureCam.orthographicSize / Camera.main.orthographicSize * 100);
        EditorHelper.CanvasInfoLabel(DrawingInfo.CreateCanvasName + " : " + orthographicSize + "%", 200 , 20);

        string x = "X : ";
        string y = "Y : ";

        Vector2 mousePos = Event.current.mousePosition;
        if (canvasRect.Contains(mousePos))
        {
            Vector2 relativeMousePos = new Vector2((mousePos.x - canvasRect.x) / canvasRect.width, (mousePos.y - canvasRect.y) / canvasRect.height);
            Vector2 pixelPos = new Vector2(relativeMousePos.x * canvasRect.width, relativeMousePos.y * canvasRect.height);

            Ray ray = _captureCam.ScreenPointToRay(pixelPos);
            RaycastHit hit;

            Vector3 worldPos = Vector3.zero;

            if (Physics.Raycast(ray, out hit))
                worldPos = hit.point;

            x += worldPos.x.ToString("F2");
            y += worldPos.y.ToString("F2");
        }

        EditorHelper.CanvasInfoLabel(x, 100, 20 , TextAnchor.MiddleLeft);
        EditorHelper.CanvasInfoLabel(y, 100, 20 , TextAnchor.MiddleLeft);

        GUILayout.EndHorizontal();
    }

    private void DrawPaintBrushGUI(Rect canvasRect)
    {
        if (BrushInfo.CurrentBrush != null && LayerInfo.ED.SelectedLayerIds.Count == 0)
            return;

        if (EditMode != E_EditMode.Paint)
            return;

    

        Event e = Event.current;
        Vector2 mousePos = e.mousePosition;

        if(canvasRect.Contains(mousePos))
        {
            Vector2 uvCoord = new Vector2((mousePos.x - canvasRect.x) / canvasRect.width, (mousePos.y - canvasRect.y) / canvasRect.height);
            Ray ray = _captureCam.ViewportPointToRay(new Vector3(uvCoord.x, 1 - uvCoord.y, 0));
            RaycastHit hitInfo;

            if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity))
            {
                if (_initialMousePos != Vector3.zero)
                {
                    Vector3 direction = hitInfo.point - _initialMousePos;
                    if (e.shift && Mathf.Abs(direction.x) > Mathf.Abs(direction.y) && Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
                        hitInfo.point = new Vector3(hitInfo.point.x, _initialMousePos.y, _initialMousePos.z);

                    else if (e.shift && Mathf.Abs(direction.y) > Mathf.Abs(direction.z))
                        hitInfo.point = new Vector3(_initialMousePos.x, hitInfo.point.y, _initialMousePos.z);

                    else if (e.shift)
                        hitInfo.point = new Vector3(_initialMousePos.x, _initialMousePos.y, hitInfo.point.z);
                }

                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    if (_initialMousePos == Vector3.zero && e.shift == false)
                    {
                        _initialMousePos = hitInfo.point;
                        BrushInfo.PaintBrush(Utils.SetZVectorZero(hitInfo.point));
                    }


                    Repaint();
                    e.Use();

                }

                else if (e.type == EventType.MouseUp && e.button == 0)
                {

                    if (e.shift && _initialMousePos != Vector3.zero)
                    {
                        Vector3 direction = (hitInfo.point - _initialMousePos).normalized;
                        float distanceBetweenCubes = Vector3.Distance(hitInfo.point, _initialMousePos);
                        int numberOfCubes = Mathf.RoundToInt(distanceBetweenCubes / BrushInfo.ED.PlacementDistance);

                        Debug.Log("Num Of Cube : " + (numberOfCubes + 1));

                        for (int i = 1; i <= numberOfCubes; i++)
                        {
                            Vector3 cubePosition = _initialMousePos + direction * BrushInfo.ED.PlacementDistance * i;
                            BrushInfo.PaintBrush(Utils.SetZVectorZero(cubePosition));
                        }
                    }
       
                    _initialMousePos = Vector3.zero;

                    Repaint();
                    e.Use();

                    Utils.AddUndo("Paint Brush", () =>
                    {
                        var destroyedBrushes = BrushInfo.ED.BrushObjects.Where(x => x.Value == null).Select(x => x.Key).ToList();
                        if (destroyedBrushes.Count > 0)
                        {
                            foreach (int id in destroyedBrushes)
                            {
                                BrushInfo.ToDeleteIds.Add(id);
                                BrushInfo.EmptyGenerateIds.Add(id);
                            }
                        }
                    });
                }
                else if (e.type == EventType.MouseDrag && e.button == 0 && e.shift == false)
                {
                    if (Vector3.Distance(_lastPlacedPos, hitInfo.point) >= BrushInfo.ED.PlacementDistance)
                    {
                        BrushInfo.PaintBrush(Utils.SetZVectorZero(hitInfo.point));
                        _lastPlacedPos = hitInfo.point;
                    }


                    Repaint();
                    e.Use();
                }

            }
        }
    }

    Vector2 _brushScrollPos;

    private void DrawBrushInfoGUI(Rect canvasRect)
    {
        if (LayerInfo.ED.SelectedLayerIds.Count == 0)
        {
            GUI.Label(new Rect(canvasRect.x, canvasRect.y + canvasRect.height, position.width, 50), "레이어를 선택하세요", EditorHelper.NotSelectedLayerLabel());
            return;
        }

        var area = new Rect(s_areaX, canvasRect.y + canvasRect.height, position.width, position.height / 2);

        float elementHeight = 15;
        float offsetY = 0;
        float currentY = offsetY; 
        float contentHeight = 0;

        foreach (int selectId in LayerInfo.ED.SelectedLayerIds)
        {
            foreach (var brush in BrushInfo.ED.BrushObjects)
            {
                if (brush.Value == null || brush.Value.ParentLayer != selectId)
                    continue;

                contentHeight += elementHeight + offsetY; 
            }
        }

        Rect contentRect = new Rect(0, 0, area.width, contentHeight);
        _brushScrollPos = GUI.BeginScrollView(area, _brushScrollPos, contentRect, false, true);

        foreach (int selectId in LayerInfo.ED.SelectedLayerIds)
        {
            foreach (var brush in BrushInfo.ED.BrushObjects)
            {
                if (brush.Value == null || brush.Value.ParentLayer != selectId)
                    continue;

                Rect labelRect = new Rect(s_areaX + 5, currentY, area.width, elementHeight);
                Color origin = GUI.color;
                if (GUI.Button(labelRect, new GUIContent(), EditorHelper.PaintBrushInfoBox(brush.Value.IsSelected)))
                {
                    brush.Value.IsSelected = !brush.Value.IsSelected;
                    Undo.RegisterCompleteObjectUndo(brush.Value, "Select Brush");
                    Utils.AddUndo("Select Brush", () => { brush.Value.IsSelected = !brush.Value.IsSelected; });
    
                }
                GUI.color = origin;
                GUI.Label(labelRect, new GUIContent("Brush " + brush.Value.Id.ToString("D3")));
                currentY += elementHeight + offsetY;
            }
        }

        GUI.EndScrollView();
        Repaint();
    }

    private Vector2 _selectionStart;
    private Vector2 _selectionEnd;
    private bool _isDragging;
    private void DrawSelectBrushGUI(Rect canvasRect)
    {
        if (_editMode != E_EditMode.Select)
            return;

        Event e = Event.current;
        Vector2 mousePos = e.mousePosition;
        Rect newRect = new Rect(canvasRect.x, canvasRect.y, position.width, canvasRect.height);

        if (newRect.Contains(mousePos))
        {
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                _isDragging = true;
                _selectionStart = _selectionEnd =  mousePos;

            }
            if (e.type == EventType.MouseDrag && _isDragging)
                _selectionEnd = mousePos;
        }

        if (e.type == EventType.MouseUp && _isDragging)
        {

            _isDragging = false;
            var draggedRect = EditorHelper.GetDraggedRect(_selectionStart, _selectionEnd);
            draggedRect.y += (canvasRect.y + position.y );

            List<int> selectedBrushesId = new List<int>();

            foreach (var brush  in BrushInfo.ED.BrushObjects)
            {
                Vector3 brushWorldPos = brush.Value.transform.position;
                Vector3 brushScreenPos = _captureCam.WorldToScreenPoint(brushWorldPos);
               
                brushScreenPos.y = (position.height - brushScreenPos.y) - canvasRect.y; // Y 좌표 반전

                Vector2 screenPos2D = new Vector2(brushScreenPos.x, brushScreenPos.y);
          
                if (draggedRect.Contains(screenPos2D)) 
                {
                    brush.Value.IsSelected = true;
                    selectedBrushesId.Add(brush.Key);
                    Undo.RegisterCompleteObjectUndo(brush.Value, "Select Dragged Brush");
                }    
            }

            if(selectedBrushesId.Count > 0)
            {
                Utils.AddUndo("Select Dragged Brush", () =>
                {
                    if (selectedBrushesId.Count > 0)
                    {
                        foreach (int id in selectedBrushesId)
                        {
                            BrushInfo.ED.BrushObjects[id].IsSelected = false;
                        }
                    }
                });
            }
        }
    }

    void DrawScreenRectBorder(Color color)
    {
        if(_isDragging)
        {
            var draggedRect = EditorHelper.GetDraggedRect(_selectionStart, _selectionEnd);
            color.a = .25f;
            EditorGUI.DrawRect(draggedRect, color);
        }
    }
    private void InputCanvasKeyCode() // 키 관련
    {
        if (Event.current.type != EventType.KeyDown)
            return;

        if ( Event.current.keyCode == KeyCode.Q)
        {
            EditMode = E_EditMode.Move;
            Event.current.Use();
        }
        else if (Event.current.keyCode == KeyCode.W)
        {
            EditMode = E_EditMode.Select;
            Event.current.Use();
        }
        else if (Event.current.keyCode == KeyCode.E)
        {
            EditMode = E_EditMode.Paint;
            Event.current.Use();
        }
        else if (Event.current.keyCode == KeyCode.R)
        {
            EditMode = E_EditMode.Erase;
            Event.current.Use();
        }

        Repaint();
    }
    private void InputCanvasWheel(Rect canvasRect) //휠 관련
    {
        Event e = Event.current;
        
         if (e.type == EventType.ScrollWheel)
        {     
            if(canvasRect.Contains(e.mousePosition))
            {
                float scrollDelta = e.delta.y;
                float newOrthographicSize = Mathf.Min(_captureCam.orthographicSize + scrollDelta, 50);

                _captureCam.orthographicSize = Mathf.Max(newOrthographicSize, Camera.main.orthographicSize);
            }
            e.Use();
        }
    }

    private Rect GetRenderTextureRect()
    {
        float aspectRatio = Camera.main.aspect;

        float scaledWidth = DrawingInfo.CreateCanvasSize.x;
        float scaledHeight = DrawingInfo.CreateCanvasSize.y;

        if (scaledWidth / aspectRatio > DrawingInfo.CreateCanvasSize.y)
            scaledWidth = DrawingInfo.CreateCanvasSize.y * aspectRatio;
        else
            scaledHeight = DrawingInfo.CreateCanvasSize.x / aspectRatio;

        float renderWidth = scaledWidth;
        float renderHeight = scaledHeight;
        float offsetX = (scaledWidth - renderWidth) / 2f;
        float offsetY = (scaledHeight - renderHeight) / 2f;

        return new Rect(s_areaX + offsetX, s_areaY + offsetY, renderWidth, renderHeight);
    }
    private void DrawGridTextureGUI()
    {
        Texture2D gridTex =  Resources.Load<Texture2D>("Textures/Grid");

        float tileWidth = gridTex.width;
        float tileHeight = gridTex.height;

        int columns = Mathf.CeilToInt(position.width / tileWidth);
        int rows = Mathf.CeilToInt(DrawingInfo.CreateCanvasSize.y / tileHeight);

        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                Rect tileRect = new Rect(i * tileWidth, j * tileHeight, tileWidth, tileHeight);
                if (tileRect.x < position.width && tileRect.y < DrawingInfo.CreateCanvasSize.y)
                {
                    float clippedWidth = Mathf.Min(tileWidth, position.width - tileRect.x);
                    float clippedHeight = Mathf.Min(tileHeight, DrawingInfo.CreateCanvasSize.y - tileRect.y);

                    if (clippedWidth > 0 && clippedHeight > 0)
                    {
                        Rect clippedRect = new Rect(s_areaX + tileRect.x, s_areaY + tileRect.y, clippedWidth, clippedHeight);
                        GUI.DrawTexture(clippedRect, gridTex);
                    }
                }
            }
        }
    }
    private void DrawCameraBorderGUI( Color color)
    {
        float thickness = 2f;
        float size = _captureCam.orthographicSize / Camera.main.orthographicSize;
        float cameraAreaWidth = DrawingInfo.CreateCanvasSize.x / size;
        float cameraAreaHeight = DrawingInfo.CreateCanvasSize.y / size;

        float cameraAreaX = (DrawingInfo.CreateCanvasSize.x - cameraAreaWidth) / 2f;
        float cameraAreaY = (DrawingInfo.CreateCanvasSize.y - cameraAreaHeight) / 2f;

        Rect rect = new Rect(s_areaX + cameraAreaX, s_areaY + cameraAreaY, cameraAreaWidth , cameraAreaHeight);
        Color originalColor = GUI.color;
        GUI.color = color;

        GUI.DrawTexture(new Rect(rect.x, rect.y - thickness, rect.width, thickness), EditorGUIUtility.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height, rect.width, thickness), EditorGUIUtility.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x - thickness, rect.y, thickness, rect.height), EditorGUIUtility.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x + rect.width, rect.y, thickness, rect.height), EditorGUIUtility.whiteTexture);

        GUI.color = originalColor;
    }

 
}
