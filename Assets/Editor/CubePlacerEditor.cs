using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class CubePlacerEditor
{
    private static CubePlacerEditorData s_editorData => GetCubeData();
    public static CubePlacerEditorData ED { get => s_editorData; }

    public static LayerDataStorage LayerStorage { get; set; }
    public static Dictionary<int, Transform> LayerObjects { get; set; } = new Dictionary<int, Transform>();
    public static List<int> ToDeleteLayerIds { get; set; } = new List<int>(); // 삭제 예정 아이디
    public static Dictionary<int, Transform> ToRestoreLayerIds { get; set; } = new Dictionary<int, Transform>(); // 복원 예정 아이디

    private static bool s_isPlacing;
    private static GameObject s_cubePrefab => GetCubePrefab();
    private static Transform s_cubeParent => GetCubeParent();
    private static Vector3 s_lastPlacedPosition;

    private static Transform s_currentLayer;

    private static Vector3 s_initialMousePosition;

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.update += RunOnceOnEditorLoad;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }
    private static void RunOnceOnEditorLoad()
    {
        InitializeLayers();
        LayerStorage = GetLayerData();
        EditorApplication.update -= RunOnceOnEditorLoad;
    }
    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        LayerObjects.Clear();
        InitializeLayers();
    }

    public static CubePlacerEditorData GetCubeData() { return Resources.Load<CubePlacerEditorData>("Data/CubePlacerEditorData");  }
    public static LayerDataStorage GetLayerData() { return Resources.Load<LayerDataStorage>("Data/LayerDataStorage"); }

    private static GameObject GetCubePrefab()
    {
        var cubePlacer = UnityEngine.Object.FindObjectOfType<CubePlacer>();
        return cubePlacer != null ? cubePlacer.CubePrefab : null;
    }

    public static Transform GetCubeParent()
    { 
        var parent = UnityEngine.Object.FindObjectOfType<CubePlacer>();
        return parent != null ? parent.transform : null;
    }

    //씬 동기화 작업
    public static void InitializeLayers()
    { 
        LayerData[] layersInScene = UnityEngine.Object.FindObjectsOfType<LayerData>();
        foreach (LayerData layerData in layersInScene)
        {
            int id = layerData.Id;
            if (!LayerObjects.ContainsKey(id))
            {
                LayerObjects.Add(id, layerData.transform);
            }
        }
    }
    private static void OnSceneGUI(SceneView sceneView)
    {
        if (s_isPlacing && s_cubePrefab != null)
        {
            Vector3 mousePosition = Event.current.mousePosition;
            mousePosition.y = sceneView.camera.pixelHeight - mousePosition.y;
            Ray ray = sceneView.camera.ScreenPointToRay(mousePosition);
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo))
            {
                // 쉬프트 키가 눌러져 있는지 확인
                bool shiftPressed = Event.current.shift;

                if (s_initialMousePosition != Vector3.zero)
                {
                    Vector3 direction = hitInfo.point - s_initialMousePosition;
                    if (shiftPressed && Mathf.Abs(direction.x) > Mathf.Abs(direction.y) && Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
                    {
                        hitInfo.point = new Vector3(hitInfo.point.x, s_initialMousePosition.y, s_initialMousePosition.z);
                    }
                    else if (shiftPressed && Mathf.Abs(direction.y) > Mathf.Abs(direction.z))
                    {
                        hitInfo.point = new Vector3(s_initialMousePosition.x, hitInfo.point.y, s_initialMousePosition.z);
                    }
                    else if (shiftPressed)
                    {
                        hitInfo.point = new Vector3(s_initialMousePosition.x, s_initialMousePosition.y, hitInfo.point.z);
                    }
                }
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {

                    PlaceCube(hitInfo.point);
                    s_lastPlacedPosition = hitInfo.point;
                    s_initialMousePosition = hitInfo.point;
                    Event.current.Use();

                    sceneView.Repaint();
                }
                else if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
                {
                    if (Vector3.Distance(s_lastPlacedPosition, hitInfo.point) >= s_editorData.PlacementDistance)
                    {
                        PlaceCube(hitInfo.point);
                        s_lastPlacedPosition = hitInfo.point;
                    }
                    Event.current.Use();

                    sceneView.Repaint();
                }
                else if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
                {
                    s_currentLayer = null;
                    s_initialMousePosition = Vector3.zero;
                }

                else if (Event.current.type == EventType.Layout)
                {
                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                }
            }

            Handles.BeginGUI();
            Rect rect1 = new Rect(10, 10, 200, 30);
            GUI.Label(rect1, "마우스 왼쪽 클릭 시 큐브 드로잉");
            Rect rect2 = new Rect(10, 30, 200, 30);
            GUI.Label(rect2, "Shift + Left Click = 수평,수직 긋기");
            Handles.EndGUI();
        }
    }
    private static void PlaceCube(Vector3 position)
    {
        Transform cubeParent = GetCubeParent();

        int selectedLayerId = CubePlacerEditorWindow.s_selectedLayerIndex;

        if (selectedLayerId >= 0)
        {
            GameObject cube = GameObject.Instantiate(s_cubePrefab, position, Quaternion.identity) as GameObject;
            Transform target = cubeParent.Cast<Transform>().FirstOrDefault(t => t.GetComponent<LayerData>() != null && t.GetComponent<LayerData>().Id == selectedLayerId);

            if (target == null)
                return;

            cube.layer = LayerMask.NameToLayer("Canvas");
            cube.transform.SetParent(target);      
            cube.transform.localScale = Vector3.one * s_editorData.CubeSize;

            cube.GetComponent<CubeRotator>().enabled = ED.RotatorEnabled;
            cube.GetComponent<CubeRotator>().RotationSpeed = ED.Random_RotSpeed;

            if (ED.MoverEnabled)
            {
                cube.GetComponent<CubeStraight>().enabled = ED.StraightEnabled;
                cube.GetComponent<CubeStraight>().MoveSpeed = ED.Straight_MoveSpeed;
                cube.GetComponent<CubeStraight>().MoveDirection = ED.Straight_MoveDirection;

                cube.GetComponent<CubeBlackhole>().enabled = ED.BlackholeEnabled;
                cube.GetComponent<CubeBlackhole>().AttractionForce = ED.Blackhole_AttractionForce;

                cube.GetComponent<CubeSnow>().enabled = ED.SnowEnabled;
                cube.GetComponent<CubeSnow>().SwayIntensity = ED.Snow_SwayIntensity;
                cube.GetComponent<CubeSnow>().SwayAmount = ED.Snow_SwayAmount;
            }

            if(ED.NatureEnabled)
            {
                cube.GetComponent<SnowSpawner>().enabled = ED.SnowSpawnEnabled;
                cube.GetComponent<SnowSpawner>().CubeSnowPrefab = s_cubePrefab;
                cube.GetComponent<SnowSpawner>().SwayAmount = ED.SnowSpawn_SwayAmount;
                cube.GetComponent<SnowSpawner>().SwayIntensity = ED.SnowSpawn_SwayIntensity;
          
            }

            Renderer renderer = cube.GetComponent<Renderer>();
            Material material = new Material(renderer.sharedMaterial);
            material.color = s_editorData.CubeColor;
            renderer.sharedMaterial = material;
            Undo.RegisterCreatedObjectUndo(cube, "Create Cube");
        }
    }

    public static void CreateNewLayer()
    {
        int newLayerId;
        if (LayerStorage.EmptyLayerIds.Count > 0)
        {
            newLayerId = LayerStorage.EmptyLayerIds.First();
            LayerStorage.EmptyLayerIds.Remove(newLayerId);
        }
        else
        {
            newLayerId = LayerStorage.GenerateId++;
        }

        GameObject newLayer = new GameObject("새 레이어 " + newLayerId.ToString("00"));
        newLayer.AddComponent(typeof(LayerData));
        newLayer.transform.SetParent(s_cubeParent);
        newLayer.transform.SetSiblingIndex(0);

        newLayer.GetComponent<LayerData>().Id = newLayerId;
        newLayer.GetComponent<LayerData>().CreationTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        newLayer.GetComponent<LayerData>().Name = newLayer.name;

        LayerObjects.Add(newLayerId, newLayer.transform);
        s_currentLayer = newLayer.transform;

        Undo.RegisterCreatedObjectUndo(s_currentLayer.gameObject, "Create Layer");

        CubePlacerEditorWindow.s_selectedLayerIndex = newLayerId;
    }

    public static void DeleteLayerIds()
    {
        if(ToDeleteLayerIds.Count > 0)
        {
            foreach (var id in ToDeleteLayerIds)
                LayerObjects.Remove(id);

            ToDeleteLayerIds.Clear();
        }
    }
    public static void RestoreLayerIds()
    {
        if (ToRestoreLayerIds.Count > 0)
        {
            foreach (var layerDic in ToRestoreLayerIds)
                LayerObjects.Add(layerDic.Key, layerDic.Value);
            
            ToRestoreLayerIds.Clear();
        }
    }
    public static void EnablePlacing()
    {
        s_isPlacing = true;
        Tools.current = Tool.None;
    }

    public static void DisablePlacing()
    {
        s_isPlacing = false;
        Tools.current = Tool.Move;
    }
}
