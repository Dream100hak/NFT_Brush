using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "BrushInfoData", menuName = "BrushInfoData/Data", order = 1)]
public class BrushInfoData : ScriptableObject
{
    public Dictionary<int, GameBrush> BrushObjects { get; set; } = new Dictionary<int, GameBrush>();
    public List<DrawingBrush> TypeBrushes;

    public List<DataBrush> DataBrushes;

    [Range(1f, 10f)]
    public float BrushSize = 1f;
    [Range(0.5f, 1f)]
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

    public DrawingBrush GetTypeBrushById(int id) =>  TypeBrushes.Find(item => item.Id == id);
    public int GetTypeBrushId() =>  TypeBrushes.FindIndex(item => item.Selected == true);

    public void SetTypeBrushById(int id)
    {
        foreach(DrawingBrush brush in TypeBrushes)
        {
            brush.Selected = false;
        }

        TypeBrushes[id].Selected = true;
    }

    public List<TResult> GetGameBrushes<TResult>(Func<KeyValuePair<int, GameBrush>, TResult> selector, Func<TResult, IComparable> sort, bool isDescending = false)
    {
        var results = new List<TResult>();

        foreach (var brushObj in BrushObjects)
        {
            if (brushObj.Value != null)
            {
                var result = selector(brushObj);
                if (result != null)
                    results.Add(result);
            }
        }

        if (sort != null)
        {
            if (isDescending)
                results.Sort((x, y) => sort(y).CompareTo(sort(x)));
            else
                results.Sort((x, y) => sort(x).CompareTo(sort(y)));
        }
        return results;
    }
}
