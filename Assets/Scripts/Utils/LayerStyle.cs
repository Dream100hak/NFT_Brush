using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


public class LayerStyle 
{
#if UNITY_EDITOR
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
#endif
}
