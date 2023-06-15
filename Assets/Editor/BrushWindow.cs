using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.Rendering;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEditor.Graphs;

public class BrushWindow : EditorWindow
{

    private bool[] _showBrushTabs = new bool[4];
    int _selectedTabCnt = 0;
    private GUIStyle _tabBtnStyle = null;

    public static Rect s_satRect;
    public static Rect s_knobSVRect = new Rect(332 , 258 ,10, 10);
    public static Rect s_hueRect;
    public static Rect s_knobHueRect = new Rect(343 ,458 ,10,10);
    public static Action _update;

    private string _brushScale = "0.5f";
    private string _brushDist = "0.5f";

    [MenuItem("Photoshop/Brush")]
    public static void ShowWindow()
    {
       GetWindow<BrushWindow>("Brush");
    }

    private void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        Initialize();
    }
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        Initialize();
    }
    private void Initialize()
    {
        Action dragH = null;
        Action dragSV = null;
        Action idle = () =>
        {
            Event e = Event.current;

            if (e.type == EventType.MouseDown)
            {
                if (e.button == 0)
                {
                    if (s_hueRect.Contains(e.mousePosition))
                    {
                        DragHue();
                        _update = dragH;
                    }
                    if (s_satRect.Contains(e.mousePosition))
                    {
                        DragSV();
                        _update = dragSV;
                    }
                }
            }
        };

        dragH = () =>
        {
            Event e = Event.current;

            if (e.type == EventType.MouseDrag)
                DragHue();

            else if (e.type == EventType.MouseUp)
            {
                _update = idle;
                e.Use();
            }
        };

        dragSV = () =>
        {
            Event e = Event.current;

            if (e.type == EventType.MouseDrag)
                DragSV();

            else if (e.type == EventType.MouseUp)
            {
                _update = idle;
                e.Use();
            }
        };

        _update = idle;
    }
    public void OnEnable()
    {
        ColorPaletteGUI.Color = Color.red;
        Initialize();
    }

    public void DragHue()
    {
        Vector2 mousePos = Event.current.mousePosition;
        float y = Mathf.Clamp(mousePos.y - s_hueRect.y, 0, s_hueRect.height);
        ColorPaletteGUI.Hue = y / s_hueRect.height;

        ColorPaletteGUI.ApplyHue();
        ColorPaletteGUI.ApplySaturation();

        s_knobHueRect = new Rect(s_hueRect.x - 34 + s_hueRect.width, s_hueRect.y - 5 + s_hueRect.height * ColorPaletteGUI.Hue, 10, 10);
        Debug.Log("Hue Pos : " + s_knobHueRect.x + " , " + s_knobHueRect.y);

        Repaint();
        Event.current.Use();
    }

    public void DragSV()
    {
        Vector2 mousePos = Event.current.mousePosition;
        float x = Mathf.Clamp(mousePos.x - s_satRect.x, -5, s_satRect.width - 5);
        float y = Mathf.Clamp(mousePos.y - s_satRect.y, -5, s_satRect.height - 5);
        ColorPaletteGUI.Saturation = x / s_satRect.width;
        ColorPaletteGUI.Value = 1 - (y / s_satRect.height);
        ColorPaletteGUI.ApplySaturation();

        s_knobSVRect = new Rect(s_satRect.x + s_satRect.width * ColorPaletteGUI.Saturation, s_satRect.y + s_satRect.height * (1 - ColorPaletteGUI.Value), 10, 10);
        Debug.Log("SV Pos : " + s_knobSVRect.x + " , " + s_knobSVRect.y);

        Repaint();
        Event.current.Use();
    }

    void OptionRefresh(int id , string name ,  ref float tabBoxPosX, Action act)
    {  
        if (_tabBtnStyle == null)
            _tabBtnStyle = EditorHelper.ToggleTabStyle();

        _showBrushTabs[id] = GUI.Toggle(new Rect(tabBoxPosX, 10, 40, 40), _showBrushTabs[id] , name, _tabBtnStyle);

        _selectedTabCnt = _showBrushTabs.Count(x => x == true);

        if (_showBrushTabs[id])
            act?.Invoke();

        tabBoxPosX += 40f;
    }

    public void OnGUI()
    {
        float tabBoxPosX = 0;

        OptionRefresh((int)E_BrushOption.Type , "종류" , ref tabBoxPosX, ()=>
        {
            GUILayout.Space(60);
            BrushInfo.DrawGridBrush(position.x, new Vector2(60, 60));
        });

        OptionRefresh((int)E_BrushOption.Setting, "설정", ref tabBoxPosX , ()=>
        {
            float space = _showBrushTabs[(int)E_BrushOption.Type] ? GUILayoutUtility.GetLastRect().y + 20f : 60;
            GUILayout.Space(space);
            SetBrushScaleGUI();
            SetBrushDistanceGUI();     
        });


        //   CustomBrushEditor.ED.CubeColor = Utils.EditPropertyWithUndo("색상", CustomBrushEditor.ED.CubeColor, newColor => CustomBrushEditor.ED.CubeColor = newColor, (label, value) => EditorGUILayout.ColorField(label, value), CustomBrushEditor.ED);

        OptionRefresh((int)E_BrushOption.Color, "색상", ref tabBoxPosX , () => 
        {
            float space = _selectedTabCnt > 1 ? GUILayoutUtility.GetLastRect().y + 20f : 60;
            GUILayout.Space(space);
            SetBrushColorGUI();
        });

        GUI.DrawTexture(new Rect(tabBoxPosX, 13, position.width * 5, 1), EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, false, 1f, new Color32(29, 29, 29, 255), 0, 0);
        GUI.DrawTexture(new Rect(tabBoxPosX, 14f, position.width * 5, 36), EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, false, 1f, new Color32(41, 41, 41, 255), 0, 0);
        GUI.DrawTexture(new Rect(tabBoxPosX, 50, position.width * 5, 1), EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, false, 1f, new Color32(29, 29, 29, 255), 0, 0);

    }
    private void SetBrushScaleGUI()
    {
        EditorGUILayout.BeginHorizontal(GUI.skin.box);

        GUI.Label(EditorHelper.GetRect(60,60 , EditorHelper.BrushScale(BrushInfo.ED.BrushSize)) ,  "●", EditorHelper.BrushScale(BrushInfo.ED.BrushSize));
        GUILayout.Space(20);
        EditorGUILayout.BeginVertical();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("크기 : ");
        GUILayout.Space(-20);

        string scaleStr = BrushInfo.ED.BrushSize.ToString("F2");

        scaleStr = EditorGUILayout.TextField(BrushInfo.ED.BrushSize.ToString("F2"), GUILayout.MaxWidth(170));

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(20);
        Rect sliderRect = EditorHelper.GetRect(300, EditorGUIUtility.singleLineHeight);
        BrushInfo.ED.BrushSize = Utils.EditPropertyWithUndo("크기", BrushInfo.ED.BrushSize, newSize => BrushInfo.ED.BrushSize = newSize, (label, value) => GUI.HorizontalSlider(sliderRect, value, 0.1f, 1f), BrushInfo.ED);

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void SetBrushDistanceGUI()
    {
        EditorGUILayout.BeginHorizontal(GUI.skin.box);

        float space = (int)(BrushInfo.ED.PlacementDistance * 7);

        string arrowText = "●" + new string(' ', (int)space) + "●";
        GUI.Label(EditorHelper.GetRect(60,60 , EditorHelper.BrushDistance()) , arrowText, EditorHelper.BrushDistance());
        GUILayout.Space(20);

        EditorGUILayout.BeginVertical();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("간격 : ");
        GUILayout.Space(-20);
        _brushDist = EditorGUILayout.TextField(BrushInfo.ED.PlacementDistance.ToString("F2"), GUILayout.MaxWidth(170));
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(20);
        Rect sliderRect = EditorHelper.GetRect(300, EditorGUIUtility.singleLineHeight);
        BrushInfo.ED.PlacementDistance = Utils.EditPropertyWithUndo("간격", BrushInfo.ED.PlacementDistance , newDistance => BrushInfo.ED.PlacementDistance = newDistance, (label, value) => GUI.HorizontalSlider(sliderRect, value, 0.1f, 1f), BrushInfo.ED);

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }
    private void SetBrushColorGUI()
    {
        EditorGUILayout.BeginHorizontal(GUI.skin.box);

        Rect paletteRect = EditorHelper.GetRect(60, 60);
        GUI.DrawTexture(paletteRect, Resources.Load<Texture2D>("Textures/Icon/ColorPaletteIcon"), ScaleMode.StretchToFill);
        paletteRect.x += 5; paletteRect.y += 5; paletteRect.width = 50; paletteRect.height = 50;
        GUI.DrawTexture(paletteRect, ColorPaletteGUI.ResultTex, ScaleMode.StretchToFill);
        GUILayout.Space(70);

        s_satRect = EditorHelper.GetRect(200, 200);

        GUI.DrawTexture(s_satRect, ColorPaletteGUI.SatTex, ScaleMode.StretchToFill);
        GUI.DrawTexture(s_knobSVRect, Resources.Load<Texture2D>("Textures/Icon/Knob_01"), ScaleMode.StretchToFill);
       
        GUILayout.Space(20);

        s_hueRect = EditorHelper.GetRect(20, 200);

        GUI.DrawTexture(s_hueRect, ColorPaletteGUI.HueTex, ScaleMode.StretchToFill);
        GUI.DrawTexture(s_knobHueRect, Resources.Load<Texture2D>("Textures/Icon/Knob_02"), ScaleMode.StretchToFill);

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        _update?.Invoke();
    }
}
