using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LayerInfo
{
    private static LayerInfoData s_infoData => GetLayerEditorData();
    public static LayerInfoData ED { get => s_infoData; }

    private static int s_generateId;
    public static int GenerateId { get => s_generateId; set => s_generateId = value; }

    public static Dictionary<int, Transform> LayerObjects { get; set; } = new Dictionary<int, Transform>();

    private static SortedSet<int> s_emptyLayerIds = new SortedSet<int>();
    public static SortedSet<int> EmptyLayerIds { get => s_emptyLayerIds; }

    public static List<int> ToDeleteLayerIds { get; set; } = new List<int>(); // 삭제 예정 아이디
    public static Dictionary<int, Transform> ToRestoreLayerIds { get; set; } = new Dictionary<int, Transform>(); // 복원 예정 아이디

    private static Transform s_currentLayer;
    public static Transform CurrentLayer { get => s_currentLayer; set => s_currentLayer = value; }


    public static LayerInfoData GetLayerEditorData() { return Resources.Load<LayerInfoData>("Data/LayerInfoData"); }

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
        ED.SelectedLayerIds.Clear();
    }
    //씬 동기화 작업
    public static void InitializeLayers()
    {
        Layer[] layersInScene = UnityEngine.Object.FindObjectsOfType<Layer>();
        foreach (Layer layerData in layersInScene)
        {
            int id = layerData.Id;
            if (!LayerObjects.ContainsKey(id))
            {
                LayerObjects.Add(id, layerData.transform);
            }
        }
    }
    public static void GenerateFirstLayerId()
    {
        while (true)
        {
            if (LayerObjects.ContainsKey(s_generateId) == false)
                break;

            s_generateId++;
        }
    }
    public static int GenerateLayerId()
    {
        int newLayerId;
        if (s_emptyLayerIds.Count > 0)
        {
            newLayerId = s_emptyLayerIds.First();
            s_emptyLayerIds.Remove(newLayerId);
        }
        else
        {
            GenerateFirstLayerId();
            newLayerId = s_generateId;
        }

        return newLayerId; 
    }

    public static void CreateLayer(int newLayerId , GameObject newLayer)
    {
        newLayer.GetOrAddComponent<Layer>();
        newLayer.transform.SetParent(BrushInfo.BrushParent);
        newLayer.transform.SetSiblingIndex(0);

        newLayer.GetComponent<Layer>().Initialize(newLayerId, newLayer.name);

        LayerObjects.Add(newLayerId, newLayer.transform);
        s_currentLayer = newLayer.transform;

        Undo.RegisterCreatedObjectUndo(s_currentLayer.gameObject, "Create Layer");

        Selection.activeGameObject = newLayer.gameObject;
        ED.SelectedLayerIds.Add(newLayerId);

        Utils.AddUndo("Create Layer" , () =>
        {
            var destroyedLayers = LayerObjects.Where(x => x.Value == null).Select(x => x.Key).ToList();
            if (destroyedLayers.Count > 0)
            {
                foreach (int id in destroyedLayers)
                {
                    ToDeleteLayerIds.Add(id);
                    EmptyLayerIds.Add(id);
                }
            }

            if (ED.SelectedLayerIds.Contains(newLayerId))
                ED.SelectedLayerIds.Remove(newLayerId);

            SearchTopLayerId();
        }  );
    }

    public static void CreateNewLayer()
    {
        int newLayerId = GenerateLayerId();

       int prevChildCount = BrushInfo.BrushParent.childCount;
    
        GameObject newLayer = new GameObject("새 레이어 " + newLayerId.ToString("00"));
        newLayer.transform.localPosition = new Vector3(0, 0, -0.01f * prevChildCount);

        CreateLayer(newLayerId, newLayer);
    
    }
    public static void CreateCloneLayer(int originalId, Vector3 dir)
    {
        if (!LayerObjects.ContainsKey(originalId))
            return;

        Transform originalLayer = LayerObjects[originalId];

        int newLayerId  = GenerateLayerId();

        GameObject newLayer = GameObject.Instantiate(originalLayer.gameObject); 
        newLayer.name = originalLayer.name + " Copy"; 
        newLayer.transform.localPosition = originalLayer.localPosition + dir * BrushInfo.ED.PlacementDistance;

        CreateLayer(newLayerId, newLayer);
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
    public static void SetLayerHasChanged()
    {
        foreach (var layerObj in LayerObjects)
        {
            if (layerObj.Value == null)
                continue;

            layerObj.Value.GetComponent<Layer>().HasChanged = true;
        }
    }
 
    public static void SelectLayerObjects()
    {
        GameObject[] selectedObjects = new GameObject[ED.SelectedLayerIds.Count];
        for (int i = 0; i < ED.SelectedLayerIds.Count; i++)
        {
            int layerId = ED.SelectedLayerIds[i];
            selectedObjects[i] = LayerObjects[layerId].gameObject;
        }

        Selection.objects = selectedObjects;
    }

    public static Dictionary<int, Transform> GetDictinaryLayers()
    {
        Transform cubeParent = BrushInfo.GetBrushParent();
        Dictionary<int, Transform> layers = new Dictionary<int, Transform>();

        for (int i = 0; i < cubeParent.childCount; i++)
        {
            Transform childLayer = cubeParent.GetChild(i);

            int id = childLayer.GetComponent<Layer>().Id;
            string name = childLayer.GetComponent<Layer>().Name;

            if (!layers.ContainsKey(id))
                layers.Add(id, childLayer);

            childLayer.name = name;
            childLayer.GetComponent<Layer>().HasChanged = true;
        }

        return layers;
    }

    public static void SearchTopLayerId()
    {
        ED.SelectedLayerIds.Clear();

        var remainingLayerObjects = LayerObjects
              .Where(x => x.Value != null)
              .OrderByDescending(x => x.Value.GetComponent<Layer>().CreationTimestamp);

        if (remainingLayerObjects.Any())
        {
            int topLayerId = remainingLayerObjects.First().Key;
            ED.SelectedLayerIds.Add(topLayerId);
        }
    }

    public static List<int> GetLayerIdList()
    {
        return LayerObjects
            .Where(x => x.Value != null)
            .Select(x => x.Key)
            .ToList();
    }
    public static List<Layer> GetLayerList()
    {
        List<Layer> layerDatas = new List<Layer>();

        foreach (var layerObj in LayerObjects)
        {
            if (layerObj.Value == null)
                continue;
            layerDatas.Add(layerObj.Value.GetComponent<Layer>());
        }

        return layerDatas;
    }

    public static List<Layer> GetLayerOrders()
    {
        List<Layer> layerDatas = new List<Layer>();

        Transform cubeParent = BrushInfo.BrushParent;
        foreach (Transform child in cubeParent)
        {
            Layer layerData = child.GetComponent<Layer>();
            if (layerData != null)
                layerDatas.Add(layerData);
        }

        return layerDatas;
    }

    public static List<KeyValuePair<int, Transform>> GetSortedCreationTimeLayerList()
    {
        return LayerObjects
            .Where(x => x.Value != null)
            .OrderByDescending(x => x.Value.GetComponent<Layer>().CreationTimestamp)
            .ToList();
           
    }
}
