using System;
using System.Collections.Generic;
using UnityEngine;

public class CanvasData
{
    public CanvasData(string canvasName , float width, float height)
    {
        _canvasName = canvasName;
        _width = width;
        _height = height;
    }

    private string _canvasName;
    private float _width;
    private float _height;

    // TODO : 낑겨넣어야 함

}

[Serializable]
[CreateAssetMenu(fileName = "DrawingInfoData", menuName = "DrawingInfoData/Data", order = 1)]
public class DrawingInfoData : ScriptableObject
{

    public List<CanvasData> CanvasDatas;


}
