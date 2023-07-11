using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class GameBrush : MonoBehaviour
{
    [SerializeField]
    private int _id = -1; //���� ���̵�
    [SerializeField]
    private int _brushTypeId = -1; // �귯�� ������ ���� ���̵�
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

        //TODO : ȿ�� ������ ���������� 
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
    public void Initialize(DataBrush brush)
    {
        _id = brush.Id;
        _brushTypeId = brush.TypeId;
        _color = new Color(brush.R, brush.G, brush.B, brush.A);

        gameObject.name = "Brush";
        gameObject.layer = LayerMask.NameToLayer("Canvas");
        transform.localScale = new Vector3(brush.ScaleX, brush.ScaleY, brush.ScaleZ);

        Renderer renderer = GetComponent<Renderer>();
        Material material = new Material(renderer.sharedMaterial);
        material.color = _color;
        renderer.sharedMaterial = material;
    }
}
