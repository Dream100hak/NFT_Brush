using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
[CreateAssetMenu(fileName = "DrawingInfoData", menuName = "DrawingInfoData/Data", order = 1)]
public class DrawingInfoData : ScriptableObject
{

    public List<DrawingCanvas> CanvasDatas;

    
}
