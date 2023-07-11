using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

public class DataLoader 
{
    public static byte[] Serialize(DataCanvas canvas, DrawingInfoData DrawingED , LayerInfoData LayerED, BrushInfoData BrushED)
    {
        byte[] bytes = null;
        using (var ms = new MemoryStream())
        {
            using (var writer = new BinaryWriter(ms))
            {
                //DataCanvas canvas = ED.GetCanvasById(_id);

                writer.Write(canvas.Id); // Canvs Id;
                writer.Write(canvas.Name); // canvas name
                writer.Write(canvas.Size.x); // canvas Size X 
                writer.Write(canvas.Size.y); // canvas Size Y           

                if (canvas.Snapshot != null)
                {
                    byte[] pixelData = canvas.Snapshot.GetRawTextureData();
                    writer.Write(pixelData.Length);
                    writer.Write(pixelData);
                }
                else
                {
                    writer.Write(0);
                }

                // !-- 레이어 브러쉬 등 실질적 데이터들 -- !
                writer.Write(LayerED.LayerObjects.Count);

                foreach (var layer in LayerED.LayerObjects)
                {
                    //LAYER .. 
                    writer.Write(layer.Key);
                    writer.Write(layer.Value.Name);
                    writer.Write(layer.Value.CreationTimestamp);
                    writer.Write(layer.Value.BrushDics.Count);
                    //BRUSH
                    foreach (var brush in layer.Value.BrushDics)
                    {
                        writer.Write(brush.Key);
                        writer.Write(brush.Value.BrushTypeId);
                        writer.Write(brush.Value.Color.r);
                        writer.Write(brush.Value.Color.g);
                        writer.Write(brush.Value.Color.b);
                        writer.Write(brush.Value.Color.a);
                        writer.Write(brush.Value.transform.position.x);
                        writer.Write(brush.Value.transform.position.y);
                        writer.Write(brush.Value.transform.position.z);
                        writer.Write(brush.Value.transform.localScale.x);
                        writer.Write(brush.Value.transform.localScale.y);
                        writer.Write(brush.Value.transform.localScale.z);
                        writer.Write(layer.Key);
                    }
                }

                bytes = ms.ToArray();
            }
        }
        return bytes;
    }

    public static void Import(ref GameCanvas gameCanvas, byte[] buffer,  DrawingInfoData DrawingED, LayerInfoData layerED, BrushInfoData brushED, bool isPreView = false)
    {
        using (var ms = new MemoryStream(buffer))
        {
            using (var reader = new BinaryReader(ms))
            {
                var canvasId = reader.ReadInt32();
                var canvasName = reader.ReadString();
                var canvasSize = new Vector2Int(reader.ReadInt32(), reader.ReadInt32());

                var pixelLen = reader.ReadInt32();
                byte[] pixelData = reader.ReadBytes(pixelLen);
                Texture2D snapshot = null;

                if (pixelLen > 0)
                {
                    snapshot = new Texture2D(50, 50, TextureFormat.RGB24, false);
                    snapshot.LoadRawTextureData(pixelData);
                    snapshot.Apply();
                }

                DataCanvas drawingCanvas = new DataCanvas() { Id = canvasId, Name = canvasName, Size = canvasSize, Snapshot = snapshot };
                gameCanvas.Initialize(drawingCanvas);


                if (DrawingED.GetCanvasById(canvasId) == null)
                    DrawingED.Canvases.Add(drawingCanvas);

                if (isPreView == false)
                {
                    int layerCount = reader.ReadInt32();

                    for (int i = 0; i < layerCount; i++)
                    {
                        //LAYER .. 
                        var layerId = reader.ReadInt32();
                        var layerName = reader.ReadString();
                        var timeStamp = reader.ReadInt64();

                        DataLayer layer = new DataLayer
                        {
                            Id = layerId,
                            Name = layerName,
                            CreateTimestamp = timeStamp
                        };

                        layerED.Layers.Add(layer);

                        int brushCount = reader.ReadInt32();
                        //BRUSH
                        for (int j = 0; j < brushCount; j++)
                        {
                            var brushId = reader.ReadInt32();
                            var brushTypeId = reader.ReadInt32();
                            var brushR = reader.ReadSingle();
                            var brushG = reader.ReadSingle();
                            var brushB = reader.ReadSingle();
                            var brushA = reader.ReadSingle();
                            var posX = reader.ReadSingle(); var posY = reader.ReadSingle(); var posZ = reader.ReadSingle();
                            var scaleX = reader.ReadSingle(); var scaleY = reader.ReadSingle(); var scaleZ = reader.ReadSingle();
                            var parentLayer = reader.ReadInt32();

                            DataBrush brush = new DataBrush()
                            {
                                Id = brushId,
                                TypeId = brushTypeId,
                                R = brushR,
                                G = brushG,
                                B = brushB,
                                A = brushA,
                                PosX = posX,
                                PosY = posY,
                                PosZ = posZ,
                                ScaleX = scaleX,
                                ScaleY = scaleY,
                                ScaleZ = scaleZ,
                                ParentLayer = parentLayer
                            };

                            brushED.DataBrushes.Add(brush);

                            //TODO : 나중에 효과 들어갑니다.

                        }
                    }
                }
            }
        }
    }
}
