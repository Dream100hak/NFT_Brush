using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

[CreateAssetMenu(fileName = "BrushInfoData", menuName = "BrushInfoData/Data", order = 1)]
public class BrushInfoData : ScriptableObject
{

    public List<Brush> Brushes;

    public float BrushSize = 0.5f;
    public float PlacementDistance  = 1.0f;
    public Color BrushColor = Color.white;

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

    public Brush GetBrush(int id)
    {
        return Brushes.Find(item => item.Id == id);
    }
    public void SetSelectedBrushId(int id)
    {
        foreach(Brush brush in Brushes)
        {
            brush.Selected = false;
        }

        Brushes[id].Selected = true;
    }

    public int GetSelectedBrushId()
    {
        return Brushes.FindIndex(item => item.Selected == true);
    }

}
