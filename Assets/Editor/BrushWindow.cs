using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;


public class BrushWindow : EditorWindow
{
    private bool[] _showBrushTabs = new bool[3];
    int _selectedTabCnt = 0;

    private Rect _svRect, _knobSVRect;
    private Rect _hueRect , _knobHueRect;
    private Action _actPaletteUpdate;

    [MenuItem("Photoshop/Brush")]
    public static void ShowWindow() => GetWindow<BrushWindow>("Brush");
    private void OnSceneOpened(Scene scene, OpenSceneMode mode) => Initialize();
    private void RefreshTabBtn()
    {
        for (int i = 0; i < _showBrushTabs.Length; i++)
            _showBrushTabs[i] = EditorPrefs.GetBool("ShowBrushTab_" + i, true);
    }

    private void Initialize()
    {
        ColorPaletteGUI.Color = BrushInfo.ED.BrushColor;
        ColorPaletteGUI.Hue = BrushInfo.ED.Hue;
        ColorPaletteGUI.Saturation = BrushInfo.ED.SV;
        ColorPaletteGUI.Value = BrushInfo.ED.Value;
        ColorPaletteGUI.ApplyHue();
        ColorPaletteGUI.ApplySaturation();

        _knobSVRect.position = BrushInfo.ED.SVPos;
        _knobHueRect.position = BrushInfo.ED.HuePos;

        Action dragH = null;
        Action dragSV = null;
        Action idle = () =>
        {
            Event e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (_hueRect.Contains(e.mousePosition))
                {
                    DragHue();
                    _actPaletteUpdate = dragH;
                }
                if (_svRect.Contains(e.mousePosition))
                {
                    DragSV();
                    _actPaletteUpdate = dragSV;
                }
            }
        };
        //HUE 
        dragH = () =>
        {
            Event e = Event.current;

            if (e.type == EventType.MouseDrag)
                DragHue();

            else if (e.type == EventType.MouseUp)
            {
                _actPaletteUpdate = idle;
                e.Use();
            }
        };
        //SATURATION 
        dragSV = () =>
        {
            Event e = Event.current;

            if (e.type == EventType.MouseDrag)
                DragSV();

            else if (e.type == EventType.MouseUp)
            {
                _actPaletteUpdate = idle;
                e.Use();
            }
        };

