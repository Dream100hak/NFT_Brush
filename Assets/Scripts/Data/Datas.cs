using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Brush
{
    public int Id;
    public string Name;
    public GameObject TargetObj;
    public bool Selected;
}

[Serializable]
public class DrawingCanvas
{
    public int Id;
    public string Name;
    public Vector2Int Size; 
    public Texture2D Snapshot;

}
