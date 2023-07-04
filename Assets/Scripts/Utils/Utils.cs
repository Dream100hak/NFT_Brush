using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public static class Utils
{
    private static Stack<(string eventName, Action action)> _undoStack = new Stack<(string, Action)>();
    private static Stack<(string eventName, Action action)> _redoStack = new Stack<(string, Action)>();

    public static void UndoPop()
    {
        (string eventName, Action undoAction) = _undoStack.Pop();
        Debug.Log("<color=red>Pop :</color> " + eventName);
    }

    public static void ClearUndoRedo()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
    public static void UndoExecute()
    {
        if (_undoStack.Count > 0)
        {
            (string eventName, Action undoAction) = _undoStack.Pop();
            _redoStack.Push((eventName, undoAction));
            undoAction.Invoke();

            Debug.Log("<color=red>Undo Execute :</color> " + eventName);
        }
    }

    public static void RedoExecute()
    {
        if (_redoStack.Count > 0)
        {
            (string eventName, Action redoAction) = _redoStack.Pop();
            _undoStack.Push((eventName, redoAction ));
            redoAction.Invoke();

            Debug.Log("Redo : " + eventName);
        }
    }

    public static void AddUndo(string eventName, Action action)
    {
        Debug.Log("<color=orange>Undo Add :</color> " + eventName);
        _undoStack.Push((eventName, action));
        _redoStack.Clear();
    }

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

    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        return go.GetComponent<T>() ?? go.AddComponent<T>();
    }
#endif
    public static Vector3 SetZVectorZero(Vector3 vec)
    {
        vec.z = 0;
        return vec;
    }
}
