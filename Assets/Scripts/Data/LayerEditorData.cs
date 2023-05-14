using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LayerEditorData", menuName = "LayerEditorData/Data", order = 1)]
public class LayerEditorData : ScriptableObject
{
    [SerializeField]
    public List<int> SelectedLayerIds = new List<int>();
}
