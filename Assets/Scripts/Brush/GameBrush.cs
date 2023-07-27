using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class GameBrush : MonoBehaviour
{
    [SerializeField]
    private int _id = -1; //고유 아이디
    [SerializeField]
    private int _brushTypeId = -1; // 브러시 종류를 고르는 아이디
    [SerializeField]
    private Color _color = Color.white;
    [SerializeField]
    private int _parentLayer = -1; //레이어 아이디
    [SerializeField]
    private bool _isSelected = false;

    Material _defaultMat;
    Material _outlineMat;

    public int Id { get { return _id; } set { _id = value; }  }  
    public int BrushTypeId { get { return _brushTypeId; } set { _brushTypeId = value; }  }  
    public Color Color { get { return _color; } set { _color = value; }  }  
    public int ParentLayer { get { return _parentLayer; } set { _parentLayer = value; }  }  
    public bool IsSelected { get { return _isSelected; } 
        set 
        {
            _isSelected = value;
            Material[] materials = new Material[2];
            if (_isSelected)
            {
                materials[0] = _defaultMat;
                materials[1] = _outlineMat;
            }
            else
            {
                materials[0] = _defaultMat;
            }
 
            CreateOutline(materials);
        }  
    }


    public void Initialize(int id , int brushTypeId, int parentLayer,  BrushInfoData ED , GameObject curBrush)
    {
        _id = id;
        _brushTypeId = brushTypeId;
        _color = ED.BrushColor;
        _parentLayer = parentLayer;

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

        _defaultMat = renderer.sharedMaterial;
        _outlineMat = Resources.Load<Material>("Materials/Outline");
    }
    public void Initialize(DataBrush brush)
    {
        _id = brush.Id;
        _brushTypeId = brush.TypeId;
        _color = new Color(brush.R, brush.G, brush.B, brush.A);
        _parentLayer = brush.ParentLayer;

        gameObject.name = "Brush";
        gameObject.layer = LayerMask.NameToLayer("Canvas");
        transform.localScale = new Vector3(brush.ScaleX, brush.ScaleY, brush.ScaleZ);

        Renderer renderer = GetComponent<Renderer>();
        Material material = new Material(renderer.sharedMaterial);
        material.color = _color;
        renderer.sharedMaterial = material;

        _defaultMat = renderer.sharedMaterial;
        _outlineMat = Resources.Load<Material>("Materials/Outline");
    }

    private void CreateOutline(Material[] materials)
    {
        Renderer renderer = GetComponent<Renderer>();
        renderer.materials = materials;
    }

}
