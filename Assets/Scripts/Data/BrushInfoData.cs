using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "BrushInfoData", menuName = "BrushInfoData/Data", order = 1)]
public class BrushInfoData : ScriptableObject
{
    public Dictionary<int, GameBrush> BrushObjects { get; set; } = new Dictionary<int, GameBrush>();
    public List<DrawingBrush> SelectedBrushes;

    public List<DataBrush> DataBrushes;

    public float BrushSize = 0.5f;
    public float PlacementDistance  = 1.0f;

    public Color BrushColor = Color.white;
    public float Hue, SV , Value;
    public Vector2 HuePos, SVPos;

    public bool RotatorEnabled = false;
    public bool MoverEnabled = false;
    public bool NatureEnabled = false;

    public float Random_RotSpeed = 1.0f;

    public bool StraightEnabled = false;
    public float Straight_MoveSpeed = 1.0f;
    public E_Direction Straight_MoveDirection = E_Direction.Down;

    public bool BlackholeEnabled= false;
    public float Blackhole_AttractionForce = 1.0f;

    public bool SnowEnabled = false;
    public float Snow_SwayIntensity = 1.0f;
    public float Snow_SwayAmount = 0.1f;

    public bool SnowSpawnEnabled = false;
    public float SnowSpawn_SwayIntensity = 1.0f;
    public float SnowSpawn_SwayAmount = 0.1f;

    public DrawingBrush GetSelectedBrushById(int id)
    {
        return SelectedBrushes.Find(item => item.Id == id);
    }
    public void SetSelectedBrushById(int id)
    {
        foreach(DrawingBrush brush in SelectedBrushes)
        {
            brush.Selected = false;
        }

        SelectedBrushes[id].Selected = true;
    }

    public int GetSelectedBrushId()
    {
        return SelectedBrushes.FindIndex(item => item.Selected == true);
    }

}
