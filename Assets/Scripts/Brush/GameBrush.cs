using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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

    [SerializeField]
    private float _outlineScale = -1.2f;
    [SerializeField]
    private Color _outlineColor = Color.green;
    private Renderer _outlineRenderer;

    public int Id { get { return _id; } set { _id = value; }  }  
    public int BrushTypeId { get { return _brushTypeId; } set { _brushTypeId = value; }  }  
    public Color Color { get { return _color; } set { _color = value; }  }  
    public int ParentLayer { get { return _parentLayer; } set { _parentLayer = value; }  }  
    public bool IsSelected { get { return _isSelected; } 
        set 
        {
            _isSelected = value;

            if(_outlineRenderer == null)
                _outlineRenderer = CreateOutline();
            
            _outlineRenderer.enabled = value;
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
    }

    public Renderer CreateOutline()
    {
        GameObject outlineGo = Instantiate(this.gameObject, transform.position, transform.rotation, transform);
        Renderer renderer = outlineGo.GetComponent<Renderer>();

        renderer.sharedMaterial = Resources.Load<Material>("Materials/Outline") ;
        renderer.sharedMaterial.SetColor("_OutlineColor", _outlineColor);
        renderer.sharedMaterial.SetFloat("_Scale", _outlineScale);
        renderer.shadowCastingMode = ShadowCastingMode.Off;

        outlineGo.GetComponent<Collider>().enabled = false;

        renderer.enabled = false;

        return renderer;
    }
}
