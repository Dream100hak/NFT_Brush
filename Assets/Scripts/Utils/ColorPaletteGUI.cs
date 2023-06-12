using System;
using UnityEngine;


public class ColorPaletteGUI : MonoBehaviour
{ 
    private static Color _color = Color.red;
    public static Color Color { get { return _color; } set { Init(value); } }
    private static Action _update;

    public static Action ApplySaturation;
    public static Action ApplyHue;

    public static Texture2D HueTex;
    public static Texture2D SatTex;
    public static Texture2D ResultTex;

    public static float Hue, Saturation, Value;

    public static void RGBToHSV(Color color)
    {
        float cmin = Mathf.Min(color.r, color.g, color.b);
        float cmax = Mathf.Max(color.r, color.g, color.b);
        float d = cmax - cmin;
        if (d == 0)
            Hue = 0;
        else if (cmax == color.r)
            Hue = Mathf.Repeat((color.g - color.b) / d, 6);
        else if (cmax == color.g)
            Hue = (color.b - color.r) / d + 2;
        else
            Hue = (color.r - color.g) / d + 4;

        Saturation = cmax == 0 ? 0 : d / cmax;
        Value = cmax;
    }
    private static void Init(Color inputColor)
    {
        var hueColors = new Color[] {
            Color.red,
            Color.yellow,
            Color.green,
            Color.cyan,
            Color.blue,
            Color.magenta,
        };
        var satColors = new Color[] {
            new Color( 0, 0, 0 ),
            new Color( 0, 0, 0 ),
            new Color( 1, 1, 1 ),
            hueColors[0],
        };

        Texture2D hueTex = new Texture2D(1, 7);
        for (int i = 0; i < 7; i++)
        {
            hueTex.SetPixel(0, i, hueColors[i % 6]);
        }
        hueTex.Apply();

        Texture2D interpolatedHueTex = new Texture2D(20, 200);
        for (int x = 0; x < 20; x++)
        {
            for (int y = 0; y < 200; y++)
            {
                float t = (float)y / 199; // 보간을 위한 비율 값 계산 (0 ~ 1)
                float hueIndex = t * 6f; // hue 인덱스 계산

                // 보간에 사용할 색상 인덱스 계산
                int colorIndex1 = Mathf.FloorToInt(hueIndex) % 6;
                int colorIndex2 = (colorIndex1 + 1) % 6;

                // hue 보간
                float lerpAmount = hueIndex % 1f;
                Color color = Color.Lerp(hueTex.GetPixel(0, colorIndex1), hueTex.GetPixel(0, colorIndex2), lerpAmount);
                interpolatedHueTex.SetPixel(x, y, color);
            }
        }
        interpolatedHueTex.Apply();

        Texture2D satTex = new Texture2D(200, 200);
        Action resetSatTex = () =>
        {
            for (int j = 0; j < satTex.height; j++)
            {
                for (int i = 0; i < satTex.width; i++)
                {
                    float x = (float)i / (satTex.width - 1);
                    float y = (float)j / (satTex.height - 1);
                    Color color = Color.Lerp(Color.Lerp(satColors[0], satColors[1], x), Color.Lerp(satColors[2], satColors[3], x), y);
                    satTex.SetPixel(i, j, color);
                }
            }
            satTex.Apply();
        };
        RGBToHSV(inputColor);

        ApplyHue = () =>
        {
            // 0~5
            // 0 -> 5
            // 1 -> 4
            // 2 -> 3
            // 3 -> 2
            // 4 -> 1
            // 5 -> 0
            var tempColors = new Color[] {
            Color.magenta,
            Color.blue,
            Color.cyan,
            Color.green,
            Color.yellow,
            Color.red,
        };

            int i0 = Math.Clamp (Mathf.FloorToInt(Hue * 6f) , 0 , 5);
            int i1 = (i0 + 1) % 6;
            float lerpAmount = Hue * 6f - i0;

            var resultColor = Color.Lerp(tempColors[i0], tempColors[i1], lerpAmount);
            satColors[3] = resultColor;
            resetSatTex();

        };

        ApplySaturation = () =>
        {
            var sv = new Vector2(Saturation, Value);
            var isv = new Vector2(1 - sv.x, 1 - sv.y);
            var c0 = isv.x * isv.y * satColors[0];
            var c1 = sv.x * isv.y * satColors[1];
            var c2 = isv.x * sv.y * satColors[2];
            var c3 = sv.x * sv.y * satColors[3];
            var resultColor = c0 + c1 + c2 + c3;
            _color = resultColor;

            ResultTex = new Texture2D(1, 1);
            ResultTex.SetPixel(0, 0, resultColor);
            ResultTex.Apply();

            resetSatTex();

        };

        ApplyHue();
        ApplySaturation();
        HueTex = interpolatedHueTex;
        SatTex = satTex;

    }


}
