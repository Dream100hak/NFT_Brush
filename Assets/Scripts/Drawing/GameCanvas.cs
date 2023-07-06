using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;
using static UnityEditor.Progress;

public class GameCanvas : MonoBehaviour
{
    [SerializeField]
    private Dictionary<int, GameLayer> _layerDics = new Dictionary<int, GameLayer>();

  
    public void Initialize()
    {    
        gameObject.layer = LayerMask.NameToLayer("Canvas");

        BoxCollider areaCol = GetComponent<BoxCollider>();
        if (areaCol == null)
        {
            areaCol = gameObject.AddComponent<BoxCollider>();
            areaCol.isTrigger = true;
            areaCol.size = new Vector3(300, 300, 0.2f);
        }
    }

    public void AddLayer(GameLayer layer)
    { 
        _layerDics.Add(layer.Id, layer);
    }
    public void RemoveLayer(int id )
    {
        if(_layerDics.ContainsKey(id))
        _layerDics.Remove(id);
    }
    public void ClearLayers() { _layerDics.Clear(); }


    public byte[] Serialize()
    {
        byte[] bytes = null;
        using (var ms = new MemoryStream())
        {
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(_layerDics.Count);

                foreach (var layer in _layerDics)
                {
                    //LAYER .. 
                    writer.Write(layer.Key); //ID  
                    writer.Write(layer.Value.Name); // NAME
                    writer.Write(layer.Value.CreationTimestamp); //만든 시간


                    writer.Write(layer.Value.BrushDics.Count);

                    //BRUSH
                    foreach (var brush in layer.Value.BrushDics)
                    {
                        writer.Write(brush.Key);
                        writer.Write(brush.Value.BrushTypeId); // 브러시 종류
                        writer.Write(brush.Value.Color.r);
                        writer.Write(brush.Value.Color.g);
                        writer.Write(brush.Value.Color.b);
                        writer.Write(brush.Value.Color.a);
                    }
                }

                bytes = ms.ToArray();
            }
        }

        return bytes;
    }

    public void Import(byte[] buffer , LayerInfoData layerED)
    {
        using (var ms = new MemoryStream(buffer))
        {
            using (var reader = new BinaryReader(ms))
            {
                int layerCount = reader.ReadInt32();

                for (int i = 0; i < layerCount; i++)
                {
                    //LAYER .. 
                    var layerId = reader.ReadInt32();
                    var layerName = reader.ReadInt32();
                    var timeStamp = reader.ReadInt32();

                    int brushCount = reader.ReadInt32();
                    //BRUSH
                    for (int j = 0; j < brushCount; j++)
                    {
                        var brushId = reader.ReadInt32();
                        var brushTypeId = reader.ReadInt32();
                        var brushR = reader.ReadInt32();
                        var brushG = reader.ReadInt32();
                        var brushV = reader.ReadInt32();
                        var brushA = reader.ReadInt32();

                        //TODO : 나중에 효과 들어갑니다.
                        
                    }

                    //AddLayer(layerId );
                    //  AddItem(pos, targetPalette.GetItem(id));
                }
            }
        }
    }
}