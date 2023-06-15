using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class Utils
{
    public static Stack<Action> _undoStack = new Stack<Action>();
    public static Stack<Action> _redoStack = new Stack<Action>();
    public static Stack<Action> UndoStack { get => _undoStack; }

    public static void UndoExecute()
    {
        if (_undoStack.Count > 0)
        {
            Action undoAction = _undoStack.Pop();
            _redoStack.Push(undoAction); 
            undoAction.Invoke();
        }
    }
    public static void RedoExeCute()
    {
        if (_redoStack.Count > 0)
        {
            Action redoAction = _redoStack.Pop();
            _undoStack.Push(redoAction);
            redoAction.Invoke(); 
        }
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
    public static GUIContent GetIconContent(string name)
    {
        return EditorGUIUtility.IconContent(name);
    }

#endif
    public static Vector3 SetZVectorZero(Vector3 vec)
    {
        vec.z = 0;
        return vec;
    }
}
