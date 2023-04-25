using UnityEngine;

[CreateAssetMenu(fileName = "CubePlacerEditorData", menuName = "CubePlacerEditor/Data", order = 1)]
public class CubePlacerEditorData : ScriptableObject
{
    public float CubeSize = 0.5f;
    public float PlacementDistance  = 1.0f;
    public Color CubeColor = Color.white;

    public bool RotatorEnabled = false;

    public bool MoverEnabled = false;

    public bool StraightEnabled = false; 
    public bool BlackholeEnabled= false;
    public bool SnowEnabled = false;

    public float MoveSpeed = 1.0f;
    public E_Direction MoveDirection = E_Direction.Down;
    public float RotSpeed = 1.0f;
    public float AttractionForce = 1.0f;

    public float SwayIntensity = 1.0f;
    public float SwayAmount = 0.1f;
}
