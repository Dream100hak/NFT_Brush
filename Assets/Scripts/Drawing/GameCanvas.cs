using System;
using System.Collections.Generic;
using System.Data;
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

    private string _canvasName = string.Empty;
    public string CanvasName { get => _canvasName; set => _canvasName = value; }

    public void Initialize(int id)
    {
        _id = id;
        MakeCollider();
    }
    public void Initialize(DataCanvas canvas )
    {
        _id = canvas.Id;
        _canvasName = canvas.Name;
        MakeCollider();
    }

    private void MakeCollider()
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
}