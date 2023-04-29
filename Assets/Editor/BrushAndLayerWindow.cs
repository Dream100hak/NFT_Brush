using UnityEditor;
using UnityEngine;

public class BrushAndLayerWindow : EditorWindow
{
    private BrushWindow _brushWindow;
    private LayerWindow _layerWindow;

    [MenuItem("Photoshop/Brush And Layer %#q")]
    public static void ShowWindow()
    {
        var mainEditorWindow = GetWindow<BrushAndLayerWindow>("Brush");
        var drawingLayerWindow = GetWindow<DrawingWindow>("Drawing");
      
        var position = new Rect(mainEditorWindow.position.x - drawingLayerWindow.position.width, mainEditorWindow.position.y, drawingLayerWindow.position.width, drawingLayerWindow.position.height);
        drawingLayerWindow.ShowAtPosition(position);
    }

    private void OnEnable()
    {
        _brushWindow = CreateInstance<BrushWindow>();
        _layerWindow = CreateInstance<LayerWindow>();
 
    }

    private bool _isDraggingSeparator;
    private const float MinDrawingLayerWindowWidth = 300.0f;

    private void OnGUI()
    {   
        // BrushWindow와 LayerWindow를 오른쪽에 독립적으로 배치
        EditorGUILayout.BeginVertical();

        // BrushWindow를 오른쪽 상단에 배치
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        _brushWindow.OnGUI();
        EditorGUILayout.EndVertical();

        // LayerWindow를 오른쪽 하단에 배치
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        _layerWindow.OnGUI();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndVertical();
    }
}