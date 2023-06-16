using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor.Graphs;
using System;
using static UnityEditor.Progress;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EditorHelper 
{
    public static Color Gray01 = new Color32(100, 100, 100, 255);
    public static Color Sky = new Color32(51, 243, 255, 255);

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

    public static GUIStyle BrushColor(Color color)
    {

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.contentOffset = new Vector2(1, 1);

        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();

        labelStyle.normal.background = texture;

        return labelStyle;

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

#if UNITY_EDITOR
    public static GUIStyle BrushTypeBtnStyle(E_BrushType brushType)
    {
        // GUI 스타일 객체 생성
        GUIStyle toggleStyle = new GUIStyle(GUI.skin.toggle);
        toggleStyle.fixedWidth = 40;
        toggleStyle.fixedHeight = 40;

        switch (brushType)
        {
            case E_BrushType.Box:
                toggleStyle.normal.background = Resources.Load<Texture2D>("Textures/Icon/BrushSquareIcon_Normal");
                toggleStyle.onNormal.background = Resources.Load<Texture2D>("Textures/Icon/BrushSquareIcon_OnNormal");
                break;
            case E_BrushType.One:
                toggleStyle.normal.background = Resources.Load<Texture2D>("Textures/Icon/BrushOneIcon_Normal");
                toggleStyle.onNormal.background = Resources.Load<Texture2D>("Textures/Icon/BrushOneIcon_OnNormal");
                break;
        }

        toggleStyle.border = new RectOffset(4, 4, 4, 4);

        return toggleStyle;
    }
    public static GUIStyle ToggleTabStyle()
    {
        // GUI 스타일 객체 생성
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
    public static GUIStyle ToggleBoxStyle()
    {
        // GUI 색상 변경
        GUIStyle toggleBoxStyle = new GUIStyle(GUI.skin.box);
        toggleBoxStyle.normal.background = EditorGUIUtility.whiteTexture;
        toggleBoxStyle.normal.textColor = Color.white;
        toggleBoxStyle.border = new RectOffset(4, 4, 4, 4); // 원하는 테두리 크기로 조정할 수 있습니다.

        GUI.color = new Color(0.1f, 0.1f, 0.1f); // 회색 계열로 변경

        return toggleBoxStyle;
    }

    public static GUIStyle CustomLabelStyle(Color color)
    {
        // GUI 색상 변경
        GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
        labelStyle.normal.textColor = color;



        return labelStyle;
    }
    public static GUIStyle WhiteLabelStyle()
    {
        return CustomLabelStyle(Color.white);
    }

    public static GUIStyle WhiteSkinBoxStyle()
    {

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = EditorGUIUtility.whiteTexture;
 
        return boxStyle;
    }

    public static void DrawSeparatorLine(float space, float height , Color color)
    {
        GUILayout.Space(space);

        GUIStyle separatorStyle = new GUIStyle(GUI.skin.box);
        separatorStyle.normal.background = EditorGUIUtility.whiteTexture;

        Color originalColor = GUI.color;
        GUI.color = color;
        GUILayout.Box("", separatorStyle, GUILayout.ExpandWidth(true), GUILayout.Height(height));
        GUI.color = originalColor;
 
    }
#endif
}
