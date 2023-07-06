using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DrawingInfo : InfoData<DrawingInfoData>
{

    public static DrawingCanvas CurrentCanvas { get; set; }
    public static GameCanvas GameCanvas => GetGameCanvas();

    public static string CreateCanvasName { get; set; }

    public static  Vector2 CreateCanvasSize { get; set; } =  new Vector2(1920 / 2.5f, 1080 / 2.5f); // 기본 캔버스 사이즈 설정
    //[InitializeOnLoadMethod]
    //private static void Initialize()
    //{
    //    EditorApplication.update += RunOnceOnEditorLoad;
    //    EditorSceneManager.sceneOpened += OnSceneOpened;
    //}
    //private static void RunOnceOnEditorLoad()
    //{
    //    InitializeCanvas();
    //    EditorApplication.update -= RunOnceOnEditorLoad;
    //}
    //private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    //{
    //    InitializeCanvas();
    //}
    public static GameCanvas GetGameCanvas()
    {
        return UnityEngine.Object.FindObjectOfType<GameCanvas>();
      
    }
}
