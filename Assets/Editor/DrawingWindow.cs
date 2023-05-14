using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum E_DRAWING
{
    SELECT,
    DRAW,
    ERASE,
}

public class DrawingWindow : EditorWindow
{
    private Vector2 _canvasSize = new Vector2(1920, 1080); // 기본 캔버스 사이즈 설정

    float _areaX = 10;
    float _areaY = 50;

    private Texture2D _gridTexture;

    private Camera _captureCamera;
    private RenderTexture _renderTexture;

    private Vector3 _initialMousePos;
    private Vector3 _lastPlacedPos;

    private bool _bStart;

    [MenuItem("Photoshop/Drawing")]
    public static void ShowWindow()
    {
        GetWindow<BrushWindow>("Drawing");
    }

    private void OnEnable()
    {
        _gridTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Textures/Grid.png");

        if(_bStart == false)
        {
            SetResolution(1920, 1080);
            _bStart = true;
        }
        
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

    }
    private void OnDisable()
    {
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        InitializeCaptureCamera();
    }
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        InitializeCaptureCamera();
    }

    private void SetResolution(int width, int height)
    {
        int index = CustomGameViewSize.FindSize(GameViewSizeGroupType.Standalone, width, height);

        if (index == -1)
            return;

        CustomGameViewSize.SetSize(index);

        float aspectRatio = (float)width / height;

        if (aspectRatio == 1)
        {
            _canvasSize.x = width * 0.3f;
            _canvasSize.y = height * 0.3f;
        }
        else
        {
            _canvasSize.x = width * 0.4f;
            _canvasSize.y = height * 0.4f;

        }

        InitializeCaptureCamera();

    }

    private void InitializeCaptureCamera()
    {
        GameObject cam = GameObject.Find("DrawCamera");
        if (cam == null)
            _captureCamera = new GameObject("DrawCamera").AddComponent<Camera>();
        else
            _captureCamera = cam.GetComponent<Camera>();

        _captureCamera.transform.position = Camera.main.transform.position;
        _captureCamera.transform.rotation = Camera.main.transform.rotation;
        _captureCamera.fieldOfView = Camera.main.fieldOfView;
        _captureCamera.nearClipPlane = Camera.main.nearClipPlane;
        _captureCamera.farClipPlane = Camera.main.farClipPlane;
        _captureCamera.orthographic = Camera.main.orthographic;
        _captureCamera.orthographicSize = Camera.main.orthographicSize;
        _captureCamera.clearFlags = Camera.main.clearFlags;
        _captureCamera.backgroundColor = Camera.main.backgroundColor;
        _captureCamera.cullingMask = Camera.main.cullingMask;
        _captureCamera.depth = Camera.main.depth;

        float aspectRatio = _canvasSize.x / _canvasSize.y;

        if (Camera.main.aspect != aspectRatio)
            _captureCamera.aspect = aspectRatio;

        else
            _captureCamera.aspect = Camera.main.aspect;

        _renderTexture = new RenderTexture((int)_canvasSize.x, (int)_canvasSize.y, 24, RenderTextureFormat.ARGB32);
        _renderTexture.Create();

        _captureCamera.targetTexture = _renderTexture;
    }
    private void OnGUI()
    {
        DrawResolutionBox();

        Rect textureRect = GetTextureRect();

        if (_gridTexture != null)
            DrawGridTexture();

        if (_renderTexture != null)
            DrawRenderTexture(textureRect);

        float edgeThickness = 2f;
        DrawBorder(edgeThickness, Color.black);

        Repaint();

        if (BrushEditor.IsPlacing && BrushEditor.CubePrefab != null)
        {
            Event e = Event.current;
            Vector2 mousePos = e.mousePosition;

            Vector2 uvCoord = new Vector2((mousePos.x - textureRect.x) / textureRect.width, (mousePos.y - textureRect.y) / textureRect.height);

            Ray ray = _captureCamera.ViewportPointToRay(new Vector3(uvCoord.x, 1 - uvCoord.y, 0));
            RaycastHit hitInfo;

            if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity))
            {
                bool shiftPressed = e.shift;
                if (_initialMousePos != Vector3.zero)
                {
                    Vector3 direction = hitInfo.point - _initialMousePos;
                    if (shiftPressed && Mathf.Abs(direction.x) > Mathf.Abs(direction.y) && Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
                        hitInfo.point = new Vector3(hitInfo.point.x, _initialMousePos.y, _initialMousePos.z);

                    else if (shiftPressed && Mathf.Abs(direction.y) > Mathf.Abs(direction.z))
                        hitInfo.point = new Vector3(_initialMousePos.x, hitInfo.point.y, _initialMousePos.z);

                    else if (shiftPressed)
                        hitInfo.point = new Vector3(_initialMousePos.x, _initialMousePos.y, hitInfo.point.z);
                }

                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    if (_initialMousePos == Vector3.zero)
                    {
                        _initialMousePos = hitInfo.point;
                        BrushEditor.PlaceCube(hitInfo.point);
                        e.Use();
                    }
                    else if (!shiftPressed)
                    {
                        BrushEditor.PlaceCube(hitInfo.point);
                        _initialMousePos = hitInfo.point;
                        e.Use();
                    }
                    Repaint();
                }
                else if (e.type == EventType.MouseUp && e.button == 0)
                {
                    if (shiftPressed && _initialMousePos != Vector3.zero)
                    {
                        Vector3 direction = (hitInfo.point - _initialMousePos).normalized;
                        float distanceBetweenCubes = Vector3.Distance(hitInfo.point, _initialMousePos);
                        int numberOfCubes = Mathf.RoundToInt(distanceBetweenCubes / BrushEditor.ED.PlacementDistance);

                        Debug.Log("Num Of Cube : " + (numberOfCubes + 1));

                        for (int i = 1; i <= numberOfCubes; i++)
                        {
                            Vector3 cubePosition = _initialMousePos + direction * BrushEditor.ED.PlacementDistance * i;
                            BrushEditor.PlaceCube(cubePosition);
                        }
                    }
                    LayerEditor.CurrentLayer = null;
                    _initialMousePos = Vector3.zero;
                }
                else if (e.type == EventType.MouseDrag && e.button == 0 && !shiftPressed)
                {
                    if (Vector3.Distance(_lastPlacedPos, hitInfo.point) >= BrushEditor.ED.PlacementDistance)
                    {
                        BrushEditor.PlaceCube(hitInfo.point);
                        _lastPlacedPos = hitInfo.point;
                    }
                    e.Use();

                    Repaint();
                }

                if (e.type == EventType.MouseDrag && e.button == 1)
                {
                    BrushEditor.RemoveCube(hitInfo);
                }
                else if (e.type == EventType.MouseDown && e.button == 1)
                {
                    BrushEditor.RemoveCube(hitInfo);
                }
            }
        }

    }

    private void DrawResolutionBox()
    {
        float buttonWidth = 100f;
        float buttonHeight = 30f;

        GUILayout.BeginArea(new Rect(10, 10, position.width, buttonHeight));
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("1920 x 1080", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
            SetResolution(1920, 1080);


        if (GUILayout.Button("2048 x 2048", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
            SetResolution(2048, 2048);


        GUILayout.EndHorizontal();
        GUILayout.EndArea();
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

        float renderWidth = scaledWidth * 0.8f;
        //float renderWidth = scaledWidth * 1f;
        float renderHeight = scaledHeight * 0.8f;
        //float renderHeight = scaledHeight * 1f;

        float offsetX = (scaledWidth - renderWidth) / 2f;
        float offsetY = (scaledHeight - renderHeight) / 2f;

        return new Rect(_areaX + offsetX, _areaY + offsetY, renderWidth, renderHeight);
    }
    private void DrawGridTexture()
    {
        float tileWidth = _gridTexture.width;
        float tileHeight = _gridTexture.height;

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
                        GUI.DrawTexture(clippedRect, _gridTexture);
                    }
                }
            }
        }
    }

    private void DrawRenderTexture(Rect rect)
    {
        GUI.DrawTexture(rect, _renderTexture, ScaleMode.ScaleToFit);
    }

    private void DrawBorder(float thickness, Color color)
    {
        float cameraAreaWidth = _canvasSize.x * 0.8f;
        float cameraAreaHeight = _canvasSize.y * 0.8f;

        float cameraAreaX = (_canvasSize.x - cameraAreaWidth) / 2f;
        float cameraAreaY = (_canvasSize.y - cameraAreaHeight) / 2f;
        Rect rect = new Rect(_areaX + cameraAreaX, _areaY + cameraAreaY, cameraAreaWidth, cameraAreaHeight);

        Color originalColor = GUI.color;
        GUI.color = color;

        GUI.DrawTexture(new Rect(rect.x, rect.y - thickness, rect.width, thickness), EditorGUIUtility.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height, rect.width, thickness), EditorGUIUtility.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x - thickness, rect.y, thickness, rect.height), EditorGUIUtility.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x + rect.width, rect.y, thickness, rect.height), EditorGUIUtility.whiteTexture);

        GUI.color = originalColor;
    }

 
}
