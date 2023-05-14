using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class Utils
{
#if UNITY_EDITOR
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
    public static bool IsWindowOpen<T>() where T : EditorWindow
    {
        T[] windows = Resources.FindObjectsOfTypeAll<T>();
        return windows != null && windows.Length > 0;
    }
    public static bool IsWindowOpen(Type windowType)
    {
        EditorWindow[] windows = Resources.FindObjectsOfTypeAll(windowType) as EditorWindow[];
        return windows != null && windows.Length > 0;
    }

#endif
}
