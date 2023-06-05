using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;

public class BrushWindow : EditorWindow
{
    private bool _showBrushSettings = true;
    private bool _showBrushEffect = true;

    private GUIStyle _tabBtnStyle = null;

    [MenuItem("Photoshop/Brush")]
    public static void ShowWindow()
    {
       GetWindow<BrushWindow>("Brush");
    }

    public void OnGUI()
    {
        float tabBoxPosX = 0;
        if (_tabBtnStyle == null)
            _tabBtnStyle = CustomLayerStyle.SetToggleTabStyle();

       
        _showBrushSettings = GUI.Toggle(new Rect(tabBoxPosX, 10, 40, 40), _showBrushSettings, "설정", _tabBtnStyle);
        tabBoxPosX += 40f;

        if (_showBrushSettings)
        {
            GUILayout.Space(60);
            _showBrushEffect = false;
            // 기본 옵션 [ 사이즈 / 간격 / 색상]
            CustomBrushEditor.ED.CubeSize = Utils.EditPropertyWithUndo("크기", CustomBrushEditor.ED.CubeSize, newSize => CustomBrushEditor.ED.CubeSize = newSize, (label, value) => EditorGUILayout.Slider(label, value, 0.1f, 2f), CustomBrushEditor.ED);
            CustomBrushEditor.ED.PlacementDistance = Utils.EditPropertyWithUndo("간격", CustomBrushEditor.ED.PlacementDistance, newDistance => CustomBrushEditor.ED.PlacementDistance = newDistance, (label, value) => EditorGUILayout.Slider(label, value, 0.1f, 1f), CustomBrushEditor.ED);
            CustomBrushEditor.ED.CubeColor = Utils.EditPropertyWithUndo("색상", CustomBrushEditor.ED.CubeColor, newColor => CustomBrushEditor.ED.CubeColor = newColor, (label, value) => EditorGUILayout.ColorField(label, value), CustomBrushEditor.ED);

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
                EditorGUILayout.BeginVertical(CustomLayerStyle.SetToggleBoxStyle());

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

                EditorGUILayout.BeginVertical(CustomLayerStyle.SetToggleBoxStyle());
                CustomBrushEditor.ED.StraightEnabled = Utils.EditPropertyWithUndo("직선", CustomBrushEditor.ED.StraightEnabled, enbled => CustomBrushEditor.ED.StraightEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), CustomBrushEditor.ED, 120f);
                if (CustomBrushEditor.ED.StraightEnabled)
                {
                    CustomBrushEditor.ED.Straight_MoveSpeed = Utils.EditPropertyWithUndo("속도", CustomBrushEditor.ED.Straight_MoveSpeed, speed => CustomBrushEditor.ED.Straight_MoveSpeed = speed, (label, value) => EditorGUILayout.FloatField(label, value), CustomBrushEditor.ED, 120f);
                    CustomBrushEditor.ED.Straight_MoveDirection = Utils.EditPropertyWithUndo("방향", CustomBrushEditor.ED.Straight_MoveDirection, direction => CustomBrushEditor.ED.Straight_MoveDirection = direction, (label, value) => (E_Direction)EditorGUILayout.EnumPopup(label, (E_Direction)value), CustomBrushEditor.ED, 120f);
                }
                EditorGUILayout.EndVertical();
                GUILayout.Space(5);

                EditorGUILayout.BeginVertical(CustomLayerStyle.SetToggleBoxStyle());
                CustomBrushEditor.ED.BlackholeEnabled = Utils.EditPropertyWithUndo("블랙홀", CustomBrushEditor.ED.BlackholeEnabled, enbled => CustomBrushEditor.ED.BlackholeEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), CustomBrushEditor.ED, 110f);
                if (CustomBrushEditor.ED.BlackholeEnabled)
                {
                    CustomBrushEditor.ED.Blackhole_AttractionForce = Utils.EditPropertyWithUndo("속도", CustomBrushEditor.ED.Blackhole_AttractionForce, speed => CustomBrushEditor.ED.Blackhole_AttractionForce = speed, (label, value) => EditorGUILayout.FloatField(label, value), CustomBrushEditor.ED, 110f);
                }
                EditorGUILayout.EndVertical();
                GUILayout.Space(5);

                EditorGUILayout.BeginVertical(CustomLayerStyle.SetToggleBoxStyle());
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

                EditorGUILayout.BeginVertical(CustomLayerStyle.SetToggleBoxStyle());
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

}
