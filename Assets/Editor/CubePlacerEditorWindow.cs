using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;
using System.Threading.Tasks;
using System;
using System.Linq;

public class CubePlacerEditorWindow : EditorWindow
{
    //����� ������ �� �� ���� �̹���
    private Texture2D _gridTexture;
    private Vector2 _scrollPosition = Vector2.zero; // �߰��� �ڵ�

    public static int s_selectedLayerIndex = -1;

    private bool _scrollToNewLayer = false;
    private float _layersTotalHeight;

    [MenuItem("Tools/Cube Placer/Settings %#q")]
    public static void ShowWindow()
    {
        GetWindow<CubePlacerEditorWindow>("���伥");
    }
    private void OnEnable()
    {
        _gridTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Textures/Grid.png");

        CubePlacerEditor.EnablePlacing();
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        Undo.undoRedoPerformed += OnUndoRedoPerformed;
    }


    private void OnDisable()
    {
        CubePlacerEditor.DisablePlacing();
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
    }

    private void OnUndoRedoPerformed()
    {
        Transform cubeParent = CubePlacerEditor.GetCubeParent();
        Dictionary<int, Transform> layers = new Dictionary<int, Transform>();

        for (int i = 0; i < cubeParent.childCount; i++)
        {
            Transform childLayer = cubeParent.GetChild(i);
            int id = childLayer.GetComponent<LayerData>().Id;
            string name = childLayer.GetComponent<LayerData>().Name;

            if (!layers.ContainsKey(id))
                layers.Add(id, childLayer);

            childLayer.name = name;
        }

        var destroyedLayers = CubePlacerEditor.LayerObjects.Where(x => x.Value == null).Select(x => x.Key).ToList();
        var createdLayers = layers.Keys.Except(CubePlacerEditor.LayerObjects.Keys).ToList();

        if (destroyedLayers.Count > 0)
        {
            foreach (int id in destroyedLayers)
            {
                CubePlacerEditor.ToDeleteLayerIds.Add(id);
                CubePlacerEditor.LayerStorage.EmptyLayerIds.Add(id);
            }
        }
        else if (createdLayers.Count > 0)
        {
            foreach (int id in createdLayers)
            {
                CubePlacerEditor.ToRestoreLayerIds.Add(id, layers[id]);
                CubePlacerEditor.LayerStorage.EmptyLayerIds.Remove(id);
            }
        }
        Repaint();
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Tools.current = Tool.Move;
            CubePlacerEditor.DisablePlacing();
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            CubePlacerEditor.EnablePlacing();
        }
    }

    private T EditPropertyWithUndo<T>(string label, T currentValue, Action<T> setValueAction, Func<string,T,T> drawField, UnityEngine.Object objectToRecord)
    {
        EditorGUI.BeginChangeCheck();
        T newValue = drawField(label, currentValue);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(objectToRecord, $"{label} Change");
            setValueAction(newValue);
            EditorUtility.SetDirty(objectToRecord);
        }

        return newValue;
    }
    private static T EditPropertyWithUndo<T>(string label, T currentValue, Action<T> setValue, Func<string, T, T> drawField, UnityEngine.Object undoRecordObject, float spaceBetweenLabelAndField)
    {
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth - spaceBetweenLabelAndField));
        T newValue = drawField("", currentValue);
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(undoRecordObject, $"{label} Change");
            setValue(newValue);
        }
        return newValue;
    }

    public Texture2D CaptureLayerSnapshot(Transform layer)
    {
        int width = 64;
        int height = 64;

        Camera tempCamera = new GameObject("TempCamera").AddComponent<Camera>();
        Camera mainCamera = Camera.main;

        tempCamera.transform.position = mainCamera.transform.position;
        tempCamera.transform.rotation = mainCamera.transform.rotation;
        tempCamera.orthographic = true;
        tempCamera.orthographicSize = mainCamera.orthographicSize;
        tempCamera.aspect = mainCamera.aspect;
        tempCamera.clearFlags = CameraClearFlags.SolidColor;
        tempCamera.backgroundColor = Color.clear;

        int canvasLayer = LayerMask.NameToLayer("Canvas");
        tempCamera.cullingMask = 1 << canvasLayer;

        //  ���� ���̾ ������ �ٸ� ���̾���� ��Ȱ��ȭ
        Transform cubeParent = CubePlacerEditor.GetCubeParent();
        List<bool> layerStates = new List<bool>();
        for (int i = 0; i < cubeParent.childCount; i++)
        {
            Transform childLayer = cubeParent.GetChild(i);
            layerStates.Add(childLayer.gameObject.activeSelf);
            if (childLayer != layer)
            {
                childLayer.gameObject.SetActive(false);
            }
        }

        RenderTexture renderTexture = new RenderTexture(width, height, 24);
        GameObject gridQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        gridQuad.layer = LayerMask.NameToLayer("Canvas");
        gridQuad.transform.position = Vector3.zero;
        gridQuad.transform.rotation = tempCamera.transform.rotation;
        gridQuad.transform.localScale = new Vector3(50, 50, 1.0f);
        gridQuad.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/Transparent"));
        gridQuad.GetComponent<Renderer>().sharedMaterial.mainTexture = _gridTexture;
        gridQuad.GetComponent<Renderer>().sharedMaterial.color = Color.gray;

        tempCamera.targetTexture = renderTexture;
        tempCamera.Render();

        RenderTexture.active = renderTexture;
        Texture2D snapshot = new Texture2D(width, height, TextureFormat.RGB24, false);
        snapshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        snapshot.Apply();

        RenderTexture.active = null;
        DestroyImmediate(tempCamera.gameObject);
        DestroyImmediate(renderTexture);
        DestroyImmediate(gridQuad);

        // ���� ���̾� ���·� ����
        for (int i = 0; i < cubeParent.childCount; i++)
        {
            Transform childLayer = cubeParent.GetChild(i);
            childLayer.gameObject.SetActive(layerStates[i]);
        }

        return snapshot;
    }
    private void OnGUI()
    {
        var cubePlacer = UnityEngine.Object.FindObjectOfType<CubePlacer>();
        if (cubePlacer == null)
        {
            if (GUILayout.Button("ĵ���� �����", GUILayout.Height(100)))
                CreateCanvas();

            return;
        }
        GUILayout.Label("�귯�� ����", EditorStyles.boldLabel);
        GUILayout.Space(10);

        //�⺻ �ɼ� [ ������ / ���� / ����]
        CubePlacerEditor.ED.CubeSize = EditPropertyWithUndo("ũ��", CubePlacerEditor.ED.CubeSize, newSize => CubePlacerEditor.ED.CubeSize = newSize, (label, value) => EditorGUILayout.Slider(label, value, 0.1f, 2f), CubePlacerEditor.ED); 
        CubePlacerEditor.ED.PlacementDistance = EditPropertyWithUndo("����", CubePlacerEditor.ED.PlacementDistance, newDistance => CubePlacerEditor.ED.PlacementDistance = newDistance, (label, value) => EditorGUILayout.Slider(label, value, 0.1f, 1f), CubePlacerEditor.ED);   
        CubePlacerEditor.ED.CubeColor = EditPropertyWithUndo("����", CubePlacerEditor.ED.CubeColor, newColor => CubePlacerEditor.ED.CubeColor = newColor, (label, value) => EditorGUILayout.ColorField(label, value), CubePlacerEditor.ED);

        GUILayout.Space(20);
        GUILayout.Label("�귯�� ȿ��", EditorStyles.boldLabel);

        GUILayout.Space(10);
        EditorGUILayout.BeginVertical();
        CubePlacerEditor.ED.RotatorEnabled = EditPropertyWithUndo("ȸ��", CubePlacerEditor.ED.RotatorEnabled, enbled => CubePlacerEditor.ED.RotatorEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), CubePlacerEditor.ED, 120f);
      
        if (CubePlacerEditor.ED.RotatorEnabled)
        {
            CubePlacerEditor.ED.RotSpeed = EditPropertyWithUndo("�ӵ�", CubePlacerEditor.ED.RotSpeed, speed => CubePlacerEditor.ED.RotSpeed = speed, (label, value) => EditorGUILayout.FloatField(label, value), CubePlacerEditor.ED, 120f);
        }
        EditorGUILayout.EndVertical();
        GUILayout.Space(10);

        CubePlacerEditor.ED.MoverEnabled = EditPropertyWithUndo("�̵�", CubePlacerEditor.ED.MoverEnabled, enbled => CubePlacerEditor.ED.MoverEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), CubePlacerEditor.ED, 120f);

        if (CubePlacerEditor.ED.MoverEnabled)
        {
            bool prevStraightEnbled = CubePlacerEditor.ED.StraightEnabled;
            bool prevBlackholeEnbled = CubePlacerEditor.ED.BlackholeEnabled;
            bool prevSnowEnabled = CubePlacerEditor.ED.SnowEnabled;

            GUILayout.BeginHorizontal(GUI.skin.box);

            EditorGUILayout.BeginVertical();
            CubePlacerEditor.ED.StraightEnabled = EditPropertyWithUndo("����", CubePlacerEditor.ED.StraightEnabled, enbled => CubePlacerEditor.ED.StraightEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), CubePlacerEditor.ED, 120f);
            if (CubePlacerEditor.ED.StraightEnabled)
            {
                CubePlacerEditor.ED.MoveSpeed = EditPropertyWithUndo("�ӵ�", CubePlacerEditor.ED.MoveSpeed, speed => CubePlacerEditor.ED.MoveSpeed = speed, (label, value) => EditorGUILayout.FloatField(label, value), CubePlacerEditor.ED, 120f);
                GUILayout.Space(10);
                CubePlacerEditor.ED.MoveDirection = EditPropertyWithUndo("����", CubePlacerEditor.ED.MoveDirection, direction => CubePlacerEditor.ED.MoveDirection = direction, (label, value) => (E_Direction)EditorGUILayout.EnumPopup(label, (E_Direction)value), CubePlacerEditor.ED, 120f);
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);

            EditorGUILayout.BeginVertical();
            CubePlacerEditor.ED.BlackholeEnabled = EditPropertyWithUndo("��Ȧ", CubePlacerEditor.ED.BlackholeEnabled, enbled => CubePlacerEditor.ED.BlackholeEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), CubePlacerEditor.ED, 110f);
            if (CubePlacerEditor.ED.BlackholeEnabled)
            {
                CubePlacerEditor.ED.AttractionForce = EditPropertyWithUndo("�ӵ�", CubePlacerEditor.ED.AttractionForce, speed => CubePlacerEditor.ED.AttractionForce = speed, (label, value) => EditorGUILayout.FloatField(label, value), CubePlacerEditor.ED, 110f);
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);

            EditorGUILayout.BeginVertical();
            CubePlacerEditor.ED.SnowEnabled = EditPropertyWithUndo("��", CubePlacerEditor.ED.SnowEnabled, enbled => CubePlacerEditor.ED.SnowEnabled = enbled, (label, value) => EditorGUILayout.Toggle(label, value), CubePlacerEditor.ED, 130f);
            if (CubePlacerEditor.ED.SnowEnabled)
            {
                CubePlacerEditor.ED.SwayIntensity = EditPropertyWithUndo("����", CubePlacerEditor.ED.SwayIntensity, speed => CubePlacerEditor.ED.SwayIntensity = speed, (label, value) => EditorGUILayout.FloatField(label, value), CubePlacerEditor.ED, 120f);
                CubePlacerEditor.ED.SwayAmount = EditPropertyWithUndo("��鸲", CubePlacerEditor.ED.SwayAmount, speed => CubePlacerEditor.ED.SwayAmount = speed, (label, value) => EditorGUILayout.FloatField(label, value), CubePlacerEditor.ED, 110f);
            }
            EditorGUILayout.EndVertical();
            GUILayout.EndHorizontal();

            CheckBrushEffectClear();
            CheckBrushEffectEnabled( prevStraightEnbled, prevBlackholeEnbled, prevSnowEnabled);
        }

        GUILayout.Space(20);
        GUILayout.Label("���̾�", EditorStyles.boldLabel);

        Transform cubeParent = CubePlacerEditor.GetCubeParent();

        if (cubeParent != null)
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true);
            _layersTotalHeight = 0;

            var sortedLayerObjects = CubePlacerEditor.LayerObjects
                  .Where(x => x.Value != null)
                  .OrderByDescending(x => x.Value.GetComponent<LayerData>().CreationTimestamp)
                  .ToList();

            foreach (var layerPair in sortedLayerObjects)
            {
                int i = layerPair.Key;
                Transform layer = layerPair.Value;

                if (layer == null)
                    continue;

                Color originalBackgroundColor = GUI.backgroundColor;
                GUI.backgroundColor = (i == s_selectedLayerIndex) ? Color.gray : Color.clear;

                GUILayout.BeginHorizontal(GUI.skin.box);

                Texture2D layerSnapshot = CaptureLayerSnapshot(layer);

                Rect imageRect = GUILayoutUtility.GetRect(50, 50);
                GUI.DrawTexture(imageRect, layerSnapshot, ScaleMode.ScaleToFit);

                LayerData layerData = layer.GetComponent<LayerData>();

                //���̾� �̸� ����
                string layerName = layerData.Name;
                layerName = EditPropertyWithUndo(
                    "",
                    layerName,
                    newName => layerData.Name = newName,
                    (label, value) => EditorGUILayout.TextField(value, GUILayout.Width(120)),
                    layerData
                );

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Delete", GUILayout.Width(50)))
                {
                    ClickDeleteLayer(layer);
                }

                GUILayout.EndHorizontal();

                Rect layerRect = GUILayoutUtility.GetLastRect();
                layerRect.width = EditorGUIUtility.currentViewWidth;
                if (Event.current.type == EventType.MouseDown && layerRect.Contains(Event.current.mousePosition))
                {
                    s_selectedLayerIndex = layerData.Id;
                    Event.current.Use();
                }

                GUI.backgroundColor = originalBackgroundColor;
                _layersTotalHeight += layerRect.height + GUI.skin.box.margin.vertical;
            }

            CubePlacerEditor.DeleteLayerIds();
            CubePlacerEditor.RestoreLayerIds();

            GUILayout.EndScrollView();
            UpdateScrollView();
        }

        GUILayout.Space(20);

        if (GUILayout.Button("Reset EditorPrefs"))
        {
            ResetEditorPrefs();
        }

        GUILayout.Space(20);

        if (GUILayout.Button("Create New Layer", GUILayout.Height(60)))
        {
            CubePlacerEditor.CreateNewLayer();
            GUIUtility.keyboardControl = 0;
            _scrollToNewLayer = true;
        }
    }

    private static void CreateCanvas()
    {
        Camera main = Camera.main;
        main.orthographic = true;
        main.orthographicSize = 10.2f;
        main.transform.position = new Vector3(0, 0, -10);

        GameObject canvas = new GameObject("Canvas");
        GameObject collider = new GameObject("Collider");
        canvas.AddComponent<CubePlacer>();
        canvas.GetComponent<CubePlacer>().CubePrefab = Resources.Load<GameObject>("Prefab/Cube");
        collider.AddComponent<BoxCollider>();
        collider.GetComponent<BoxCollider>().isTrigger = true;
        collider.GetComponent<BoxCollider>().size = new Vector3(100, 100, 0.2f);
    }

    private void ResetEditorPrefs()
    {
        EditorPrefs.DeleteAll();
        Debug.Log("EditorPrefs has been reset.");
        CubePlacerEditor.LayerStorage.GenerateId = 0;
        CubePlacerEditor.LayerStorage.EmptyLayerIds.Clear();
        CubePlacerEditor.LayerObjects.Clear();
    }
    private void ClickDeleteLayer(Transform layer)
    {
        int layerIndex = CubePlacerEditor.LayerObjects.FirstOrDefault(x => x.Value == layer).Key;
        CubePlacerEditor.ToDeleteLayerIds.Add(layerIndex);
        CubePlacerEditor.LayerStorage.EmptyLayerIds.Add(layerIndex);

        GUIUtility.keyboardControl = 0;

        Undo.DestroyObjectImmediate(layer.gameObject);
    }
    private void CheckBrushEffectClear()
    {
        if (CubePlacerEditor.ED.MoverEnabled == false)
        {
            CubePlacerEditor.ED.StraightEnabled = false;
            CubePlacerEditor.ED.BlackholeEnabled = false;
            CubePlacerEditor.ED.SnowEnabled = false;
        }
    }

    private void CheckBrushEffectEnabled(bool prevStraightEnabled, bool prevBlackholeEnabled, bool prevSnowEnabled)
    {
        int cnt = 0;
        cnt = (CubePlacerEditor.ED.StraightEnabled) ? cnt + 1 : cnt;
        cnt = (CubePlacerEditor.ED.BlackholeEnabled) ? cnt + 1 : cnt;
        cnt = (CubePlacerEditor.ED.SnowEnabled) ? cnt + 1 : cnt;

        if(cnt > 1)
        {
            if (prevStraightEnabled)
                CubePlacerEditor.ED.StraightEnabled = false;
            else if(prevBlackholeEnabled)
                CubePlacerEditor.ED.BlackholeEnabled = false;
            else if(prevSnowEnabled)
                CubePlacerEditor.ED.SnowEnabled = false;
        }
    }

    private void UpdateScrollView()
    {
        if (_scrollToNewLayer)
        {
            Transform cubeParent = CubePlacerEditor.GetCubeParent();

            int selectedIndex = cubeParent.childCount - 1 - s_selectedLayerIndex;

            if (selectedIndex >= 0)
            {
                float elementHeight = 70;
                float totalHeight = elementHeight * selectedIndex;
                float halfWindowHeight = position.height * 0.5f;

                _scrollPosition.y = Mathf.Clamp(totalHeight - halfWindowHeight, 0, Mathf.Max(0, _layersTotalHeight - position.height));
            }
            _scrollToNewLayer = false;
            Repaint();
        }
    }
}
