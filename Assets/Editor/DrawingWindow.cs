using System.Collections;
using System.Collections.Generic;
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

    private Texture2D _canvasTexture;
    private Vector2 _canvasSize = new Vector2(800, 600); // 기본 캔버스 사이즈 설정

    private Texture2D _gridTexture;

    private Camera _captureCamera;
    private RenderTexture _renderTexture;

    private Vector3 _initialMousePos;
    private Vector3 _lastPlacedPos;

    [MenuItem("Photoshop/Drawing")]
    public static void ShowWindow()
    {
        GetWindow<BrushWindow>("Drawing");
    }
    private void OnEnable()
    {

        _gridTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Textures/Grid.png");

        InitializeCaptureCamera();
        InitializeCanvasTexture();

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
        InitializeCanvasTexture();
    }
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        InitializeCaptureCamera();
        InitializeCanvasTexture();
    }

    private void InitializeCanvasTexture()
    {
        _canvasTexture = new Texture2D((int)_canvasSize.x, (int)_canvasSize.y, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[_canvasTexture.width * _canvasTexture.height];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white; // 기본 캔버스 텍스처 색상 설정
        }

        _canvasTexture.SetPixels(pixels);
        _canvasTexture.Apply(); // 변경 사항을 적용합니다.
    }

    private void InitializeCaptureCamera()
    {
        GameObject cam = GameObject.Find("DrawCamera");
        if (cam == null)
        {
            _captureCamera = new GameObject("DrawCamera").AddComponent<Camera>();
        }
        else
        {
            _captureCamera = cam.GetComponent<Camera>();
        }

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

        _renderTexture = new RenderTexture((int)_canvasSize.x, (int)_canvasSize.y, 24, RenderTextureFormat.ARGB32);
        _renderTexture.Create();

        _captureCamera.targetTexture = _renderTexture;
    }


    private void OnGUI()
    {
        // 사용자 입력 UI를 렌더 텍스처 바로 아래 그립니다.
        // 사용자 입력 UI를 렌더 텍스처 바로 아래 그립니다.
        float padding = 20;
        float areaWidth = 200;
        float areaHeight = 50;

        float areaX = 0;
        float areaY = areaHeight + padding;

        GUILayout.BeginArea(new Rect(0, 0, areaWidth, areaHeight));
        Vector2 newCanvasSize = EditorGUILayout.Vector2Field("Size", _canvasSize, GUILayout.Width(200));
        GUILayout.EndArea();

        if (newCanvasSize != _canvasSize)
        {
            _canvasSize = newCanvasSize;
            InitializeCanvasTexture();
        }

        if (_canvasTexture != null)
        {
            Rect canvasRect = new Rect(areaX, areaY, _canvasSize.x, _canvasSize.y);
            GUI.DrawTexture(canvasRect, _canvasTexture);
        }

        float windowWidth = position.width;
        float windowHeight = position.height;

        float scaledWidth = 0;
        float scaledHeight = 0;

        Rect textureRect = new Rect(areaX, areaY, scaledWidth, scaledHeight);

        if (_renderTexture != null)
        {
            float aspectRatio = (float)_renderTexture.width / (float)_renderTexture.height;
            scaledWidth = windowWidth;
            scaledHeight = windowHeight;

            if (scaledWidth / aspectRatio > windowHeight)
                scaledWidth = windowHeight * aspectRatio;

            else
                scaledHeight = windowWidth / aspectRatio;

            textureRect = new Rect(areaX, areaY, scaledWidth, scaledHeight);
        }

        if (_gridTexture != null)
        {
            float tileWidth = _gridTexture.width;
            float tileHeight = _gridTexture.height;

            int columns = Mathf.CeilToInt(scaledWidth / tileWidth);
            int rows = Mathf.CeilToInt(scaledHeight / tileHeight);

            for (int i = 0; i < columns; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    Rect tileRect = new Rect(i * tileWidth, j * tileHeight, tileWidth, tileHeight);
                    if (tileRect.x < scaledWidth && tileRect.y < scaledHeight)
                    {
                        float clippedWidth = Mathf.Min(tileWidth, scaledWidth - tileRect.x);
                        float clippedHeight = Mathf.Min(tileHeight, scaledHeight - tileRect.y);

                        if (clippedWidth > 0 && clippedHeight > 0)
                        {
                            Rect clippedRect = new Rect(areaX + tileRect.x, areaY + tileRect.y, clippedWidth, clippedHeight);
                            if (textureRect.Contains(new Vector2(clippedRect.x, clippedRect.y)) || textureRect.Contains(new Vector2(clippedRect.x + clippedWidth, clippedRect.y + clippedHeight)))
                            {
                                GUI.DrawTexture(clippedRect, _gridTexture);
                            }
                        }
                    }
                }
            }
        }

        float cameraAreaWidth = scaledWidth * 0.95f; // 카메라 영역의 가로 길이를 조절합니다 (예: 전체 그리기 영역의 80%)
        float cameraAreaHeight = scaledHeight * 0.95f; // 카메라 영역의 세로 길이를 조절합니다 (예: 전체 그리기 영역의 80%)

        float cameraAreaX = (scaledWidth - cameraAreaWidth) / 2f;
        float cameraAreaY = (scaledHeight - cameraAreaHeight) / 2f;
        Rect cameraAreaRect = new Rect(areaX + cameraAreaX, areaY + cameraAreaY, cameraAreaWidth, cameraAreaHeight);

        // 2. 경계선 두께 정의
        float edgeThickness = 2f;

        // 3. 경계선 그리기
        DrawBorder(cameraAreaRect, edgeThickness, Color.black);

        if (_renderTexture != null)
            GUI.DrawTexture(textureRect, _renderTexture, ScaleMode.ScaleToFit);

        Repaint();

        if (BrushEditor.IsPlacing && BrushEditor.CubePrefab != null)
        {
            Event e = Event.current;
            Vector2 mousePos = e.mousePosition;

            if (textureRect.Contains(mousePos))
            {
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
                     
                            Debug.Log("Num Of Cube : " + (numberOfCubes + 1) );

                            for (int i = 1; i <= numberOfCubes; i++)
                            {
                                Vector3 cubePosition = _initialMousePos + direction * BrushEditor.ED.PlacementDistance * i;
                                BrushEditor.PlaceCube(cubePosition);
                            }
                        }
                        BrushEditor.CurrentLayer = null;
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
                }
            }
        }
    }

    // 경계선을 그리는 함수
    private void DrawBorder(Rect rect, float thickness, Color color)
    {
        Color originalColor = GUI.color;
        GUI.color = color;

        // 상단 경계선
        GUI.DrawTexture(new Rect(rect.x, rect.y - thickness, rect.width, thickness), EditorGUIUtility.whiteTexture);
        // 하단 경계선
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height, rect.width, thickness), EditorGUIUtility.whiteTexture);
        // 좌측 경계선
        GUI.DrawTexture(new Rect(rect.x - thickness, rect.y, thickness, rect.height), EditorGUIUtility.whiteTexture);
        // 우측 경계선
        GUI.DrawTexture(new Rect(rect.x + rect.width, rect.y, thickness, rect.height), EditorGUIUtility.whiteTexture);

        GUI.color = originalColor;
    }
}
