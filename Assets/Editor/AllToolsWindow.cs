using System;
using UnityEditor;
using UnityEngine;

public class AllToolsWindow : EditorWindow
{
    BrushWindow _brushWindow;

    [MenuItem("Photoshop/All Tools %#w")]
    public static void ShowWindow()
    {
        if (IsWindowOpen<AllToolsWindow>() && IsWindowOpen<DrawingWindow>() && IsWindowOpen<LayerWindow>())
        {
            return;
        }

        var allLayerWindow = GetWindow<AllToolsWindow>("Brush");
        var drawingWindow = GetWindow<DrawingWindow>("Drawing");
        var layerWindow = GetWindow<LayerWindow>("Layer");

        allLayerWindow.Dock(drawingWindow, E_DockPosition.Left);
        allLayerWindow.Dock(layerWindow, E_DockPosition.Bottom);
    }

    private static bool IsWindowOpen<T>() where T : EditorWindow
    {
        T[] windows = Resources.FindObjectsOfTypeAll<T>();
        return windows != null && windows.Length > 0;
    }

    private void OnEnable()
    {
        minSize = new Vector2(600, 800);

        // Set the initial size of the window
        float width = 800;
        float height = 600;
        float xPos = (Screen.currentResolution.width - width) * 0.5f;
        float yPos = (Screen.currentResolution.height - height) * 0.5f;

        // Adjust for the main editor window
        var mainEditorWindowPosition = GetMainEditorWindowPosition();
        xPos += mainEditorWindowPosition.x;
        yPos += mainEditorWindowPosition.y;

        position = new Rect(xPos, yPos, width, height);

        _brushWindow = CreateInstance<BrushWindow>();
    }
    private Vector2 GetMainEditorWindowPosition()
    {
        Vector2 position = Vector2.zero;
        Type mainEditorWindowType = Type.GetType("UnityEditor.MainView.UnityEditor");
        if (mainEditorWindowType != null)
        {
            var mainEditorWindow = UnityEditor.EditorWindow.GetWindow(mainEditorWindowType);
            if (mainEditorWindow != null)
            {
                position = new Vector2(mainEditorWindow.position.x, mainEditorWindow.position.y);
            }
        }
        return position;
    }
    private void OnGUI()
    {
        _brushWindow.OnGUI();  
    }
}