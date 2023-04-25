using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayerData : MonoBehaviour
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
}
