using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class DrawingInfo : InfoData<DrawingInfoData>
{
    public static Dictionary<int, GameCanvas> CanvasObjects { get; set; } = new Dictionary<int, GameCanvas>();
    public static DrawingCanvas CurrentCanvas { get; set; }
    public static GameCanvas GameCanvas => GetGameCanvas();

    public static string CreateCanvasName { get; set; }

    public static  Vector2 CreateCanvasSize { get; set; } =  new Vector2(1920 / 2.5f, 1080 / 2.5f); // 기본 캔버스 사이즈 설정
    [InitializeOnLoadMethod]
    private static void Initialize() => EditorSceneManager.sceneOpened += OnSceneOpened;

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        Clear();
    }
    public static void ClearHandler()
    {
        DrawingInfo.CanvasObjects.Clear();
        DrawingInfo.ED.Canvases.Clear();
        GameCanvas gameCanvas = UnityEngine.Object.FindObjectOfType<GameCanvas>();
        UnityEngine.Object.DestroyImmediate(gameCanvas.gameObject);

        DrawingInfo.CurrentCanvas = null;
    }

    public static void CreateCanvas(GameCanvas gameCanvas)
    {
        int newCanvasId = NewGenerateId<GameCanvas>(CanvasObjects);

        DrawingCanvas canvas = new DrawingCanvas()
        {
            Id = newCanvasId,
            Name = CreateCanvasName,
            Size = new Vector2Int(1920, 1080),
        };

        gameCanvas.Initialize(newCanvasId);

        ED.Canvases.Add(canvas); // TODO : 세이브 여부 물어볼지는 나중에.. 
        CanvasObjects.Add(newCanvasId, gameCanvas);
        CurrentCanvas = canvas;
    }

    public static GameCanvas GetGameCanvas()
    {
        return UnityEngine.Object.FindObjectOfType<GameCanvas>();
      
    }
}
