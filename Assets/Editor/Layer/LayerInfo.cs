using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor; 
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LayerInfo : InfoData<LayerInfoData>
{
    public static Dictionary<int, GameLayer> LayerObjects { get; set; } = new Dictionary<int, GameLayer>();

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.update += RunOnceOnEditorLoad;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }
    private static void RunOnceOnEditorLoad()
    {
        OnClear += () =>
        {
            LayerObjects.Clear();
            ToDeleteIds.Clear();
            ToRestoreIds.Clear();
            ED.SelectedLayerIds.Clear();

        };

        InitializeLayers();
        EditorApplication.update -= RunOnceOnEditorLoad;
    }
    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        InitializeLayers();
    }
    //씬 동기화 작업
    public static void InitializeLayers()
    {
        GameLayer[] layersInScene = UnityEngine.Object.FindObjectsOfType<GameLayer>();
        foreach (GameLayer layerData in layersInScene)
        {
            int id = layerData.Id;
            if (!LayerObjects.ContainsKey(id))
            {
                LayerObjects.Add(id, layerData);
            }
        }
    }

    public static void CreateLayer(int newLayerId , GameObject newLayerObj)
    {
        GameLayer newLayer =  newLayerObj.GetOrAddComponent<GameLayer>();
        newLayerObj.transform.SetParent(DrawingInfo.GameCanvas.transform);
        newLayerObj.transform.SetSiblingIndex(0);

        newLayerObj.GetComponent<GameLayer>().Initialize(newLayerId, newLayerObj.name);
        LayerObjects.Add(newLayerId, newLayer);

        DrawingInfo.GameCanvas.AddLayer(newLayer);
 
        Undo.RegisterCreatedObjectUndo(newLayerObj, "Create Layer");

        Selection.activeGameObject = newLayerObj.gameObject;
        ED.SelectedLayerIds.Add(newLayerId);

        Utils.AddUndo("Create Layer" , () =>
        {
            var destroyedLayers = LayerObjects.Where(x => x.Value == null).Select(x => x.Key).ToList();
            if (destroyedLayers.Count > 0)
            {
                foreach (int id in destroyedLayers)
                {
                    ToDeleteIds.Add(id);
                    EmptyGenerateIds.Add(id);
                    DrawingInfo.GameCanvas.RemoveLayer(id);
                }
            }

            if (ED.SelectedLayerIds.Contains(newLayerId))
                ED.SelectedLayerIds.Remove(newLayerId);

            SearchTopLayerId();
        }  );
    }

    public static void CreateNewLayer()
    {
        int newLayerId = NewGenerateId(LayerObjects);

       int prevChildCount = DrawingInfo.GameCanvas.transform.childCount;
    
        GameObject newLayer = new GameObject("새 레이어 " + newLayerId.ToString("00"));
        newLayer.transform.localPosition = new Vector3(0, 0, -0.01f * prevChildCount);

        CreateLayer(newLayerId, newLayer);
    
    }
    public static void CreateCloneLayer(int originalId, Vector3 dir)
    {
        if (!LayerObjects.ContainsKey(originalId))
            return;

        GameLayer originalLayer = LayerObjects[originalId];

        int newLayerId  = NewGenerateId(LayerObjects);

        GameObject newLayer = GameObject.Instantiate(originalLayer.gameObject); 
        newLayer.name = originalLayer.name + " Copy"; 
        newLayer.transform.localPosition = originalLayer.transform.localPosition + dir * BrushInfo.ED.PlacementDistance;

        CreateLayer(newLayerId, newLayer);
    }
    public static void SetLayerHasChanged()
    {
        foreach (var layerObj in LayerObjects)
        {
            if (layerObj.Value == null)
                continue;

            layerObj.Value.GetComponent<GameLayer>().HasChanged = true;
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

    public static Dictionary<int, GameLayer> GetLayersHierarchy()
    {
        Transform cubeParent = DrawingInfo.GameCanvas.transform;
        Dictionary<int, GameLayer> layers = new Dictionary<int, GameLayer>();

        for (int i = 0; i < cubeParent.childCount; i++)
        {
            Transform childLayerTr = cubeParent.GetChild(i);
            GameLayer childLayer = childLayerTr.GetComponent<GameLayer>();

            int id = childLayerTr.GetComponent<GameLayer>().Id;
            string name = childLayerTr.GetComponent<GameLayer>().Name;

            if (!layers.ContainsKey(id))
                layers.Add(id, childLayer);

            childLayerTr.name = name;
            childLayerTr.GetComponent<GameLayer>().HasChanged = true;
        }

        return layers;
    }

    public static void SearchTopLayerId()
    {
        ED.SelectedLayerIds.Clear();

        var remainingLayerObjects = LayerObjects
              .Where(x => x.Value != null)
              .OrderByDescending(x => x.Value.GetComponent<GameLayer>().CreationTimestamp);

        if (remainingLayerObjects.Any())
        {
            int topLayerId = remainingLayerObjects.First().Key;
            ED.SelectedLayerIds.Add(topLayerId);
            Selection.activeGameObject = remainingLayerObjects.First().Value.gameObject;
        }

 
    }

    public static List<int> GetLayerIdList()
    {
        return LayerObjects
            .Where(x => x.Value != null)
            .Select(x => x.Key)
            .ToList();
    }
    public static List<GameLayer> GetLayerList()
    {
        List<GameLayer> layerDatas = new List<GameLayer>();

        foreach (var layerObj in LayerObjects)
        {
            if (layerObj.Value == null)
                continue;
            layerDatas.Add(layerObj.Value.GetComponent<GameLayer>());
        }

        return layerDatas;
    }

    public static List<GameLayer> GetLayerOrders()
    {
        List<GameLayer> layerDatas = new List<GameLayer>();

        Transform cubeParent = DrawingInfo.GameCanvas.transform;
        foreach (Transform child in cubeParent)
        {
            GameLayer layerData = child.GetComponent<GameLayer>();
            if (layerData != null)
                layerDatas.Add(layerData);
        }
        return layerDatas;
    }
    public static List<KeyValuePair<int, GameLayer>> GetSortedCreationTimeLayerList()
    {
        return LayerObjects
            .Where(x => x.Value != null)
            .OrderByDescending(x => x.Value.CreationTimestamp)
            .ToList();
           
    }
}
