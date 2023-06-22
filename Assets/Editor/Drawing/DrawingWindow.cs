using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;


public class DrawingWindow : EditorWindow
{
    private E_DrawingMode _drawingMode;
    public E_DrawingMode DrawingMode { get => _drawingMode; set { if (_drawingMode != value) _drawingMode = value; } }
    private E_EditMode _editMode;
    public E_EditMode EditMode { get => _editMode; set { if (_editMode != value) _editMode = value;  } }
    
    private GUIContent[] editGUIContents;

    private string _canvasName = "Good Canvas";
    private Vector2 _canvasSize = new Vector2(1920 / 2.5f , 1080 / 2.5f); // 기본 캔버스 사이즈 설정

    private static float s_areaX = 0;
    private static float s_areaY = 80;
    
    private Camera _captureCam;
    private RenderTexture _renderTexture;

    private Vector3 _initialMousePos;
    private Vector3 _lastPlacedPos;

    [MenuItem("Photoshop/Drawing")]
    public static void ShowWindow() => GetWindow<DrawingWindow>("Drawing");

    private void OnEnable()
    {
        editGUIContents = new GUIContent[]
        {
          EditorHelper.GetTrIcon("ViewToolMove" , "옮기기"),
          EditorHelper.GetTrIcon("Grid.BoxTool" , "선택"),
          EditorHelper.GetTrIcon("Grid.PaintTool" , "그리기"),
          EditorHelper.GetTrIcon("Grid.EraserTool", "지우기")
        };

        InitializeCaptureCamera();
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

    }
    private void OnDisable()
    {
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }
    private void OnSceneOpened(Scene scene, OpenSceneMode mode) =>  InitializeCaptureCamera();
    private void OnPlayModeStateChanged(PlayModeStateChange state) => InitializeCaptureCamera();

    private void InitializeCaptureCamera()
    {
        GameObject cam = GameObject.Find("DrawCamera") ?? new GameObject("DrawCamera");
        if(cam.GetComponent<Camera>() == null)
            cam.AddComponent<Camera>();

        _captureCam = cam.GetComponent<Camera>();

        _captureCam.transform.position = Camera.main.transform.position;
        _captureCam.transform.rotation = Camera.main.transform.rotation;
        _captureCam.fieldOfView = Camera.main.fieldOfView;
        _captureCam.nearClipPlane = Camera.main.nearClipPlane;
        _captureCam.farClipPlane = Camera.main.farClipPlane;
        _captureCam.orthographic = true;
        _captureCam.orthographicSize = Camera.main.orthographicSize + 3f;
        _captureCam.clearFlags = CameraClearFlags.SolidColor;
        _captureCam.backgroundColor = Color.clear;
        _captureCam.cullingMask = Camera.main.cullingMask;
        _captureCam.depth = Camera.main.depth;

        float aspectRatio = _canvasSize.x / _canvasSize.y;
        _captureCam.aspect = (Camera.main.aspect != aspectRatio) ? aspectRatio : Camera.main.aspect;

        _renderTexture = new RenderTexture((int)_canvasSize.x, (int)_canvasSize.y, 24, RenderTextureFormat.ARGB32);
        _renderTexture.Create();

        _captureCam.targetTexture = _renderTexture;
    }

    private void OnGUI()
    {
        switch (DrawingMode)
        {
            case E_DrawingMode.Create:
                DrawCreateCanvasGUI();
                break;
            case E_DrawingMode.Edit:
                DrawEditCanvasGUI();
                break;
        }
    }

