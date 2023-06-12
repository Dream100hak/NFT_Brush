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

public class BrushWindow : EditorWindow
{
    private bool _showBrushSettings = true;
    private bool _showBrushEffect = true;
    private bool _showBrushType = true;
    private bool _showBrushColor = true;

    private bool _brushOne = false;
    private bool _brushSquare = false;

    private GUIStyle _tabBtnStyle = null;

    private string _brushScaleValue = "0.5f";
    private string _brushDistance = "0.5f";

    private Rect _satRect;
    private Rect _knobSVRect = new Rect(332 , 258 ,10, 10);
    private Rect _hueRect;
    private Rect _knobHueRect = new Rect(343 ,458 ,10,10);
    private Action _update;

    [MenuItem("Photoshop/Brush")]
    public static void ShowWindow()
    {
       GetWindow<BrushWindow>("Brush");
    }
    public void OnEnable()
    {

        Event e = Event.current;

        Action dragH = null;
        Action dragSV = null;
        Action idle = () =>
        {
            if (e.type == EventType.MouseDown)
            {
                if (e.button == 0)
                {
                    if (_hueRect.Contains(e.mousePosition))
                    {
                        DragHue();
                        _update = dragH;
                    }
                    if (_satRect.Contains(e.mousePosition))
                    {
                        DragSV();
                        _update = dragSV;

                    }
                }
            }
        };

        dragH = () =>
        {
            if(e.type == EventType.MouseDrag)
                DragHue();

            else  if (e.type == EventType.MouseUp)
            {
                _update = idle;
                e.Use();
            }
        };

        dragSV = () =>
        {
            if (e.type == EventType.MouseDrag)
                DragSV();

            else if (e.type == EventType.MouseUp)
            {
                _update = idle;
                e.Use();
            }
        };

        _update = idle;

        ColorPaletteGUI.Color = Color.red;
    }
    void DragHue()
    {
        Vector2 mousePos = Event.current.mousePosition;
        float y = Mathf.Clamp(mousePos.y - _hueRect.y, 0, _hueRect.height);
        ColorPaletteGUI.Hue = y / _hueRect.height;

        ColorPaletteGUI.ApplyHue();
        ColorPaletteGUI.ApplySaturation();

        _knobHueRect = new Rect(_hueRect.x - 34 + _hueRect.width, _hueRect.y - 5 + _hueRect.height * ColorPaletteGUI.Hue, 10, 10);
        Debug.Log("Hue Pos : " + _knobHueRect.x + " , " + _knobHueRect.y);

        Repaint();
        Event.current.Use();
    }

