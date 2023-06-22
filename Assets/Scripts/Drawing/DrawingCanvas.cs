using UnityEditor;
using UnityEngine;

public class DrawingCanvas : MonoBehaviour
{
    public void Initialize()
    {    
        BoxCollider areaCol = GetComponent<BoxCollider>();
        if (areaCol == null)
            areaCol = gameObject.AddComponent<BoxCollider>();

        areaCol.isTrigger = true;
        areaCol.size = new Vector3(300, 300, 0.2f);
    }
}