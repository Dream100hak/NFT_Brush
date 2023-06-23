using UnityEngine;

public class DrawingInfo : InfoData<DrawingInfoData>
{

    public static DrawingCanvas CurrentCanvas
    {
        get { return ED.CanvasDatas[ED.CanvasDatas.Count -1] ?? null; }
        set 
        {
            ED.CanvasDatas.Add(value);
        }
    }
    public static string CreateCanvasName { get; set; }

    public static  Vector2 CreateCanvasSize { get; set; } =  new Vector2(1920 / 2.5f, 1080 / 2.5f); // 기본 캔버스 사이즈 설정

   // public static Dictionary<Vector3, >


}
