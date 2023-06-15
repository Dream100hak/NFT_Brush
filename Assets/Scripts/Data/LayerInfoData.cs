using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LayerInfoData", menuName = "LayerInfoData/Data", order = 1)]
public class LayerInfoData : ScriptableObject
{
    [SerializeField]
    public List<int> SelectedLayerIds = new List<int>();
}
