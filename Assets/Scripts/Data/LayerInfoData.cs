using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "LayerInfoData", menuName = "LayerInfoData/Data", order = 1)]
public class LayerInfoData : ScriptableObject
{
    public Dictionary<int, GameLayer> LayerObjects { get; set; } = new Dictionary<int, GameLayer>();

    [SerializeField]
    public List<int> SelectedLayerIds = new List<int>();

    [SerializeField]
    public List<DataLayer> Layers = new List<DataLayer>();

    public DataLayer GetLayerById(int id)
    {
        return Layers.Find(item => item.Id == id);
    }

    public List<TResult> GetGameLayers<TResult>(Func<KeyValuePair<int, GameLayer>, TResult> selector, Func<TResult, IComparable> sort, bool isDescending = false)
    {
        var results = new List<TResult>();

        foreach (var layerObject in LayerObjects)
        {
            if (layerObject.Value != null)
                results.Add(selector(layerObject));
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
