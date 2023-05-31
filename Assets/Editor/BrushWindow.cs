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
        if (_tabBtnStyle == null)
            _tabBtnStyle = LayerStyle.SetToggleTabStyle();

        _showBrushSettings = GUI.Toggle(new Rect(0, 10, 40, 40), _showBrushSettings, "����", _tabBtnStyle);

        if (_showBrushSettings)
        {
            GUILayout.Space(60);
            _showBrushEffect = false;
            // �⺻ �ɼ� [ ������ / ���� / ����]
            BrushEditor.ED.CubeSize = Utils.EditPropertyWithUndo("ũ��", BrushEditor.ED.CubeSize, newSize => BrushEditor.ED.CubeSize = newSize, (label, value) => EditorGUILayout.Slider(label, value, 0.1f, 2f), BrushEditor.ED);
            BrushEditor.ED.PlacementDistance = Utils.EditPropertyWithUndo("����", BrushEditor.ED.PlacementDistance, newDistance => BrushEditor.ED.PlacementDistance = newDistance, (label, value) => EditorGUILayout.Slider(label, value, 0.1f, 1f), BrushEditor.ED);
            BrushEditor.ED.CubeColor = Utils.EditPropertyWithUndo("����", BrushEditor.ED.CubeColor, newColor => BrushEditor.ED.CubeColor = newColor, (label, value) => EditorGUILayout.ColorField(label, value), BrushEditor.ED);
        }
        _showBrushEffect = GUI.Toggle(new Rect(40, 10, 40, 40), _showBrushEffect, "ȿ��", _tabBtnStyle);

        if (_showBrushEffect)
        {
            _showBrushSettings = false;
            GUILayout.Space(60);
            Color originalColor = GUI.color;

            GUILayout.BeginHorizontal();
            BrushEditor.ED.RotatorEnabled = Utils.EditPropertyWithUndo("ȸ��", BrushEditor.ED.RotatorEnabled, enbled => BrushEditor.ED.RotatorEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), BrushEditor.ED, 120f);
            GUILayout.EndHorizontal();

            LayerStyle.DrawSeparatorLine(5, 1.7f, new Color(0.3f, 0.3f, 0.3f, 0.5f));

            if (BrushEditor.ED.RotatorEnabled)
            {
                EditorGUILayout.BeginVertical(LayerStyle.SetToggleBoxStyle());

                BrushEditor.ED.Random_RotSpeed = Utils.EditPropertyWithUndo("�ӵ�", BrushEditor.ED.Random_RotSpeed, speed => BrushEditor.ED.Random_RotSpeed = speed, (label, value) => EditorGUILayout.FloatField(label, value), BrushEditor.ED, 120f);

                EditorGUILayout.EndVertical();
                LayerStyle.DrawSeparatorLine(0, 1.7f, new Color(0.3f, 0.3f, 0.3f, 0.5f));
            }

            GUI.color = originalColor;
            GUILayout.Space(5);

            BrushEditor.ED.MoverEnabled = Utils.EditPropertyWithUndo("�̵�", BrushEditor.ED.MoverEnabled, enbled => BrushEditor.ED.MoverEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), BrushEditor.ED, 120f);

            LayerStyle.DrawSeparatorLine(5, 1.7f, new Color(0.3f, 0.3f, 0.3f, 0.5f));

            if (BrushEditor.ED.MoverEnabled)
            {
                bool prevStraightEnbled = BrushEditor.ED.StraightEnabled;
                bool prevBlackholeEnbled = BrushEditor.ED.BlackholeEnabled;
                bool prevSnowEnabled = BrushEditor.ED.SnowEnabled;

                GUILayout.BeginHorizontal(GUI.skin.box);

                EditorGUILayout.BeginVertical(LayerStyle.SetToggleBoxStyle());
                BrushEditor.ED.StraightEnabled = Utils.EditPropertyWithUndo("����", BrushEditor.ED.StraightEnabled, enbled => BrushEditor.ED.StraightEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), BrushEditor.ED, 120f);
                if (BrushEditor.ED.StraightEnabled)
                {
                    BrushEditor.ED.Straight_MoveSpeed = Utils.EditPropertyWithUndo("�ӵ�", BrushEditor.ED.Straight_MoveSpeed, speed => BrushEditor.ED.Straight_MoveSpeed = speed, (label, value) => EditorGUILayout.FloatField(label, value), BrushEditor.ED, 120f);
                    BrushEditor.ED.Straight_MoveDirection = Utils.EditPropertyWithUndo("����", BrushEditor.ED.Straight_MoveDirection, direction => BrushEditor.ED.Straight_MoveDirection = direction, (label, value) => (E_Direction)EditorGUILayout.EnumPopup(label, (E_Direction)value), BrushEditor.ED, 120f);
                }
                EditorGUILayout.EndVertical();
                GUILayout.Space(5);

                EditorGUILayout.BeginVertical(LayerStyle.SetToggleBoxStyle());
                BrushEditor.ED.BlackholeEnabled = Utils.EditPropertyWithUndo("��Ȧ", BrushEditor.ED.BlackholeEnabled, enbled => BrushEditor.ED.BlackholeEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), BrushEditor.ED, 110f);
                if (BrushEditor.ED.BlackholeEnabled)
                {
                    BrushEditor.ED.Blackhole_AttractionForce = Utils.EditPropertyWithUndo("�ӵ�", BrushEditor.ED.Blackhole_AttractionForce, speed => BrushEditor.ED.Blackhole_AttractionForce = speed, (label, value) => EditorGUILayout.FloatField(label, value), BrushEditor.ED, 110f);
                }
                EditorGUILayout.EndVertical();
                GUILayout.Space(5);

                EditorGUILayout.BeginVertical(LayerStyle.SetToggleBoxStyle());
                BrushEditor.ED.SnowEnabled = Utils.EditPropertyWithUndo("��", BrushEditor.ED.SnowEnabled, enbled => BrushEditor.ED.SnowEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), BrushEditor.ED, 130f);
                if (BrushEditor.ED.SnowEnabled)
                {
                    BrushEditor.ED.Snow_SwayIntensity = Utils.EditPropertyWithUndo("����", BrushEditor.ED.Snow_SwayIntensity, speed => BrushEditor.ED.Snow_SwayIntensity = speed, (label, value) => EditorGUILayout.FloatField(label, value), BrushEditor.ED, 120f);
                    BrushEditor.ED.Snow_SwayAmount = Utils.EditPropertyWithUndo("��鸲", BrushEditor.ED.Snow_SwayAmount, speed => BrushEditor.ED.Snow_SwayAmount = speed, (label, value) => EditorGUILayout.FloatField(label, value), BrushEditor.ED, 110f);
                }
                EditorGUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUI.color = originalColor;

                LayerStyle.DrawSeparatorLine(0, 1.7f, new Color(0.3f, 0.3f, 0.3f, 0.5f));

                CheckBrushEffectEnabled(prevStraightEnbled, prevBlackholeEnbled, prevSnowEnabled);

            }

            GUILayout.Space(5);

            BrushEditor.ED.NatureEnabled = Utils.EditPropertyWithUndo("�ڿ�", BrushEditor.ED.NatureEnabled, enbled => BrushEditor.ED.NatureEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), BrushEditor.ED, 120f);
            LayerStyle.DrawSeparatorLine(5, 1.7f, new Color(0.3f, 0.3f, 0.3f, 0.5f));

            if (BrushEditor.ED.NatureEnabled)
            {
                GUILayout.BeginHorizontal(GUI.skin.box);

                EditorGUILayout.BeginVertical(LayerStyle.SetToggleBoxStyle());
                BrushEditor.ED.SnowSpawnEnabled = Utils.EditPropertyWithUndo("��", BrushEditor.ED.SnowSpawnEnabled, enbled => BrushEditor.ED.SnowSpawnEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), BrushEditor.ED, 120f);
                if (BrushEditor.ED.SnowSpawnEnabled)
                {
                    BrushEditor.ED.SnowSpawn_SwayIntensity = Utils.EditPropertyWithUndo("����", BrushEditor.ED.SnowSpawn_SwayIntensity, speed => BrushEditor.ED.SnowSpawn_SwayIntensity = speed, (label, value) => EditorGUILayout.FloatField(label, value), BrushEditor.ED, 120f);
                    BrushEditor.ED.SnowSpawn_SwayAmount = Utils.EditPropertyWithUndo("��鸲", BrushEditor.ED.SnowSpawn_SwayAmount, speed => BrushEditor.ED.SnowSpawn_SwayAmount = speed, (label, value) => EditorGUILayout.FloatField(label, value), BrushEditor.ED, 110f);
                }
                EditorGUILayout.EndVertical();
                GUILayout.Space(10);

                GUILayout.EndHorizontal();
                GUI.color = originalColor;

                LayerStyle.DrawSeparatorLine(0, 1.7f, new Color(0.3f, 0.3f, 0.3f, 0.5f));
            }
        }
    }

  
 
    private void CheckBrushEffectEnabled(bool prevStraightEnabled, bool prevBlackholeEnabled, bool prevSnowEnabled)
    {
        int cnt = 0;
        cnt = (BrushEditor.ED.StraightEnabled) ? cnt + 1 : cnt;
        cnt = (BrushEditor.ED.BlackholeEnabled) ? cnt + 1 : cnt;
        cnt = (BrushEditor.ED.SnowEnabled) ? cnt + 1 : cnt;

        if(cnt > 1)
        {
            if (prevStraightEnabled)
                BrushEditor.ED.StraightEnabled = false;
            else if(prevBlackholeEnabled)
                BrushEditor.ED.BlackholeEnabled = false;
            else if(prevSnowEnabled)
                BrushEditor.ED.SnowEnabled = false;
        }
    }

}
