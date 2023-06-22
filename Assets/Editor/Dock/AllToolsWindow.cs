using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;

public class AllToolsWindow : EditorWindow
{
     BrushWindow _brushWindow;

    [MenuItem("Photoshop/All Tools %#w")]
    public static void ShowWindow()
    {
        if (Utils.IsWindowOpen<AllToolsWindow>() && Utils.IsWindowOpen<DrawingWindow>() )
            return;

          DockWindows();
    }
    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        minSize = new Vector2(600, 800);

        float width = 300;
        float height = 600;
        float xPos = (Screen.currentResolution.width - width) * 0.5f;
        float yPos = (Screen.currentResolution.height - height) * 0.5f;
         
        _brushWindow = CreateInstance<BrushWindow>();
    }
    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
        {
            var drawingWindow = GetWindow<DrawingWindow>("Drawing");
            var layerWindow = GetWindow<LayerWindow>("Layer");

            while (docked == false)
            {
                this.Dock(drawingWindow, E_DockPosition.Left);
                this.Dock(layerWindow, E_DockPosition.Bottom );
            }
        }
    }
    private static void DockWindows()
    {
        var allLayerWindow = GetWindow<AllToolsWindow>("Brush");
        var drawingWindow = GetWindow<DrawingWindow>("Drawing");
        var layerWindow = GetWindow<LayerWindow>("Layer");

        allLayerWindow.Dock(drawingWindow, E_DockPosition.Left);
        allLayerWindow.Dock(layerWindow, E_DockPosition.Bottom);
    }

 
    private void OnGUI()
    {
        _brushWindow.OnGUI();  
    }
}