        _actPaletteUpdate = idle;
    }
    public void OnEnable()
    {
        RefreshTabBtn();
        Initialize();
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private void OnDisable()
    {
        EditorSceneManager.sceneOpened -= OnSceneOpened;
    }
    public void DragHue()
    {
        Vector2 mousePos = Event.current.mousePosition;
        float y = Mathf.Clamp(mousePos.y - _hueRect.y, 0, _hueRect.height);
        ColorPaletteGUI.Hue = y / _hueRect.height;

        BrushInfo.ED.Hue = ColorPaletteGUI.Hue;

        ColorPaletteGUI.ApplyHue();
        ColorPaletteGUI.ApplySaturation();

        BrushInfo.ED.BrushColor = ColorPaletteGUI.Color;

        _knobHueRect = new Rect(_hueRect.x - 34 + _hueRect.width, _hueRect.y - 5 + _hueRect.height * ColorPaletteGUI.Hue, 10, 10);
        BrushInfo.ED.HuePos = _knobHueRect.position;
       
        Repaint();
        Event.current.Use();
    }

    public void DragSV()
    {
        Vector2 mousePos = Event.current.mousePosition;
        float x = Mathf.Clamp(mousePos.x - _svRect.x, -5, _svRect.width - 5);
        float y = Mathf.Clamp(mousePos.y - _svRect.y, -5, _svRect.height - 5);
        ColorPaletteGUI.Saturation = x / _svRect.width;
        ColorPaletteGUI.Value = 1 - (y / _svRect.height);

        BrushInfo.ED.SV = ColorPaletteGUI.Saturation;
        BrushInfo.ED.Value = ColorPaletteGUI.Value;

        ColorPaletteGUI.ApplySaturation();

        BrushInfo.ED.BrushColor =ColorPaletteGUI.Color;

        _knobSVRect = new Rect(_svRect.x + _svRect.width * ColorPaletteGUI.Saturation, _svRect.y + _svRect.height * (1 - ColorPaletteGUI.Value), 10, 10);
        BrushInfo.ED.SVPos = _knobSVRect.position;

        Repaint();
        Event.current.Use();
    }

    void OptionRefresh(int id , string name ,  ref float tabBoxPosX, Action act)
    {  
        GUIStyle tabBtnStyle = EditorHelper.ToggleTabStyle();

        _showBrushTabs[id] = GUI.Toggle(new Rect(tabBoxPosX, 10, 40, 40), _showBrushTabs[id] , name, tabBtnStyle);
        EditorPrefs.SetBool("ShowBrushTab_" + id, _showBrushTabs[id]);
        _selectedTabCnt = _showBrushTabs.Count(x => x == true);

        if (_showBrushTabs[id])
            act?.Invoke();

        tabBoxPosX += 40f;
    }

    void DrawTab()
    {
        float tabBoxPosX = 0;

        OptionRefresh((int)E_BrushOption.Type, "종류", ref tabBoxPosX, () =>
        {
            GUILayout.Space(60);
            BrushInfo.DrawGridBrush(position.x, new Vector2(60, 60));
        });

        OptionRefresh((int)E_BrushOption.Setting, "설정", ref tabBoxPosX, () =>
        {
            float space = _showBrushTabs[(int)E_BrushOption.Type] ? GUILayoutUtility.GetLastRect().y + 20f : 60;
            GUILayout.Space(space);
            SetBrushScaleGUI();
            SetBrushDistanceGUI();
        });

        OptionRefresh((int)E_BrushOption.Color, "색상", ref tabBoxPosX, () =>
        {
            float space = _selectedTabCnt > 1 ? GUILayoutUtility.GetLastRect().y + 20f : 60;
            GUILayout.Space(space);
            SetBrushColorGUI();
        });

        GUI.DrawTexture(new Rect(tabBoxPosX, 13, position.width * 5, 1), EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, false, 1f, new Color32(29, 29, 29, 255), 0, 0);
        GUI.DrawTexture(new Rect(tabBoxPosX, 14f, position.width * 5, 36), EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, false, 1f, new Color32(41, 41, 41, 255), 0, 0);
        GUI.DrawTexture(new Rect(tabBoxPosX, 50, position.width * 5, 1), EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, false, 1f, new Color32(29, 29, 29, 255), 0, 0);
    }

    public void OnGUI()
    {
        DrawTab();
    }
    private void SetBrushScaleGUI()
    {
        EditorGUILayout.BeginHorizontal(GUI.skin.box);

        GUI.Label(EditorHelper.GetRect(60,60 , EditorHelper.BrushScale(BrushInfo.ED.BrushSize)) ,  "●", EditorHelper.BrushScale(BrushInfo.ED.BrushSize));
        GUILayout.Space(20);
        EditorGUILayout.BeginVertical();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("크기 : ");
        GUILayout.Space(-20);

        string scaleStr = BrushInfo.ED.BrushSize.ToString("F2");

        scaleStr = EditorGUILayout.TextField(BrushInfo.ED.BrushSize.ToString("F2"), GUILayout.MaxWidth(170));

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(20);
        Rect sliderRect = EditorHelper.GetRect(300, EditorGUIUtility.singleLineHeight);
        BrushInfo.ED.BrushSize = Utils.EditPropertyWithUndo("크기", BrushInfo.ED.BrushSize, newSize => BrushInfo.ED.BrushSize = newSize, (label, value) => GUI.HorizontalSlider(sliderRect, value, 0.1f, 1f), BrushInfo.ED);

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }
    private void SetBrushDistanceGUI()
    {
        EditorGUILayout.BeginHorizontal(GUI.skin.box);

        float space = (int)(BrushInfo.ED.PlacementDistance * 7);

        string arrowText = "●" + new string(' ', (int)space) + "●";
        GUI.Label(EditorHelper.GetRect(60,60 , EditorHelper.BrushDistance()) , arrowText, EditorHelper.BrushDistance());
        GUILayout.Space(20);

        EditorGUILayout.BeginVertical();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("간격 : ");
        GUILayout.Space(-20);

        string distStr = BrushInfo.ED.PlacementDistance.ToString("F2");
        distStr = EditorGUILayout.TextField(BrushInfo.ED.PlacementDistance.ToString("F2"), GUILayout.MaxWidth(170));
      
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(20);
        Rect sliderRect = EditorHelper.GetRect(300, EditorGUIUtility.singleLineHeight);
        BrushInfo.ED.PlacementDistance = Utils.EditPropertyWithUndo("간격", BrushInfo.ED.PlacementDistance , newDistance => BrushInfo.ED.PlacementDistance = newDistance, (label, value) => GUI.HorizontalSlider(sliderRect, value, 0.1f, 1f), BrushInfo.ED);

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }
    private void SetBrushColorGUI()
    {
        EditorGUILayout.BeginHorizontal(GUI.skin.box);

        Rect paletteRect = EditorHelper.GetRect(60, 60);
        GUI.DrawTexture(paletteRect, Resources.Load<Texture2D>("Textures/Icon/ColorPaletteIcon"), ScaleMode.StretchToFill);
        paletteRect.x += 5; paletteRect.y += 5; paletteRect.width = 50; paletteRect.height = 50;

        GUI.DrawTexture(paletteRect, ColorPaletteGUI.ResultTex, ScaleMode.StretchToFill);

        GUILayout.Space(70);

        _svRect = EditorHelper.GetRect(200, 200);
        _knobSVRect = new Rect(_svRect.x + _svRect.width * ColorPaletteGUI.Saturation, _svRect.y + _svRect.height * (1 - ColorPaletteGUI.Value), 10, 10);

        GUI.DrawTexture(_svRect, ColorPaletteGUI.SatTex, ScaleMode.StretchToFill);
        GUI.DrawTexture(_knobSVRect, Resources.Load<Texture2D>("Textures/Icon/Knob_01"), ScaleMode.StretchToFill);
       
        GUILayout.Space(20);

        _hueRect = EditorHelper.GetRect(20, 200);
        _knobHueRect = new Rect(_hueRect.x - 34 + _hueRect.width, _hueRect.y - 5 + _hueRect.height * ColorPaletteGUI.Hue, 10, 10);

        GUI.DrawTexture(_hueRect, ColorPaletteGUI.HueTex, ScaleMode.StretchToFill);
        GUI.DrawTexture(_knobHueRect, Resources.Load<Texture2D>("Textures/Icon/Knob_02"), ScaleMode.StretchToFill);

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        _actPaletteUpdate?.Invoke();
    }
}
