using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EditorHelper 
{
    public static Color Gray01 = new Color32(100, 100, 100, 255);
    public static Color Sky = new Color32(51, 243, 255, 255);
#if UNITY_EDITOR
    public static void DrawGridBrushItems(int space, int itemCnt, Action<int> onDrawer)
    {
        GUILayout.BeginHorizontal();
        for (int i = 0; i < itemCnt; i++)
        {
            onDrawer(i);
            GUILayout.Space(space);
        }
        GUILayout.EndHorizontal();
    }
    public static Vector2 DrawGridPreviewCanvasItems(Vector2 scrollPos, int gapSpace, int itemCnt, float areaWidth, Vector2 slotSize, Action<int> onDrawer)
    {
        scrollPos = GUILayout.BeginScrollView(scrollPos , GUI.skin.window);
        {
            int horCnt = (int)(areaWidth / slotSize.x);
            if (horCnt <= 0)
            {
                horCnt = 1;
            }

            int verCnt = itemCnt / horCnt;

            if (itemCnt % horCnt > 0)
            {
                verCnt++;
            }

            if (verCnt <= 0)
            {
                verCnt = 1;
            }

            GUILayout.BeginVertical();
            {
                for (int i = 0; i < verCnt; i++)
                {
                    GUILayout.BeginHorizontal();
                    {
                        for (int j = 0; j < horCnt; j++)
                        {
                            int index = i * horCnt + j;
                            if (index >= itemCnt)
                            {
                                break;
                            }
                            onDrawer(index);
                            GUILayout.Space(gapSpace);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();

        }
        GUILayout.EndScrollView();

        return scrollPos;
    }

    public static Rect GetRect(float width, float height , GUIStyle customStyle = null)
    {
        if(customStyle == null)
            customStyle = GUIStyle.none;

        return GUILayoutUtility.GetRect(GUIContent.none, customStyle, GUILayout.Width(width), GUILayout.Height(height));
    }
    public static GUIStyle SelectedBrushButton(GameObject target , bool isSelected)
    {
        GUIStyle style = new GUIStyle(GUI.skin.button);
        style.normal.background = AssetPreview.GetAssetPreview(target);
        style.alignment = TextAnchor.MiddleCenter;
        style.contentOffset = new Vector2(0, 30);
        style.normal.textColor = isSelected ? Color.green : Color.white;
        style.fontStyle = FontStyle.Bold;
        
        return style;

    }
    public static GUIStyle BrushDistance()
    {
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.contentOffset = new Vector2(1, 1);
        labelStyle.normal.background = Resources.Load<Texture2D>("Textures/Icon/BrushScaleIcon");

        return labelStyle;
    }
    public static GUIStyle BrushScale(float value)
    {
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.contentOffset = new Vector2(1.04f, 1.04f);
        labelStyle.fontSize = (int)Mathf.Lerp(10f, 30f, value);
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.normal.background = Resources.Load<Texture2D>("Textures/Icon/BrushScaleIcon");

        return labelStyle;
    }
    public static GUIStyle ToggleTabStyle()
    {
        // GUI 胶鸥老 按眉 积己
        GUIStyle toggleStyle = new GUIStyle(GUI.skin.toggle);
        toggleStyle.fixedWidth = 40;
        toggleStyle.fixedHeight = 40;
        toggleStyle.alignment = TextAnchor.MiddleLeft;
        toggleStyle.contentOffset = new Vector2(-10, 0);
        toggleStyle.fontStyle = FontStyle.Normal;
        toggleStyle.normal.background = Resources.Load<Texture2D>("Textures/Icon/TabBlack");
        toggleStyle.onNormal.background = Resources.Load<Texture2D>("Textures/Icon/Tab");

        return toggleStyle;
    }
    public static GUIStyle WhiteSkinBoxStyle()
    {
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = EditorGUIUtility.whiteTexture;
        return boxStyle;
    }
    public static void CanvasInfoLabel(string text , float width , float height , TextAnchor anchor = TextAnchor.MiddleCenter)
    {
        var tex2D = Resources.Load<Texture2D>("Textures/Icon/CanvasInfoTab");
        GUIContent content = new GUIContent(text);
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.alignment = anchor;

        if(anchor == TextAnchor.MiddleLeft)
            style.contentOffset = new Vector2(10, 0);
        else if(anchor == TextAnchor.MiddleCenter)
            style.contentOffset = new Vector2(0, 0);
        style.normal.background = tex2D;
        style.normal.textColor = Color.white;

        GUILayout.Box(content, style, GUILayout.Width(width), GUILayout.Height(height));

    }
    public static void DrawSeparatorHeightLine(float space, float height , Color color, bool expand = true)
    {
        GUILayout.Space(space);
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.normal.background = EditorGUIUtility.whiteTexture;

        Color originalColor = GUI.color;
        GUI.color = color;
        GUILayout.Box("", style, GUILayout.ExpandWidth(expand), GUILayout.Height(height));
        GUI.color = originalColor;
 
    }
    public static void DrawCenterLabel(GUIContent content, Color color, int size, FontStyle style)
    {
        var guiStyle = new GUIStyle();
        guiStyle.fontSize = size;
        guiStyle.fontStyle = style;
        guiStyle.normal.textColor = color;

        guiStyle.padding.top = 10;
        guiStyle.padding.bottom = 10;

        GUILayout.BeginHorizontal();
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label(content, guiStyle);
            GUILayout.FlexibleSpace();
        }
        GUILayout.EndHorizontal();

    }
    public static GUIStyle DrawCreateCanvasButton()
    {
        GUIStyle style = new GUIStyle(GUI.skin.button);
        style.normal.textColor = Color.white;
        style.fontStyle = FontStyle.Bold;
        style.normal.background = MakeColorTexture(2, 2, new Color(0.11f, 0.514f, 0.902f, 1));
        style.border = new RectOffset(3, 3, 3, 3);
        style.margin = new RectOffset(10, 10, 10, 10);
        return style;
    }

    public static  Texture2D MakeColorTexture(int width, int height, Color color)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = color;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();

        return result;
    }

    public static GUIStyle NotSelectedLayerLabel()
    {
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.font = Resources.Load<Font>("Fonts/The Jamsil 3 Regular");
        style.alignment = TextAnchor.MiddleCenter;

        return style;
    }
    public static GUIStyle PreviewCanvasLabel(Color color)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.font = Resources.Load<Font>("Fonts/The Jamsil 3 Regular");
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = color;

        return style;
    }
    public static GUIContent GetIcon(string name)
    {
        return EditorGUIUtility.IconContent(name);
    }
    public static GUIContent GetTrIcon(string name , string tooltip = null)
    {
        return EditorGUIUtility.TrIconContent(name , tooltip);
    }
#endif
}
