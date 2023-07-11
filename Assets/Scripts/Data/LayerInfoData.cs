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
}
