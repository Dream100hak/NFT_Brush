using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LayerEditor
{
    private static LayerEditorData s_editorData => GetLayerEditorData();
    public static LayerEditorData ED { get => s_editorData; }

    private static int s_generateId;
    public static int GenerateId { get => s_generateId; set => s_generateId = value; }

    public static Dictionary<int, Transform> LayerObjects { get; set; } = new Dictionary<int, Transform>();

    private static SortedSet<int> s_emptyLayerIds = new SortedSet<int>();
    public static SortedSet<int> EmptyLayerIds { get => s_emptyLayerIds; }

    public static List<int> ToDeleteLayerIds { get; set; } = new List<int>(); // 삭제 예정 아이디
    public static Dictionary<int, Transform> ToRestoreLayerIds { get; set; } = new Dictionary<int, Transform>(); // 복원 예정 아이디

    private static Transform s_currentLayer;
    public static Transform CurrentLayer { get => s_currentLayer; set => s_currentLayer = value; }

    public static LayerEditorData GetLayerEditorData() { return Resources.Load<LayerEditorData>("Data/LayerEditorData"); }

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

    public static void LayerGenerateId()
    {
        while (true)
        {
            if (LayerObjects.ContainsKey(s_generateId) == false)
                break;

            s_generateId++;
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
            LayerGenerateId();
            newLayerId = s_generateId;
        }

        GameObject newLayer = new GameObject("새 레이어 " + newLayerId.ToString("00"));
        newLayer.AddComponent(typeof(LayerData));
        newLayer.transform.SetParent(BrushEditor.CubeParent);
        newLayer.transform.SetSiblingIndex(0);

        newLayer.GetComponent<LayerData>().Id = newLayerId;
        newLayer.GetComponent<LayerData>().CreationTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        newLayer.GetComponent<LayerData>().Name = newLayer.name;

        LayerObjects.Add(newLayerId, newLayer.transform);
        s_currentLayer = newLayer.transform;

        Undo.RegisterCreatedObjectUndo(s_currentLayer.gameObject, "Create Layer");

        ED.SelectedLayerIds.Add(newLayerId);
    }
    public static void DeleteLayerIds()
    {
        if (ToDeleteLayerIds.Count > 0)
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

    public static  List<LayerData> GetLayerDatas()
    {
        List<LayerData> layerDatas = new List<LayerData>();

        foreach (var layerObj in LayerObjects)
        {
            if (layerObj.Value == null)
                continue;
            layerDatas.Add(layerObj.Value.GetComponent<LayerData>());
        }

        return layerDatas;
    }
    public static List<LayerData> GetLayerOrders()
    {
        List<LayerData> layerDatas = new List<LayerData>();

        Transform cubeParent = BrushEditor.CubeParent;
        foreach (Transform child in cubeParent)
        {
            LayerData layerData = child.GetComponent<LayerData>();
            if (layerData != null)
                layerDatas.Add(layerData);
        }

        return layerDatas;
    }

}
