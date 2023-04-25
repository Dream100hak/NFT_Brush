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
        // GUI 색상 변경
  
        GUIStyle toggleBoxStyle = new GUIStyle(GUI.skin.box);
        toggleBoxStyle.normal.background = EditorGUIUtility.whiteTexture;
        toggleBoxStyle.normal.textColor = Color.white;
        toggleBoxStyle.border = new RectOffset(4, 4, 4, 4); // 원하는 테두리 크기로 조정할 수 있습니다.

        GUI.color = new Color(0.1f, 0.1f, 0.1f); // 회색 계열로 변경

        return toggleBoxStyle;
    }

    public static GUIStyle SetLableStyle(Color color)
    {
        // GUI 색상 변경
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
