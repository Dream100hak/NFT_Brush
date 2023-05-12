using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BrushEditor
{

    private static SortedSet<int> s_emptyLayerIds = new SortedSet<int>();
    public static SortedSet<int> EmptyLayerIds { get => s_emptyLayerIds; }

    private static int s_generateId;
    public static int GenerateId { get => s_generateId; set => s_generateId = value; }
    private static CubePlacerEditorData s_editorData => GetCubeData();
    public static CubePlacerEditorData ED { get => s_editorData; }
    public static Dictionary<int, Transform> LayerObjects { get; set; } = new Dictionary<int, Transform>();
    public static List<int> ToDeleteLayerIds { get; set; } = new List<int>(); // 삭제 예정 아이디
    public static Dictionary<int, Transform> ToRestoreLayerIds { get; set; } = new Dictionary<int, Transform>(); // 복원 예정 아이디

    private static bool s_isPlacing;
    public static bool IsPlacing { get => s_isPlacing; }
    private static GameObject s_cubePrefab => GetCubePrefab();
    public static GameObject CubePrefab { get => s_cubePrefab; }
    private static Transform s_cubeParent => GetCubeParent();
    public static Transform CubeParent { get => s_cubeParent; }

    private static Transform s_currentLayer;
    public static Transform CurrentLayer { get => s_currentLayer; set => s_currentLayer = value; }

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.update += RunOnceOnEditorLoad;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }
    private static void RunOnceOnEditorLoad()
    {
        InitializeLayers();
        EditorApplication.update -= RunOnceOnEditorLoad;
    }
    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        Clear();

        InitializeLayers();
    }
    public static void Clear()
    {
        LayerObjects.Clear();
        s_emptyLayerIds.Clear();
        s_generateId = 0;
        s_currentLayer = null;
        ToDeleteLayerIds.Clear();
        ToRestoreLayerIds.Clear();
    }

    public static CubePlacerEditorData GetCubeData() { return Resources.Load<CubePlacerEditorData>("Data/BrushData");  }
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
    public static void PlaceCube(Vector3 position)
    {
        Transform cubeParent = GetCubeParent();

        int selectedLayerId = LayerWindow.s_selectedLayerIndex;

        if (selectedLayerId >= 0)
        {
            GameObject cube = GameObject.Instantiate(s_cubePrefab, position, Quaternion.identity) as GameObject;
            Transform target = cubeParent.Cast<Transform>().FirstOrDefault(t => t.GetComponent<LayerData>() != null && t.GetComponent<LayerData>().Id == selectedLayerId);

            if (target == null)
                return;

            cube.name = "Cube";
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

    public static void CubeGenerateId()
    {
        while(true)
        {
            if (LayerObjects.ContainsKey(s_generateId) == false)
                break;

            s_generateId++;       
        }
    }
    public static void RemoveCube(RaycastHit hitInfo)
    {
        if (hitInfo.collider != null && hitInfo.collider.gameObject.layer == LayerMask.NameToLayer("Canvas"))
        {
            GameObject cube = hitInfo.collider.gameObject;
            Undo.DestroyObjectImmediate(cube);
        }
    }

    public static void CreateNewLayer()
    {
        int newLayerId;
        if (s_emptyLayerIds.Count > 0)
        {
            newLayerId = s_emptyLayerIds.First();
            s_emptyLayerIds.Remove(newLayerId);
        }
        else
        {
            CubeGenerateId();
            newLayerId = s_generateId;
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

        LayerWindow.s_selectedLayerIndex = newLayerId;
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
