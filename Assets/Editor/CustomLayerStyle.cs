using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor.Graphs;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CustomLayerStyle 
{
#if UNITY_EDITOR
    public static GUIStyle SetBrushTabStyle()
    {
        GUI.color = Color.clear;
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.border = new RectOffset(0, 0, 1, 1); // ���ϴ� �׵θ� ũ��� ������ �� �ֽ��ϴ�.
        
        boxStyle.alignment = TextAnchor.MiddleCenter;

        return boxStyle;
    }
    public static GUIStyle SetToggleTabStyle()
    {
        // GUI ��Ÿ�� ��ü ����
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
    public static GUIStyle SetToggleBoxStyle()
    {
        // GUI ���� ����
        GUIStyle toggleBoxStyle = new GUIStyle(GUI.skin.box);
        toggleBoxStyle.normal.background = EditorGUIUtility.whiteTexture;
        toggleBoxStyle.normal.textColor = Color.white;
        toggleBoxStyle.border = new RectOffset(4, 4, 4, 4); // ���ϴ� �׵θ� ũ��� ������ �� �ֽ��ϴ�.

        GUI.color = new Color(0.1f, 0.1f, 0.1f); // ȸ�� �迭�� ����

        return toggleBoxStyle;
    }

    public static GUIStyle SetLableStyle(Color color)
    {
        // GUI ���� ����
        GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
        labelStyle.normal.textColor = color;

        return labelStyle;
    }
    public static GUIStyle SetWhilteLableStyle()
    {
        return SetLableStyle(Color.white);
    }

    public static GUIStyle SetWhiteSkinBoxStyle()
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
