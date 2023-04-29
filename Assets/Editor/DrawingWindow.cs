using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DrawingWindow : EditorWindow
{
    private Texture2D _gridTexture;

    private Camera _captureCamera;
    private RenderTexture _renderTexture;

    private Vector3 _initialMousePos;
    private Vector3 _lastPlacedPos;

    [MenuItem("Photoshop/Drawing")]
    public static void ShowWindow()
    {
        GetWindow<DrawingWindow>("Drawing");
    }
    public void ShowAtPosition(Rect position)
    {
        var window = GetWindow<DrawingWindow>("Drawing");
        window.position = position;
    }

    private void OnEnable()
    {
        _gridTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Textures/Grid.png");

        InitializeCaptureCamera();
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
    private void InitializeCaptureCamera()
    {
        _captureCamera = GameObject.Find("DrawCamera")?.GetComponent<Camera>();
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

        // 캡처용 RenderTexture를 생성합니다.
        _renderTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        _renderTexture.Create();

        // 카메라의 출력을 RenderTexture로 설정합니다.
        _captureCamera.targetTexture = _renderTexture;
    }


    private void OnGUI()
    {
        float windowWidth = position.width;
        float windowHeight = position.height;

        float scaledWidth = 0;
        float scaledHeight = 0;
        Rect textureRect = new Rect(0, 0, scaledWidth, scaledHeight);

        // RenderTexture를 편집기 윈도우에 표시합니다.
        if (_renderTexture != null)
        {
            // 게임 화면 텍스처의 원래 크기와 비율을 유지하면서 창 크기에 맞게 확대/축소합니다.
            float aspectRatio = (float)_renderTexture.width / (float)_renderTexture.height;
            scaledWidth = windowWidth;
            scaledHeight = windowHeight;

            if (scaledWidth / aspectRatio > windowHeight)
            {
                scaledWidth = windowHeight * aspectRatio;
            }
            else
            {
                scaledHeight = windowWidth / aspectRatio;
            }

            // 확대/축소된 텍스처를 그립니다.
            textureRect = new Rect(0, 0, scaledWidth, scaledHeight);
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
                        // 아래 여백 문제를 해결하기 위해 아래 코드를 추가합니다.
                        float clippedWidth = Mathf.Min(tileWidth, scaledWidth - tileRect.x);
                        float clippedHeight = Mathf.Min(tileHeight, scaledHeight - tileRect.y);

                        if (clippedWidth > 0 && clippedHeight > 0)
                        {
                            Rect clippedRect = new Rect(tileRect.x, tileRect.y, clippedWidth, clippedHeight);
                            if (textureRect.Contains(new Vector2(clippedRect.x, clippedRect.y)) || textureRect.Contains(new Vector2(clippedRect.x + clippedWidth, clippedRect.y + clippedHeight)))
                            {
                                GUI.DrawTexture(clippedRect, _gridTexture);
                            }
                        }
                    }
                }
            }
        }

        // RenderTexture를 편집기 윈도우에 표시합니다.
        if (_renderTexture != null)
        {
            GUI.DrawTexture(textureRect, _renderTexture, ScaleMode.ScaleToFit);
        }

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
                            {
                                hitInfo.point = new Vector3(hitInfo.point.x, _initialMousePos.y, _initialMousePos.z);
                            }
                            else if (shiftPressed && Mathf.Abs(direction.y) > Mathf.Abs(direction.z))
                            {
                                hitInfo.point = new Vector3(_initialMousePos.x, hitInfo.point.y, _initialMousePos.z);
                            }
                            else if (shiftPressed)
                            {
                                hitInfo.point = new Vector3(_initialMousePos.x, _initialMousePos.y, hitInfo.point.z);
                            }
                        }

                        if (e.type == EventType.MouseDown && e.button == 0)
                        {
                            BrushEditor.PlaceCube(hitInfo.point);
                            _lastPlacedPos = hitInfo.point;
                            _initialMousePos = hitInfo.point;
                            e.Use();

                            Repaint();
                        }
                        else if (e.type == EventType.MouseDrag && e.button == 0)
                        {
                            if (Vector3.Distance(_lastPlacedPos, hitInfo.point) >= BrushEditor.ED.PlacementDistance)
                            {
                                BrushEditor.PlaceCube(hitInfo.point);
                                _lastPlacedPos = hitInfo.point;
                            }
                            e.Use();

                            Repaint();
                        }
                        else if (e.type == EventType.MouseUp && e.button == 0)
                        {
                            BrushEditor.CurrentLayer = null;
                            _initialMousePos = Vector3.zero;
                        }
                        else if (e.type == EventType.Layout)
                        {
                            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                        }
                    }
                }
            }
        
    }
}
