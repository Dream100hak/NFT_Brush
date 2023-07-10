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
    private int _id = -1;

    public int Id { get => _id; set => _id = value; }


    [SerializeField]
    private Dictionary<int, GameLayer> _layerDics = new Dictionary<int, GameLayer>();

    public void Initialize(int id)
    {
        _id = id;
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
    public void RemoveLayer(int id)
    {
        if (_layerDics.ContainsKey(id))
            _layerDics.Remove(id);
    }
    public void ClearLayers() { _layerDics.Clear(); }


    public byte[] Serialize(DrawingInfoData ED)
    {
        byte[] bytes = null;
        using (var ms = new MemoryStream())
        {
            using (var writer = new BinaryWriter(ms))
            {
                DrawingCanvas canvas = ED.GetCanvas(_id);

                writer.Write(_id); // Canvs Id;
                writer.Write(canvas.Name); // canvas name
                writer.Write(canvas.Size.x); // canvas Size X 
                writer.Write(canvas.Size.y); // canvas Size Y           

                byte[] pixelData = canvas.Snapshot.GetRawTextureData();
                writer.Write(pixelData.Length); // Length of pixel data
                writer.Write(pixelData); // Pixel data

                // !-- ���̾� �귯�� �� ������ �����͵� -- !
                writer.Write(_layerDics.Count); // layer count

                foreach (var layer in _layerDics)
                {
                    //LAYER .. 
                    writer.Write(layer.Key); //ID  
                    writer.Write(layer.Value.Name); // NAME
                    writer.Write(layer.Value.CreationTimestamp); //���� �ð�
                    writer.Write(layer.Value.BrushDics.Count);
                    //BRUSH
                    foreach (var brush in layer.Value.BrushDics)
                    {
                        writer.Write(brush.Key);
                        writer.Write(brush.Value.BrushTypeId); // �귯�� ����
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

    public void Import(byte[] buffer, DrawingInfoData DrawingED, LayerInfoData layerED, bool isPrieView = false)
    {
        using (var ms = new MemoryStream(buffer))
        {
            using (var reader = new BinaryReader(ms))
            {
                var canvasId = reader.ReadInt32();
                var canvasName = reader.ReadString();
                var canvasSize = new Vector2Int(reader.ReadInt32(), reader.ReadInt32());

                var textureLen = reader.ReadInt32();
                byte[] pixelData = reader.ReadBytes(textureLen);
                Texture2D snapshot = new Texture2D(2, 2);
                snapshot.LoadRawTextureData(pixelData);
                snapshot.Apply();

                if(DrawingED.GetCanvas(canvasId) == null)
                {
                    DrawingCanvas drawingCanvas = new DrawingCanvas()
                    {
                        Id = canvasId,
                        Name = canvasName,
                        Size = canvasSize,
                        Snapshot = snapshot
                    };

                    DrawingED.Canvases.Add(drawingCanvas);
                }

                if (isPrieView == false)
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

                            //TODO : ���߿� ȿ�� ���ϴ�.

                        }

                        //AddLayer(layerId );
                        //  AddItem(pos, targetPalette.GetItem(id));
                    }
                }
            }
        }
    }
}