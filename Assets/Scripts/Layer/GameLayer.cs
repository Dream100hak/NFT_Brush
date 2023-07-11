using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GameLayer : MonoBehaviour
{
 
    [SerializeField]
    private int _id = -1;
    public int Id { get => _id; set { _id = value; } }
    [SerializeField]
    private long _creationTimestamp = -1;

    public long CreationTimestamp { get => _creationTimestamp; set { _creationTimestamp = value; } }

    [SerializeField]
    private string _name = string.Empty;
    public string Name { 
        get => _name; 
        set 
        {
            if (_name != value)
            {
                _name = value;
                gameObject.name = value;
            }
           
        }
    }
    [SerializeField]
    private Rect _layerRect = new Rect();
    public Rect LayerRect
    {
        get => _layerRect;
        set
        {
            if (_layerRect != value)
            _layerRect = value;
        }
    }

    [SerializeField]
    private bool _hasChanged = false;
    public bool HasChanged
    {
        get => _hasChanged;
        set
        {
            if (_hasChanged != value)
                _hasChanged = value;
            
        }
    }

    [SerializeField]
    private Texture2D _snapShot;

    public Texture2D SnapShot
    {
        get => _snapShot;
        set
        {
            if (_snapShot != value)
                _snapShot = value;
            
        }
    }

    private Dictionary<int, GameBrush> _brushDics = new Dictionary<int, GameBrush>();
    public Dictionary<int, GameBrush> BrushDics { get => _brushDics; }


    public void AddBrush(GameBrush brush)
    {
        _brushDics.Add(brush.Id, brush);
    }
    public void RemoveBrush(int id)
    {
        if(_brushDics.ContainsKey(id))
            _brushDics.Remove(id);
    }

    public void Initialize(int newId, string newName, long timestamp = -1 )
    {
        Id = newId;
        CreationTimestamp = timestamp == -1 ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : timestamp;
        Name = newName;
        HasChanged = true;
    }

    public void RemoveChildBrush()
    {

    }

}
