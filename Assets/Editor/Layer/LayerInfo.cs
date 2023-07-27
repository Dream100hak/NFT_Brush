using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor; 
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LayerInfo : InfoData<LayerInfoData>
{

    [InitializeOnLoadMethod]
    private static void Initialize() => EditorSceneManager.sceneOpened += OnSceneOpened;
    private static void OnSceneOpened(Scene scene, OpenSceneMode mode) => Clear();
    public static void ClearHandler()
    {
        ED.LayerObjects.Clear();
        ED.SelectedLayerIds.Clear();
        ED.Layers.Clear();
        GameLayer[] layersInScene = UnityEngine.Object.FindObjectsOfType<GameLayer>();
        foreach (GameLayer layerData in layersInScene)
        UnityEngine.Object.DestroyImmediate(layerData.gameObject);
    }
    //씬 동기화 작업
    public static void InitializeLayers()
    {
        GameLayer[] layersInScene = UnityEngine.Object.FindObjectsOfType<GameLayer>();
        foreach (GameLayer layerData in layersInScene)
        {
            int id = layerData.Id;
            if (!ED.LayerObjects.ContainsKey(id))
            {
                ED.LayerObjects.Add(id, layerData);
            }
        }
    }

    public static void CreateLayer(int newLayerId , GameObject newLayerObj , long timestamp = -1)
    {
        GameLayer newLayer =  newLayerObj.GetOrAddComponent<GameLayer>();
        newLayer.transform.SetParent(DrawingInfo.GameCanvas.transform);
        newLayer.transform.SetSiblingIndex(0);

        newLayer.Initialize(newLayerId, newLayerObj.name , timestamp);
        ED.LayerObjects.Add(newLayerId, newLayer);

        Undo.RegisterCreatedObjectUndo(newLayerObj, "Create Layer");

        Selection.activeGameObject = newLayerObj.gameObject;
        ED.SelectedLayerIds.Add(newLayerId);

        SearchTopLayerId();

        // Load로 불러온 레이어는 Undo 제외
        if(timestamp == -1)
        {
            Utils.AddUndo("Create Layer", () =>
            {
                var destroyedLayers = ED.LayerObjects.Where(x => x.Value == null).Select(x => x.Key).ToList();
                if (destroyedLayers.Count > 0)
                {
                    foreach (int id in destroyedLayers)
                    {
                        ToDeleteIds.Add(id);
                        EmptyGenerateIds.Add(id);
                    }
                }

                if (ED.SelectedLayerIds.Contains(newLayerId))
                    ED.SelectedLayerIds.Remove(newLayerId);

                SearchTopLayerId();
            });
        }  
    }

    public static void CreateNewLayer()
    {
        int newLayerId = NewGenerateId(ED.LayerObjects);

       int prevChildCount = DrawingInfo.GameCanvas.transform.childCount;
    
        GameObject newLayer = new GameObject("새 레이어 " + newLayerId.ToString("00"));
        newLayer.transform.localPosition = new Vector3(0, 0, -0.01f * prevChildCount);

        CreateLayer(newLayerId, newLayer);
    
    }
    public static void LoadLayer()
    {
        int prevChildCount = DrawingInfo.GameCanvas.transform.childCount;

        foreach(DataLayer layer in ED.Layers)
        {
            GameObject newLayer = new GameObject(layer.Name);
            newLayer.transform.localPosition = new Vector3(0, 0, -0.01f * prevChildCount);
            CreateLayer(layer.Id, newLayer , layer.CreateTimestamp);
        }
    }

    public static void CreateCloneLayer(int originalId, Vector3 dir)
    {
        if (!ED.LayerObjects.ContainsKey(originalId))
            return;

        GameLayer originalLayer = ED.LayerObjects[originalId];

        int newLayerId  = NewGenerateId(ED.LayerObjects);

        GameObject newLayer = GameObject.Instantiate(originalLayer.gameObject); 
        newLayer.name = originalLayer.name + " Copy"; 
        newLayer.transform.localPosition = originalLayer.transform.localPosition + dir * BrushInfo.ED.PlacementDistance;

        CreateLayer(newLayerId, newLayer);
    }
    public static void SetLayerHasChanged()
    {
        foreach (var layerObj in ED.LayerObjects)
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
            selectedObjects[i] = ED.LayerObjects[layerId].gameObject;
        }

        Selection.objects = selectedObjects;
    }

    public static void SearchTopLayerId()
    {
        ED.SelectedLayerIds.Clear();

        var remainingLayerObjects = ED.LayerObjects
              .Where(x => x.Value != null)
              .OrderByDescending(x => x.Value.GetComponent<GameLayer>().CreationTimestamp);

        if (remainingLayerObjects.Any())
        {
            int topLayerId = remainingLayerObjects.First().Key;
            ED.SelectedLayerIds.Add(topLayerId);
            Selection.activeGameObject = remainingLayerObjects.First().Value.gameObject;
        }
    }

    public static List<TResult> GetHierarchyLayers<TResult>(Func<GameLayer, TResult> selector, Func<TResult, IComparable> sort, bool isDescending = false)
    {
        var results = new List<TResult>();

        if (DrawingInfo.GameCanvas == null)
            return results;

        foreach (Transform child in DrawingInfo.GameCanvas.transform)
        {
            GameLayer gameLayer = child.GetComponent<GameLayer>();
            if (gameLayer != null)
                results.Add(selector(gameLayer));
        }

        if (sort != null)
        {
            if (isDescending)
                results.Sort((x, y) => sort(y).CompareTo(sort(x)));
            else
                results.Sort((x, y) => sort(x).CompareTo(sort(y)));
        }

        return results;
    }
}
