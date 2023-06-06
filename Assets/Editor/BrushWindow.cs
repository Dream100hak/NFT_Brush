using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.Rendering;

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
    private string _brushDistance  = "0.5f";

    [MenuItem("Photoshop/Brush")]
    public static void ShowWindow()
    {
       GetWindow<BrushWindow>("Brush");
    }
    public void OnEnable()
    {
        ColorPaletteGUI.Color = Color.red;
    }

    public void OnGUI()
    {
        float tabBoxPosX = 0;
        if (_tabBtnStyle == null)
            _tabBtnStyle = CustomLayerStyle.ToggleTabStyle();

        _showBrushType = GUI.Toggle(new Rect(tabBoxPosX, 10, 40, 40), _showBrushType, "����", _tabBtnStyle);
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

            GUILayout.Label("<-- Select Brush Mode One or Square", GUI.skin.label); // �ؽ�Ʈ�� �߾ӿ� ��ġ

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();

            //CustomBrushEditor.ED.SnowEnabled = Utils.EditPropertyWithUndo("��", CustomBrushEditor.ED.SnowEnabled, enbled => CustomBrushEditor.ED.SnowEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), CustomBrushEditor.ED, 130f);
            //if (CustomBrushEditor.ED.SnowEnabled)
            //{
            //    CustomBrushEditor.ED.Snow_SwayIntensity = Utils.EditPropertyWithUndo("����", CustomBrushEditor.ED.Snow_SwayIntensity, speed => CustomBrushEditor.ED.Snow_SwayIntensity = speed, (label, value) => EditorGUILayout.FloatField(label, value), CustomBrushEditor.ED, 120f);
            //    CustomBrushEditor.ED.Snow_SwayAmount = Utils.EditPropertyWithUndo("��鸲", CustomBrushEditor.ED.Snow_SwayAmount, speed => CustomBrushEditor.ED.Snow_SwayAmount = speed, (label, value) => EditorGUILayout.FloatField(label, value), CustomBrushEditor.ED, 110f);
            //}
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        _showBrushSettings = GUI.Toggle(new Rect(tabBoxPosX, 10, 40, 40), _showBrushSettings, "����", _tabBtnStyle);
        tabBoxPosX += 40f;

        if (_showBrushSettings)
        {
            float space = _showBrushType ? 10 : 60;
            GUILayout.Space(space);
            _showBrushEffect = false;

            SetBrushScaleGUI();
            SetBrushDistanceGUI();

            // �⺻ �ɼ� [ ������ / ���� / ����]
            //   CustomBrushEditor.ED.CubeSize = Utils.EditPropertyWithUndo("ũ��", CustomBrushEditor.ED.CubeSize, newSize => CustomBrushEditor.ED.CubeSize = newSize, (label, value) => EditorGUILayout.Slider(label, value, 0.1f, 2f), CustomBrushEditor.ED);
            //   CustomBrushEditor.ED.PlacementDistance = Utils.EditPropertyWithUndo("����", CustomBrushEditor.ED.PlacementDistance, newDistance => CustomBrushEditor.ED.PlacementDistance = newDistance, (label, value) => EditorGUILayout.Slider(label, value, 0.1f, 1f), CustomBrushEditor.ED);
            //   CustomBrushEditor.ED.CubeColor = Utils.EditPropertyWithUndo("����", CustomBrushEditor.ED.CubeColor, newColor => CustomBrushEditor.ED.CubeColor = newColor, (label, value) => EditorGUILayout.ColorField(label, value), CustomBrushEditor.ED);

        }



        _showBrushColor = GUI.Toggle(new Rect(tabBoxPosX, 10, 40, 40), _showBrushColor, "����", _tabBtnStyle);
        tabBoxPosX += 40f;

        if(_showBrushColor)
        {
            SetBrushColorGUI();
        }


        _showBrushEffect = GUI.Toggle(new Rect(tabBoxPosX, 10, 40, 40), _showBrushEffect, "ȿ��", _tabBtnStyle);    
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
            CustomBrushEditor.ED.RotatorEnabled = Utils.EditPropertyWithUndo("ȸ��", CustomBrushEditor.ED.RotatorEnabled, enbled => CustomBrushEditor.ED.RotatorEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), CustomBrushEditor.ED, 120f);
            GUILayout.EndHorizontal();

            CustomLayerStyle.DrawSeparatorLine(5, 1.7f, new Color(0.3f, 0.3f, 0.3f, 0.5f));

            if (CustomBrushEditor.ED.RotatorEnabled)
            {
                EditorGUILayout.BeginVertical(CustomLayerStyle.ToggleBoxStyle());

                CustomBrushEditor.ED.Random_RotSpeed = Utils.EditPropertyWithUndo("�ӵ�", CustomBrushEditor.ED.Random_RotSpeed, speed => CustomBrushEditor.ED.Random_RotSpeed = speed, (label, value) => EditorGUILayout.FloatField(label, value), CustomBrushEditor.ED, 120f);

                EditorGUILayout.EndVertical();
                CustomLayerStyle.DrawSeparatorLine(0, 1.7f, new Color(0.3f, 0.3f, 0.3f, 0.5f));
            }

            GUI.color = originalColor;
            GUILayout.Space(5);

            CustomBrushEditor.ED.MoverEnabled = Utils.EditPropertyWithUndo("�̵�", CustomBrushEditor.ED.MoverEnabled, enbled => CustomBrushEditor.ED.MoverEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), CustomBrushEditor.ED, 120f);

            CustomLayerStyle.DrawSeparatorLine(5, 1.7f, new Color(0.3f, 0.3f, 0.3f, 0.5f));

            if (CustomBrushEditor.ED.MoverEnabled)
            {
                bool prevStraightEnbled = CustomBrushEditor.ED.StraightEnabled;
                bool prevBlackholeEnbled = CustomBrushEditor.ED.BlackholeEnabled;
                bool prevSnowEnabled = CustomBrushEditor.ED.SnowEnabled;

                GUILayout.BeginHorizontal(GUI.skin.box);

                EditorGUILayout.BeginVertical(CustomLayerStyle.ToggleBoxStyle());
                CustomBrushEditor.ED.StraightEnabled = Utils.EditPropertyWithUndo("����", CustomBrushEditor.ED.StraightEnabled, enbled => CustomBrushEditor.ED.StraightEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), CustomBrushEditor.ED, 120f);
                if (CustomBrushEditor.ED.StraightEnabled)
                {
                    CustomBrushEditor.ED.Straight_MoveSpeed = Utils.EditPropertyWithUndo("�ӵ�", CustomBrushEditor.ED.Straight_MoveSpeed, speed => CustomBrushEditor.ED.Straight_MoveSpeed = speed, (label, value) => EditorGUILayout.FloatField(label, value), CustomBrushEditor.ED, 120f);
                    CustomBrushEditor.ED.Straight_MoveDirection = Utils.EditPropertyWithUndo("����", CustomBrushEditor.ED.Straight_MoveDirection, direction => CustomBrushEditor.ED.Straight_MoveDirection = direction, (label, value) => (E_Direction)EditorGUILayout.EnumPopup(label, (E_Direction)value), CustomBrushEditor.ED, 120f);
                }
                EditorGUILayout.EndVertical();
                GUILayout.Space(5);

                EditorGUILayout.BeginVertical(CustomLayerStyle.ToggleBoxStyle());
                CustomBrushEditor.ED.BlackholeEnabled = Utils.EditPropertyWithUndo("��Ȧ", CustomBrushEditor.ED.BlackholeEnabled, enbled => CustomBrushEditor.ED.BlackholeEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), CustomBrushEditor.ED, 110f);
                if (CustomBrushEditor.ED.BlackholeEnabled)
                {
                    CustomBrushEditor.ED.Blackhole_AttractionForce = Utils.EditPropertyWithUndo("�ӵ�", CustomBrushEditor.ED.Blackhole_AttractionForce, speed => CustomBrushEditor.ED.Blackhole_AttractionForce = speed, (label, value) => EditorGUILayout.FloatField(label, value), CustomBrushEditor.ED, 110f);
                }
                EditorGUILayout.EndVertical();
                GUILayout.Space(5);

                EditorGUILayout.BeginVertical(CustomLayerStyle.ToggleBoxStyle());
                CustomBrushEditor.ED.SnowEnabled = Utils.EditPropertyWithUndo("��", CustomBrushEditor.ED.SnowEnabled, enbled => CustomBrushEditor.ED.SnowEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), CustomBrushEditor.ED, 130f);
                if (CustomBrushEditor.ED.SnowEnabled)
                {
                    CustomBrushEditor.ED.Snow_SwayIntensity = Utils.EditPropertyWithUndo("����", CustomBrushEditor.ED.Snow_SwayIntensity, speed => CustomBrushEditor.ED.Snow_SwayIntensity = speed, (label, value) => EditorGUILayout.FloatField(label, value), CustomBrushEditor.ED, 120f);
                    CustomBrushEditor.ED.Snow_SwayAmount = Utils.EditPropertyWithUndo("��鸲", CustomBrushEditor.ED.Snow_SwayAmount, speed => CustomBrushEditor.ED.Snow_SwayAmount = speed, (label, value) => EditorGUILayout.FloatField(label, value), CustomBrushEditor.ED, 110f);
                }
                EditorGUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUI.color = originalColor;

                CustomLayerStyle.DrawSeparatorLine(0, 1.7f, new Color(0.3f, 0.3f, 0.3f, 0.5f));

                CheckBrushEffectEnabled(prevStraightEnbled, prevBlackholeEnbled, prevSnowEnabled);

            }

            GUILayout.Space(5);

            CustomBrushEditor.ED.NatureEnabled = Utils.EditPropertyWithUndo("�ڿ�", CustomBrushEditor.ED.NatureEnabled, enbled => CustomBrushEditor.ED.NatureEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), CustomBrushEditor.ED, 120f);
            CustomLayerStyle.DrawSeparatorLine(5, 1.7f, new Color(0.3f, 0.3f, 0.3f, 0.5f));

            if (CustomBrushEditor.ED.NatureEnabled)
            {
                GUILayout.BeginHorizontal(GUI.skin.box);

                EditorGUILayout.BeginVertical(CustomLayerStyle.ToggleBoxStyle());
                CustomBrushEditor.ED.SnowSpawnEnabled = Utils.EditPropertyWithUndo("��", CustomBrushEditor.ED.SnowSpawnEnabled, enbled => CustomBrushEditor.ED.SnowSpawnEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), CustomBrushEditor.ED, 120f);
                if (CustomBrushEditor.ED.SnowSpawnEnabled)
                {
                    CustomBrushEditor.ED.SnowSpawn_SwayIntensity = Utils.EditPropertyWithUndo("����", CustomBrushEditor.ED.SnowSpawn_SwayIntensity, speed => CustomBrushEditor.ED.SnowSpawn_SwayIntensity = speed, (label, value) => EditorGUILayout.FloatField(label, value), CustomBrushEditor.ED, 120f);
                    CustomBrushEditor.ED.SnowSpawn_SwayAmount = Utils.EditPropertyWithUndo("��鸲", CustomBrushEditor.ED.SnowSpawn_SwayAmount, speed => CustomBrushEditor.ED.SnowSpawn_SwayAmount = speed, (label, value) => EditorGUILayout.FloatField(label, value), CustomBrushEditor.ED, 110f);
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

        GUI.Label(GUILayoutUtility.GetRect(GUIContent.none, CustomLayerStyle.BrushScaleLabelStyle(CustomBrushEditor.ED.CubeSize), GUILayout.Width(60), GUILayout.Height(60)), "��", CustomLayerStyle.BrushScaleLabelStyle(CustomBrushEditor.ED.CubeSize));
        GUILayout.Space(20);
        EditorGUILayout.BeginVertical();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("ũ�� : ");
        GUILayout.Space(-20);
        _brushScaleValue = EditorGUILayout.TextField(CustomBrushEditor.ED.CubeSize.ToString("F2"), GUILayout.MaxWidth(170));
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(20);
        Rect sliderRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.MaxWidth(300),  GUILayout.Height(EditorGUIUtility.singleLineHeight));
        CustomBrushEditor.ED.CubeSize = GUI.HorizontalSlider(sliderRect, CustomBrushEditor.ED.CubeSize, 0.1f, 1f);

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void SetBrushDistanceGUI()
    {
        EditorGUILayout.BeginHorizontal(GUI.skin.box);

        float space = (int)(CustomBrushEditor.ED.PlacementDistance * 7);

        string arrowText = "��" + new string(' ', (int)space) + "��";
        GUI.Label(GUILayoutUtility.GetRect(GUIContent.none, CustomLayerStyle.BrushDistanceLabelStyle(), GUILayout.Width(60), GUILayout.Height(60)), arrowText, CustomLayerStyle.BrushDistanceLabelStyle());
        GUILayout.Space(20);

        EditorGUILayout.BeginVertical();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("���� : ");
        GUILayout.Space(-20);
        _brushDistance = EditorGUILayout.TextField(CustomBrushEditor.ED.PlacementDistance.ToString("F2"), GUILayout.MaxWidth(170));
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(20);
        Rect sliderRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.MaxWidth(300), GUILayout.Height(EditorGUIUtility.singleLineHeight));
        CustomBrushEditor.ED.PlacementDistance = GUI.HorizontalSlider(sliderRect, CustomBrushEditor.ED.PlacementDistance, 0.1f, 1f);

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void SetBrushColorGUI()
    {
        EditorGUILayout.BeginHorizontal(GUI.skin.box);

        GUI.Label(GUILayoutUtility.GetRect(GUIContent.none, CustomLayerStyle.BrushColorLabelStyle(EditorGUIUtility.whiteTexture), GUILayout.Width(60), GUILayout.Height(60)), "", CustomLayerStyle.BrushColorLabelStyle(EditorGUIUtility.whiteTexture));
        GUILayout.Space(70);

        GUI.Label(GUILayoutUtility.GetRect(GUIContent.none, CustomLayerStyle.BrushColorLabelStyle(ColorPaletteGUI._satTex), GUILayout.Width(200), GUILayout.Height(200)), "", CustomLayerStyle.BrushColorLabelStyle(ColorPaletteGUI._satTex));

        GUILayout.Space(20);
        GUI.Label(GUILayoutUtility.GetRect(GUIContent.none, CustomLayerStyle.BrushColorLabelStyle(ColorPaletteGUI._hueTex), GUILayout.Width(20), GUILayout.Height(200)), "", CustomLayerStyle.BrushColorLabelStyle(ColorPaletteGUI._hueTex));

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }
}
