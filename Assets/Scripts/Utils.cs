using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Utils
{
    public static T EditPropertyWithUndo<T>(string label, T currentValue, Action<T> setValueAction, Func<string, T, T> drawField, UnityEngine.Object undoRecordObject)
    {
        EditorGUI.BeginChangeCheck();
        T newValue = drawField(label, currentValue);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(undoRecordObject, $"{label} Change");
            setValueAction(newValue);
            EditorUtility.SetDirty(undoRecordObject);
        }

        return newValue;
    }
    public static T EditPropertyWithUndo<T>(string label, T currentValue, Action<T> setValue, Func<string, T, T> drawField, UnityEngine.Object undoRecordObject, float spaceBetweenLabelAndField)
    {
        Color origin = GUI.color;
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.BeginHorizontal();
        GUI.color = Color.white;
        EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth - spaceBetweenLabelAndField));
        T newValue = drawField("", currentValue);
        EditorGUILayout.EndHorizontal();
        GUI.color = origin;

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(undoRecordObject, $"{label} Change");
            setValue(newValue);
        }
        return newValue;
    }
}