    private void DrawCreateCanvasGUI()
    {   
        if (GUILayout.Button("캔버스 만들기", GUILayout.Height(100)))
        {
            CreateCanvas();
        }
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

        DrawingCanvas canvas = FindObjectOfType<DrawingCanvas>() ?? new GameObject("Canvas").AddComponent<DrawingCanvas>();
        canvas.Initialize();

        DrawingMode = E_DrawingMode.Edit;
    }
    private void DrawEditCanvasGUI()
    {
        GUILayout.Space(10);

        DrawTopButtonsGUI();
        Rect canvasRect = GetRenderTextureRect();

        DrawCanvasInfoGUI(canvasRect);
        DrawGridTextureGUI();
        GUI.DrawTexture(canvasRect, _renderTexture, ScaleMode.ScaleToFit);
        DrawCameraBorderGUI(Color.black);

        InputCanvasKeyCode();
        InputCanvasWheel();

        DrawPaintBrushGUI(canvasRect);
        Repaint();
    }
    private void DrawTopButtonsGUI()
    {
        GUILayout.BeginHorizontal(GUI.skin.box);

        if (GUILayout.Button(EditorHelper.GetTrIcon("back", "뒤로 가기"), GUILayout.Width(40), GUILayout.Height(30)))
        {
            DrawingMode = E_DrawingMode.Create;
        }
        GUILayout.FlexibleSpace();

        EditMode = (E_EditMode)GUILayout.Toolbar((int)_editMode, editGUIContents, GUILayout.Width(40 * (int)E_EditMode.End), GUILayout.Height(30));

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Save", GUILayout.Width(100), GUILayout.Height(30)))
        {
            // TODO : Save
        }
        GUILayout.EndHorizontal();
    }

    private void DrawCanvasInfoGUI(Rect canvasRect)
    {
        GUILayout.BeginHorizontal(GUI.skin.box);

        int orthographicSize = (int)(_captureCam.orthographicSize / Camera.main.orthographicSize * 100);
        EditorHelper.CanvasInfoLabel(_canvasName + " @ " + orthographicSize + "%", 200 , 20);

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

    private void DrawPaintBrushGUI(Rect textureRect)
    {

        if (BrushInfo.CurrentBrush != null && LayerInfo.ED.SelectedLayerIds.Count == 0)
            return;

        if (EditMode != E_EditMode.Paint)
            return;

        Event e = Event.current;
        Vector2 mousePos = e.mousePosition;
        Vector2 uvCoord = new Vector2((mousePos.x - textureRect.x) / textureRect.width, (mousePos.y - textureRect.y) / textureRect.height);
        
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
                LayerInfo.CurrentLayer = null;
                _initialMousePos = Vector3.zero;

                Repaint();
                e.Use();
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
    private void InputCanvasKeyCode() // 키 관련
    {
        //Debug.Log(EditMode + " : "  + Event.current );

        if (Event.current.type != EventType.KeyDown)
            return;

        if ( Event.current.keyCode == KeyCode.Q)
        {
            EditMode = E_EditMode.Move;
            Repaint();
            Event.current.Use();
        }
        else if (Event.current.keyCode == KeyCode.W)
        {
            EditMode = E_EditMode.Select;
            Repaint();
            Event.current.Use();
        }
        else if (Event.current.keyCode == KeyCode.E)
        {
            EditMode = E_EditMode.Paint;
            Repaint();
            Event.current.Use();
        }
        else if (Event.current.keyCode == KeyCode.R)
        {
            EditMode = E_EditMode.Erase;
            Repaint();
            Event.current.Use();
        }

    }
    private void InputCanvasWheel() //휠 관련
    {
        Event e = Event.current;
        
         if (e.type == EventType.ScrollWheel)
        {
            float scrollDelta = e.delta.y;
            float newOrthographicSize = Mathf.Min(_captureCam.orthographicSize + scrollDelta, 50);

            _captureCam.orthographicSize = Mathf.Max(newOrthographicSize, Camera.main.orthographicSize);

            e.Use();
        }
    }

    private Rect GetRenderTextureRect()
    {
        float aspectRatio = Camera.main.aspect;

        float scaledWidth = _canvasSize.x;
        float scaledHeight = _canvasSize.y;

        if (scaledWidth / aspectRatio > _canvasSize.y)
            scaledWidth = _canvasSize.y * aspectRatio;
        else
            scaledHeight = _canvasSize.x / aspectRatio;

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
        int rows = Mathf.CeilToInt(_canvasSize.y / tileHeight);

        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                Rect tileRect = new Rect(i * tileWidth, j * tileHeight, tileWidth, tileHeight);
                if (tileRect.x < position.width && tileRect.y < _canvasSize.y)
                {
                    float clippedWidth = Mathf.Min(tileWidth, position.width - tileRect.x);
                    float clippedHeight = Mathf.Min(tileHeight, _canvasSize.y - tileRect.y);

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
        float cameraAreaWidth = _canvasSize.x / size;
        float cameraAreaHeight = _canvasSize.y / size;

        float cameraAreaX = (_canvasSize.x - cameraAreaWidth) / 2f;
        float cameraAreaY = (_canvasSize.y - cameraAreaHeight) / 2f;

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
