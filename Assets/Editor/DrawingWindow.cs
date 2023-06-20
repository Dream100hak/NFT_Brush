using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;


public class DrawingWindow : EditorWindow
{
    private Vector2 _canvasSize = new Vector2(1920 / 2.5f , 1080 / 2.5f); // 기본 캔버스 사이즈 설정

    float _areaX = 10;
    float _areaY = 50;

    private Camera _captureCam;
    private RenderTexture _renderTexture;

    private Vector3 _initialMousePos;
    private Vector3 _lastPlacedPos;

    [MenuItem("Photoshop/Drawing")]
    public static void ShowWindow()
    {
        GetWindow<DrawingWindow>("Drawing");
    }

    private void OnEnable()
    {
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
        GameObject cam = GameObject.Find("DrawCamera");
        _captureCam = (cam == null) ? new GameObject(name).AddComponent<Camera>() : _captureCam = cam.GetComponent<Camera>();

        _captureCam.transform.position = Camera.main.transform.position;
        _captureCam.transform.rotation = Camera.main.transform.rotation;
        _captureCam.fieldOfView = Camera.main.fieldOfView;
        _captureCam.nearClipPlane = Camera.main.nearClipPlane;
        _captureCam.farClipPlane = Camera.main.farClipPlane;
        _captureCam.orthographic = true;
        _captureCam.orthographicSize = Camera.main.orthographicSize;
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
        Rect textureRect = GetTextureRect();
        DrawGridTextureGUI();

    
        GUI.DrawTexture(textureRect, _renderTexture, ScaleMode.ScaleToFit);

        DrawBorderGUI(Color.black);
        Repaint();

        if ( BrushInfo.CurrentBrush != null)
            DrawPaintBrushGUI(textureRect);
    

        GUILayout.Box(EditorGUIUtility.IconContent("_Popup@2x"));
    }

    private void DrawPaintBrushGUI(Rect textureRect)
    {
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
                if (_initialMousePos == Vector3.zero)
                {
                    _initialMousePos = hitInfo.point;
                    BrushInfo.PaintBrush(Utils.SetZVectorZero(hitInfo.point));
                    e.Use();
                }
                else if (e.shift == false)
                {
                    BrushInfo.PaintBrush(Utils.SetZVectorZero(hitInfo.point));
                    _initialMousePos = hitInfo.point;
                    e.Use();
                }
                Repaint();
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
            }
            else if (e.type == EventType.MouseDrag && e.button == 0 && e.shift == false)
            {
                if (Vector3.Distance(_lastPlacedPos, hitInfo.point) >= BrushInfo.ED.PlacementDistance)
                {
                    BrushInfo.PaintBrush(Utils.SetZVectorZero(hitInfo.point));
                    _lastPlacedPos = hitInfo.point;
                }
                e.Use();

                Repaint();
            }
            else if (e.type == EventType.ScrollWheel)
            {
                float scrollDelta = e.delta.y;
                float newOrthographicSize = Mathf.Min(_captureCam.orthographicSize + scrollDelta , 50);

                _captureCam.orthographicSize = Mathf.Max(newOrthographicSize, Camera.main.orthographicSize);

                e.Use(); 
            }
        }
    }

    private Rect GetTextureRect()
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

        return new Rect(_areaX + offsetX, _areaY + offsetY, renderWidth, renderHeight);
    }
    private void DrawGridTextureGUI()
    {
        Texture2D gridTex =  Resources.Load<Texture2D>("Textures/Grid");

        float tileWidth = gridTex.width;
        float tileHeight = gridTex.height;

        int columns = Mathf.CeilToInt(_canvasSize.x / tileWidth);
        int rows = Mathf.CeilToInt(_canvasSize.y / tileHeight);

        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                Rect tileRect = new Rect(i * tileWidth, j * tileHeight, tileWidth, tileHeight);
                if (tileRect.x < _canvasSize.x && tileRect.y < _canvasSize.y)
                {
                    float clippedWidth = Mathf.Min(tileWidth, _canvasSize.x - tileRect.x);
                    float clippedHeight = Mathf.Min(tileHeight, _canvasSize.y - tileRect.y);

                    if (clippedWidth > 0 && clippedHeight > 0)
                    {
                        Rect clippedRect = new Rect(_areaX + tileRect.x, _areaY + tileRect.y, clippedWidth, clippedHeight);
                        GUI.DrawTexture(clippedRect, gridTex);
                    }
                }
            }
        }
    }
    private void DrawBorderGUI( Color color)
    {
        float thickness = 2f;
        float cameraAreaWidth = _canvasSize.x;
        float cameraAreaHeight = _canvasSize.y;

        Rect rect = new Rect(_areaX, _areaY, cameraAreaWidth, cameraAreaHeight);

        Color originalColor = GUI.color;
        GUI.color = color;

        GUI.DrawTexture(new Rect(rect.x, rect.y - thickness, rect.width, thickness), EditorGUIUtility.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height, rect.width, thickness), EditorGUIUtility.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x - thickness, rect.y, thickness, rect.height), EditorGUIUtility.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x + rect.width, rect.y, thickness, rect.height), EditorGUIUtility.whiteTexture);

        GUI.color = originalColor;
    }

 
}