    void DragSV()
    {
        Vector2 mousePos = Event.current.mousePosition;
        float x = Mathf.Clamp(mousePos.x - _satRect.x, -5, _satRect.width - 5);
        float y = Mathf.Clamp(mousePos.y - _satRect.y, -5, _satRect.height - 5);
        ColorPaletteGUI.Saturation = x / _satRect.width;
        ColorPaletteGUI.Value = 1 - (y / _satRect.height);
        ColorPaletteGUI.ApplySaturation();

        _knobSVRect = new Rect(_satRect.x + _satRect.width * ColorPaletteGUI.Saturation, _satRect.y + _satRect.height * (1 - ColorPaletteGUI.Value), 10, 10);
        Debug.Log("SV Pos : " + _knobSVRect.x + " , " + _knobSVRect.y);

        Repaint();
        Event.current.Use();
    }
    public void OnGUI()
    {
        float tabBoxPosX = 0;
        if (_tabBtnStyle == null)
            _tabBtnStyle = CustomLayerStyle.ToggleTabStyle();

        _showBrushType = GUI.Toggle(new Rect(tabBoxPosX, 10, 40, 40), _showBrushType, "종류", _tabBtnStyle);
        tabBoxPosX += 40f;

        if (_showBrushType)
        {
            GUILayout.Space(60);
            GUILayout.BeginHorizontal(GUI.skin.box);

            _brushOne = GUILayout.Toggle(_brushOne, GUIContent.none, CustomLayerStyle.BrushTypeBtnStyle(E_BrushType.One));
            _brushSquare = GUILayout.Toggle(_brushSquare, GUIContent.none, CustomLayerStyle.BrushTypeBtnStyle(E_BrushType.Square));

            GUILayout.Space(5);
            GUILayout.BeginVertical(GUILayout.Height(40));
            GUILayout.FlexibleSpace();

            GUILayout.Label("<-- Select Brush Mode One or Square", GUI.skin.label); // 텍스트를 중앙에 배치

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();

            //CustomBrushEditor.ED.SnowEnabled = Utils.EditPropertyWithUndo("눈", CustomBrushEditor.ED.SnowEnabled, enbled => CustomBrushEditor.ED.SnowEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), CustomBrushEditor.ED, 130f);
            //if (CustomBrushEditor.ED.SnowEnabled)
            //{
            //    CustomBrushEditor.ED.Snow_SwayIntensity = Utils.EditPropertyWithUndo("강도", CustomBrushEditor.ED.Snow_SwayIntensity, speed => CustomBrushEditor.ED.Snow_SwayIntensity = speed, (label, value) => EditorGUILayout.FloatField(label, value), CustomBrushEditor.ED, 120f);
            //    CustomBrushEditor.ED.Snow_SwayAmount = Utils.EditPropertyWithUndo("흔들림", CustomBrushEditor.ED.Snow_SwayAmount, speed => CustomBrushEditor.ED.Snow_SwayAmount = speed, (label, value) => EditorGUILayout.FloatField(label, value), CustomBrushEditor.ED, 110f);
            //}
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        _showBrushSettings = GUI.Toggle(new Rect(tabBoxPosX, 10, 40, 40), _showBrushSettings, "설정", _tabBtnStyle);
        tabBoxPosX += 40f;

        if (_showBrushSettings)
        {
            float space = _showBrushType ? 10 : 60;
            GUILayout.Space(space);
            _showBrushEffect = false;

            SetBrushScaleGUI();
            SetBrushDistanceGUI();

            // 기본 옵션 [ 사이즈 / 간격 / 색상]
            //   CustomBrushEditor.ED.CubeSize = Utils.EditPropertyWithUndo("크기", CustomBrushEditor.ED.CubeSize, newSize => CustomBrushEditor.ED.CubeSize = newSize, (label, value) => EditorGUILayout.Slider(label, value, 0.1f, 2f), CustomBrushEditor.ED);
            //   CustomBrushEditor.ED.PlacementDistance = Utils.EditPropertyWithUndo("간격", CustomBrushEditor.ED.PlacementDistance, newDistance => CustomBrushEditor.ED.PlacementDistance = newDistance, (label, value) => EditorGUILayout.Slider(label, value, 0.1f, 1f), CustomBrushEditor.ED);
            //   CustomBrushEditor.ED.CubeColor = Utils.EditPropertyWithUndo("색상", CustomBrushEditor.ED.CubeColor, newColor => CustomBrushEditor.ED.CubeColor = newColor, (label, value) => EditorGUILayout.ColorField(label, value), CustomBrushEditor.ED);
        }

        _showBrushColor = GUI.Toggle(new Rect(tabBoxPosX, 10, 40, 40), _showBrushColor, "색상", _tabBtnStyle);
        tabBoxPosX += 40f;

        if(_showBrushColor)
        { 
            SetBrushColorGUI();
        }
        _showBrushEffect = GUI.Toggle(new Rect(tabBoxPosX, 10, 40, 40), _showBrushEffect, "효과", _tabBtnStyle);    
        tabBoxPosX += 40f;

        GUI.DrawTexture(new Rect(tabBoxPosX, 13, position.width * 5, 1), EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, false, 1f, new Color32(29, 29, 29, 255), 0, 0);
        GUI.DrawTexture(new Rect(tabBoxPosX, 14f, position.width * 5, 36), EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, false, 1f, new Color32(41, 41, 41, 255), 0, 0);
        GUI.DrawTexture(new Rect(tabBoxPosX, 50, position.width * 5, 1), EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, false, 1f, new Color32(29, 29, 29, 255), 0, 0);

        if (_showBrushEffect)
        {
            _showBrushSettings = false;
            GUILayout.Space(60);
            Color originalColor = GUI.color;

            GUILayout.BeginHorizontal();
            CustomBrushEditor.ED.RotatorEnabled = Utils.EditPropertyWithUndo("회전", CustomBrushEditor.ED.RotatorEnabled, enbled => CustomBrushEditor.ED.RotatorEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), CustomBrushEditor.ED, 120f);
            GUILayout.EndHorizontal();

            CustomLayerStyle.DrawSeparatorLine(5, 1.7f, new Color(0.3f, 0.3f, 0.3f, 0.5f));

            if (CustomBrushEditor.ED.RotatorEnabled)
            {
                EditorGUILayout.BeginVertical(CustomLayerStyle.ToggleBoxStyle());

                CustomBrushEditor.ED.Random_RotSpeed = Utils.EditPropertyWithUndo("속도", CustomBrushEditor.ED.Random_RotSpeed, speed => CustomBrushEditor.ED.Random_RotSpeed = speed, (label, value) => EditorGUILayout.FloatField(label, value), CustomBrushEditor.ED, 120f);

                EditorGUILayout.EndVertical();
                CustomLayerStyle.DrawSeparatorLine(0, 1.7f, new Color(0.3f, 0.3f, 0.3f, 0.5f));
            }

            GUI.color = originalColor;
            GUILayout.Space(5);

            CustomBrushEditor.ED.MoverEnabled = Utils.EditPropertyWithUndo("이동", CustomBrushEditor.ED.MoverEnabled, enbled => CustomBrushEditor.ED.MoverEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), CustomBrushEditor.ED, 120f);

            CustomLayerStyle.DrawSeparatorLine(5, 1.7f, new Color(0.3f, 0.3f, 0.3f, 0.5f));

            if (CustomBrushEditor.ED.MoverEnabled)
            {
                bool prevStraightEnbled = CustomBrushEditor.ED.StraightEnabled;
                bool prevBlackholeEnbled = CustomBrushEditor.ED.BlackholeEnabled;
                bool prevSnowEnabled = CustomBrushEditor.ED.SnowEnabled;

                GUILayout.BeginHorizontal(GUI.skin.box);

                EditorGUILayout.BeginVertical(CustomLayerStyle.ToggleBoxStyle());
                CustomBrushEditor.ED.StraightEnabled = Utils.EditPropertyWithUndo("직선", CustomBrushEditor.ED.StraightEnabled, enbled => CustomBrushEditor.ED.StraightEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), CustomBrushEditor.ED, 120f);
                if (CustomBrushEditor.ED.StraightEnabled)
                {
                    CustomBrushEditor.ED.Straight_MoveSpeed = Utils.EditPropertyWithUndo("속도", CustomBrushEditor.ED.Straight_MoveSpeed, speed => CustomBrushEditor.ED.Straight_MoveSpeed = speed, (label, value) => EditorGUILayout.FloatField(label, value), CustomBrushEditor.ED, 120f);
                    CustomBrushEditor.ED.Straight_MoveDirection = Utils.EditPropertyWithUndo("방향", CustomBrushEditor.ED.Straight_MoveDirection, direction => CustomBrushEditor.ED.Straight_MoveDirection = direction, (label, value) => (E_Direction)EditorGUILayout.EnumPopup(label, (E_Direction)value), CustomBrushEditor.ED, 120f);
                }
                EditorGUILayout.EndVertical();
                GUILayout.Space(5);

                EditorGUILayout.BeginVertical(CustomLayerStyle.ToggleBoxStyle());
                CustomBrushEditor.ED.BlackholeEnabled = Utils.EditPropertyWithUndo("블랙홀", CustomBrushEditor.ED.BlackholeEnabled, enbled => CustomBrushEditor.ED.BlackholeEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), CustomBrushEditor.ED, 110f);
                if (CustomBrushEditor.ED.BlackholeEnabled)
                {
                    CustomBrushEditor.ED.Blackhole_AttractionForce = Utils.EditPropertyWithUndo("속도", CustomBrushEditor.ED.Blackhole_AttractionForce, speed => CustomBrushEditor.ED.Blackhole_AttractionForce = speed, (label, value) => EditorGUILayout.FloatField(label, value), CustomBrushEditor.ED, 110f);
                }
                EditorGUILayout.EndVertical();
                GUILayout.Space(5);

