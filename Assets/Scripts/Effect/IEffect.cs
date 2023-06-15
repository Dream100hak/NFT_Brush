using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEffect 
{
    void ApplyEffect(BrushInfoData ED);
}

public interface ISpawner
{
    void ApplySpawner(BrushInfoData ED , GameObject go);
}
