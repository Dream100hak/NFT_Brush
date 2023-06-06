using System;
using UnityEngine;


public class ColorPaletteGUI : MonoBehaviour
{ 
    private static Color _color = Color.red;
    public static Color Color { get { return _color; } set { Init(value); } }
    private static  Action<Color> _onValueChange;
    private static Action _update;

    public static Texture2D _hueTex;
    public static Texture2D _satTex;
    public static Texture2D _resultTex;
    public static void RGBToHSV(Color color, out float h, out float s, out float v)
    {
        float cmin = Mathf.Min(color.r, color.g, color.b);
        float cmax = Mathf.Max(color.r, color.g, color.b);
        float d = cmax - cmin;
        if (d == 0)
            h = 0;
        else if (cmax == color.r)
            h = Mathf.Repeat((color.g - color.b) / d, 6);
        else if (cmax == color.g)
            h = (color.b - color.r) / d + 2;
        else
            h = (color.r - color.g) / d + 4;

        s = cmax == 0 ? 0 : d / cmax;
        v = cmax;
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
            hueTex.SetPixel(0, i, hueColors[i % 6]);

        hueTex.Apply();

        Texture2D satTex = new Texture2D(2, 2);
        Action resetSatTex = () =>
        {
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 2; j++)
                    satTex.SetPixel(j, i, satColors[j + i * 2]);

            satTex.Apply();
            _satTex = satTex;
        };

        float hue, saturation, value;
        RGBToHSV(inputColor, out hue, out saturation, out value);

        Action applyHue = () =>
        {
            int i0 = Mathf.Clamp((int)hue, 0, 5);
            int i1 = (i0 + 1) % 6;
            var resultColor = Color.Lerp(hueColors[i0], hueColors[i1], hue - i0);
            satColors[3] = resultColor;
            resetSatTex();
        };
        Action applySaturation = () =>
        {
            Vector2 sv = new Vector2(saturation, value);
            Vector2 isv = new Vector2(1 - sv.x, 1 - sv.y);
            var c0 = isv.x * isv.y * satColors[0];
            var c1 = sv.x * isv.y * satColors[1];
            var c2 = isv.x * sv.y * satColors[2];
            var c3 = sv.x * sv.y * satColors[3];
            var resultColor = c0 + c1 + c2 + c3;

            if (_color != resultColor)
            {
                if (_onValueChange != null)
                    _onValueChange(resultColor);
                _color = resultColor;
            }
        };

        applyHue();
        applySaturation();
        _hueTex = hueTex;
    }


}