                EditorGUILayout.BeginVertical(CustomLayerStyle.ToggleBoxStyle());
                CustomBrushEditor.ED.SnowEnabled = Utils.EditPropertyWithUndo("눈", CustomBrushEditor.ED.SnowEnabled, enbled => CustomBrushEditor.ED.SnowEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), CustomBrushEditor.ED, 130f);
                if (CustomBrushEditor.ED.SnowEnabled)
                {
                    CustomBrushEditor.ED.Snow_SwayIntensity = Utils.EditPropertyWithUndo("강도", CustomBrushEditor.ED.Snow_SwayIntensity, speed => CustomBrushEditor.ED.Snow_SwayIntensity = speed, (label, value) => EditorGUILayout.FloatField(label, value), CustomBrushEditor.ED, 120f);
                    CustomBrushEditor.ED.Snow_SwayAmount = Utils.EditPropertyWithUndo("흔들림", CustomBrushEditor.ED.Snow_SwayAmount, speed => CustomBrushEditor.ED.Snow_SwayAmount = speed, (label, value) => EditorGUILayout.FloatField(label, value), CustomBrushEditor.ED, 110f);
                }
                EditorGUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUI.color = originalColor;

                CustomLayerStyle.DrawSeparatorLine(0, 1.7f, new Color(0.3f, 0.3f, 0.3f, 0.5f));

                CheckBrushEffectEnabled(prevStraightEnbled, prevBlackholeEnbled, prevSnowEnabled);

            }

            GUILayout.Space(5);

            CustomBrushEditor.ED.NatureEnabled = Utils.EditPropertyWithUndo("자연", CustomBrushEditor.ED.NatureEnabled, enbled => CustomBrushEditor.ED.NatureEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), CustomBrushEditor.ED, 120f);
            CustomLayerStyle.DrawSeparatorLine(5, 1.7f, new Color(0.3f, 0.3f, 0.3f, 0.5f));

            if (CustomBrushEditor.ED.NatureEnabled)
            {
                GUILayout.BeginHorizontal(GUI.skin.box);

                EditorGUILayout.BeginVertical(CustomLayerStyle.ToggleBoxStyle());
                CustomBrushEditor.ED.SnowSpawnEnabled = Utils.EditPropertyWithUndo("눈", CustomBrushEditor.ED.SnowSpawnEnabled, enbled => CustomBrushEditor.ED.SnowSpawnEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), CustomBrushEditor.ED, 120f);
                if (CustomBrushEditor.ED.SnowSpawnEnabled)
                {
                    CustomBrushEditor.ED.SnowSpawn_SwayIntensity = Utils.EditPropertyWithUndo("강도", CustomBrushEditor.ED.SnowSpawn_SwayIntensity, speed => CustomBrushEditor.ED.SnowSpawn_SwayIntensity = speed, (label, value) => EditorGUILayout.FloatField(label, value), CustomBrushEditor.ED, 120f);
                    CustomBrushEditor.ED.SnowSpawn_SwayAmount = Utils.EditPropertyWithUndo("흔들림", CustomBrushEditor.ED.SnowSpawn_SwayAmount, speed => CustomBrushEditor.ED.SnowSpawn_SwayAmount = speed, (label, value) => EditorGUILayout.FloatField(label, value), CustomBrushEditor.ED, 110f);
                }
                EditorGUILayout.EndVertical();
                GUILayout.Space(10);

                GUILayout.EndHorizontal();
                GUI.color = originalColor;

                CustomLayerStyle.DrawSeparatorLine(0, 1.7f, new Color(0.3f, 0.3f, 0.3f, 0.5f));
            }
        }
    }

    private void CheckBrushEffectEnabled(bool prevStraightEnabled, bool prevBlackholeEnabled, bool prevSnowEnabled)
    {
        int cnt = 0;
        cnt = (CustomBrushEditor.ED.StraightEnabled) ? cnt + 1 : cnt;
        cnt = (CustomBrushEditor.ED.BlackholeEnabled) ? cnt + 1 : cnt;
        cnt = (CustomBrushEditor.ED.SnowEnabled) ? cnt + 1 : cnt;

        if(cnt > 1)
        {
            if (prevStraightEnabled)
                CustomBrushEditor.ED.StraightEnabled = false;
            else if(prevBlackholeEnabled)
                CustomBrushEditor.ED.BlackholeEnabled = false;
            else if(prevSnowEnabled)
                CustomBrushEditor.ED.SnowEnabled = false;
        }
    }

    private void SetBrushScaleGUI()
    {
        EditorGUILayout.BeginHorizontal(GUI.skin.box);

        GUI.Label(CustomLayerStyle.GetRect(60,60 , CustomLayerStyle.BrushScale(CustomBrushEditor.ED.CubeSize)) ,  "●", CustomLayerStyle.BrushScale(CustomBrushEditor.ED.CubeSize));
        GUILayout.Space(20);
        EditorGUILayout.BeginVertical();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("크기 : ");
        GUILayout.Space(-20);
        _brushScaleValue = EditorGUILayout.TextField(CustomBrushEditor.ED.CubeSize.ToString("F2"), GUILayout.MaxWidth(170));
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(20);
        Rect sliderRect = CustomLayerStyle.GetRect(300, EditorGUIUtility.singleLineHeight);
        CustomBrushEditor.ED.CubeSize = GUI.HorizontalSlider(sliderRect, CustomBrushEditor.ED.CubeSize, 0.1f, 1f);

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void SetBrushDistanceGUI()
    {
        EditorGUILayout.BeginHorizontal(GUI.skin.box);

        float space = (int)(CustomBrushEditor.ED.PlacementDistance * 7);

        string arrowText = "●" + new string(' ', (int)space) + "●";
        GUI.Label(CustomLayerStyle.GetRect(60,60 , CustomLayerStyle.BrushDistance()) , arrowText, CustomLayerStyle.BrushDistance());
        GUILayout.Space(20);

        EditorGUILayout.BeginVertical();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("간격 : ");
        GUILayout.Space(-20);
        _brushDistance = EditorGUILayout.TextField(CustomBrushEditor.ED.PlacementDistance.ToString("F2"), GUILayout.MaxWidth(170));
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(20);
        Rect sliderRect = CustomLayerStyle.GetRect(300, EditorGUIUtility.singleLineHeight);
        CustomBrushEditor.ED.PlacementDistance = GUI.HorizontalSlider(sliderRect, CustomBrushEditor.ED.PlacementDistance, 0.1f, 1f);

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }
    private void SetBrushColorGUI()
    {
        EditorGUILayout.BeginHorizontal(GUI.skin.box);

        Rect paletteRect = CustomLayerStyle.GetRect(60, 60);
        GUI.DrawTexture(paletteRect, Resources.Load<Texture2D>("Textures/Icon/ColorPaletteIcon"), ScaleMode.StretchToFill);
        paletteRect.x += 5; paletteRect.y += 5; paletteRect.width = 50; paletteRect.height = 50;
        GUI.DrawTexture(paletteRect, ColorPaletteGUI.ResultTex, ScaleMode.StretchToFill);
        GUILayout.Space(70);

        _satRect = CustomLayerStyle.GetRect(200, 200);

        GUI.DrawTexture(_satRect, ColorPaletteGUI.SatTex, ScaleMode.StretchToFill);
        GUI.DrawTexture(_knobSVRect, Resources.Load<Texture2D>("Textures/Icon/Knob_01"), ScaleMode.StretchToFill);
       
        GUILayout.Space(20);

        _hueRect = CustomLayerStyle.GetRect(20, 200);

        GUI.DrawTexture(_hueRect, ColorPaletteGUI.HueTex, ScaleMode.StretchToFill);
        GUI.DrawTexture(_knobHueRect, Resources.Load<Texture2D>("Textures/Icon/Knob_02"), ScaleMode.StretchToFill);

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        _update?.Invoke();
    }
}
