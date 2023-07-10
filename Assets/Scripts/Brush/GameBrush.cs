using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class GameBrush : MonoBehaviour
{
    [SerializeField]
    private int _id = -1; //고유 아이디
    [SerializeField]
    private int _brushTypeId = -1; // 브러시 종류를 고르는 아이디
    [SerializeField]
    private Color _color = Color.white;

    public int Id { get { return _id; } set { _id = value; }  }  
    public int BrushTypeId { get { return _brushTypeId; } set { _brushTypeId = value; }  }  
    public Color Color { get { return _color; } set { _color = value; }  }  


    public void Initialize(int id , int brushTypeId, BrushInfoData ED , GameObject curBrush)
    {
        _id = id;
        _brushTypeId = brushTypeId;
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
