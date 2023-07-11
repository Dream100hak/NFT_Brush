using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DrawingBrush
{
    public int Id;
    public string Name;
    public GameObject TargetObj;
    public bool Selected;
}
[Serializable]
public class DataBrush
{
    public int Id;
    public int TypeId;
    public float R, G, B, A;
    public float PosX, PosY, PosZ;
    public float ScaleX, ScaleY, ScaleZ;
    public int ParentLayer;
}

[Serializable]
public class DataLayer
{
    public int Id;
    public string Name;
    public long CreateTimestamp;
    public int BrushCount;
}

[Serializable]
public class DataCanvas
{
    public int Id;
    public string Name;
    public Vector2Int Size; 
    public Texture2D Snapshot;

}
