using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Brush : MonoBehaviour
{
    public float brushSize = 0.1f;
    public Color brushColor = Color.red;
    public float brushStrength = 0.5f;
    public float brushSpeed = 5.0f;

    private Material material;
    private Vector2 previousUV;

    void Start()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        material = meshRenderer.material;
        previousUV = new Vector2(0.5f, 0.5f);
    }

    void Update()
    {
        Vector2 uv = Input.mousePosition;
        uv /= new Vector2(Screen.width, Screen.height);

        Vector2 deltaUV = uv - previousUV;

        float distance = deltaUV.magnitude;
        if (distance > 0)
        {
            int steps = Mathf.CeilToInt(distance / brushSize);
            for (int i = 0; i < steps; i++)
            {
                Vector2 currentUV = Vector2.Lerp(previousUV, uv, (float)i / steps);
                material.SetVector("_BrushParams", new Vector4(currentUV.x, currentUV.y, brushStrength, brushSize));
                material.SetColor("_BrushColor", brushColor);
                material.SetFloat("_Time", Time.time * brushSpeed);
                Graphics.DrawMesh(GetComponent<MeshFilter>().mesh, transform.localToWorldMatrix, material, 0);
            }
        }

        previousUV = uv;
    }
}
