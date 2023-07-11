using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
[CreateAssetMenu(fileName = "DrawingInfoData", menuName = "DrawingInfoData/Data", order = 1)]
public class DrawingInfoData : ScriptableObject
{
    public List<DataCanvas> Canvases;

    public Dictionary<int, GameCanvas> CanvasObjects { get; set; } = new Dictionary<int, GameCanvas>();
    public DataCanvas GetCanvasByName(string name)
    {
        return Canvases.Find(item => item.Name == name);
    }
    public DataCanvas GetCanvasById(int id)
    {
        return Canvases.Find(item => item.Id == id);
    }
}
