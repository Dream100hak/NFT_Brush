using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class GameBrush : MonoBehaviour
{
    public Color _color = Color.white;
    public int _id = -1;
    public int _parentLayerId = -1;
    public GameObject _currentBrush;

    public void Initialize(int id , int parentLayerId , BrushInfoData ED , GameObject curBrush)
    {
        _id = id;
        _parentLayerId = parentLayerId;
        _currentBrush = curBrush;
        _color = ED.BrushColor;
        
        gameObject.name = "Brush";
        gameObject.layer = LayerMask.NameToLayer("Canvas");    
        transform.localScale = Vector3.one * ED.BrushSize;

        EffectStraight effStraight = gameObject.GetOrAddComponent<EffectStraight>();
        EffectBlackhole effBlackhole = gameObject.GetOrAddComponent<EffectBlackhole>();
        EffectSnow effSnow = gameObject.GetOrAddComponent<EffectSnow>();
        SpawnerSnow spawnerSnow = gameObject.GetOrAddComponent<SpawnerSnow>();

        //TODO : 효과 저장은 마지막으로 
        if (ED.MoverEnabled)
        {
            effStraight.ApplyEffect(ED);
            effBlackhole.ApplyEffect(ED);
            effSnow.ApplyEffect(ED);
        }

        if (ED.NatureEnabled)
        {
            spawnerSnow.ApplySpawner(ED, curBrush);
        }

        Renderer renderer = GetComponent<Renderer>();
        Material material = new Material(renderer.sharedMaterial);
        material.color = ED.BrushColor;
        renderer.sharedMaterial = material;
    }
}
