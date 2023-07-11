using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class DrawingInfo : InfoData<DrawingInfoData>
{

    public static DataCanvas CurrentCanvas { get; set; }
    public static GameCanvas GameCanvas => GetGameCanvas();

    public static string CreateCanvasName { get; set; }

    public static  Vector2 CreateCanvasSize { get; set; } =  new Vector2(1920 / 2.5f, 1080 / 2.5f); // 기본 캔버스 사이즈 설정

    public static FileInfo[] CanvasFileInfo;
    [InitializeOnLoadMethod]
    private static void Initialize() => EditorSceneManager.sceneOpened += OnSceneOpened;

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode) => Clear();

    public static void ClearHandler()
    { 
        ED.Canvases.Clear();
        ED.CanvasObjects.Clear();
        GameCanvas gameCanvas = UnityEngine.Object.FindObjectOfType<GameCanvas>();
        if(gameCanvas != null)
         UnityEngine.Object.DestroyImmediate(gameCanvas.gameObject);

        CurrentCanvas = null;
    }

    public static void CreateCanvas(GameCanvas gameCanvas)
    {
        int newCanvasId = NewGenerateId<GameCanvas>(ED.CanvasObjects);

        DataCanvas canvas = new DataCanvas()
        {
            Id = newCanvasId,
            Name = CreateCanvasName,
            Size = new Vector2Int(1920, 1080),
        };

        gameCanvas.Initialize(newCanvasId);
        ED.Canvases.Add(canvas); // TODO : 세이브 여부 물어볼지는 나중에.. 
        CurrentCanvas = canvas;
    }
    public static void LoadCanvas(GameCanvas gameCanvas)
    {
        gameCanvas.Initialize(gameCanvas.Id);
        ED.CanvasObjects[gameCanvas.Id] = gameCanvas;
        CurrentCanvas = ED.GetCanvasById(gameCanvas.Id);
    }

    public static GameCanvas GetGameCanvas()
    {
        return UnityEngine.Object.FindObjectOfType<GameCanvas>();
    }
    public static bool IsNameDoubleCheck(string name)
    {
        foreach(DataCanvas canvas in ED.Canvases)
        {
            if (canvas != null && canvas.Name == name)
                return true; 
        }
        return false;
    }

    public static FileInfo GetPreviewCanvasFile(string fileName)
    {
        foreach (FileInfo f in CanvasFileInfo)
        {
            if (fileName == Regex.Replace(f.Name, @"\.bin$", ""))
                return f;
        }
        return null;
    }
}
