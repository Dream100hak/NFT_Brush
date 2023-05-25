using System.Linq;
using UnityEditor;
using UnityEngine;

public class BrushEditor
{

    private static BrushEditorData s_editorData => GetCubeData();
    public static BrushEditorData ED { get => s_editorData; }

    private static bool s_isPlacing;
    public static bool IsPlacing { get => s_isPlacing; }
    private static GameObject s_cubePrefab => GetCubePrefab();
    public static GameObject CubePrefab { get => s_cubePrefab; }
    private static Transform s_cubeParent => GetCubeParent();
    public static Transform CubeParent { get => s_cubeParent; }

    public static BrushEditorData GetCubeData() { return Resources.Load<BrushEditorData>("Data/BrushData");  }
    private static GameObject GetCubePrefab()
    {
        var cubePlacer = UnityEngine.Object.FindObjectOfType<CubePlacer>();
        return cubePlacer != null ? cubePlacer.CubePrefab : null;
    }

    public static Transform GetCubeParent()
    { 
        var parent = UnityEngine.Object.FindObjectOfType<CubePlacer>();
        return parent != null ? parent.transform : null;
    }

    public static void PlaceCube(Vector3 position)
    {
        Transform cubeParent = GetCubeParent();
        var layerWindow = EditorWindow.GetWindow<LayerWindow>();

        if (LayerEditor.ED.SelectedLayerIds.Any() == false)
            return;

        foreach(int selectedLayerId in LayerEditor.ED.SelectedLayerIds)
        {
            GameObject cube = GameObject.Instantiate(s_cubePrefab, position, Quaternion.identity) as GameObject;
            Transform target = cubeParent.Cast<Transform>().FirstOrDefault(t => t.GetComponent<LayerData>() != null && t.GetComponent<LayerData>().Id == selectedLayerId);

            if (target == null)
                return;

            cube.name = "Cube";
            cube.layer = LayerMask.NameToLayer("Canvas");
            cube.transform.SetParent(target);
            cube.transform.localScale = Vector3.one * s_editorData.CubeSize;

            cube.GetComponent<CubeRotator>().enabled = ED.RotatorEnabled;
            cube.GetComponent<CubeRotator>().RotationSpeed = ED.Random_RotSpeed;

            if (ED.MoverEnabled)
            {
                cube.GetComponent<CubeStraight>().enabled = ED.StraightEnabled;
                cube.GetComponent<CubeStraight>().MoveSpeed = ED.Straight_MoveSpeed;
                cube.GetComponent<CubeStraight>().MoveDirection = ED.Straight_MoveDirection;

                cube.GetComponent<CubeBlackhole>().enabled = ED.BlackholeEnabled;
                cube.GetComponent<CubeBlackhole>().AttractionForce = ED.Blackhole_AttractionForce;

                cube.GetComponent<CubeSnow>().enabled = ED.SnowEnabled;
                cube.GetComponent<CubeSnow>().SwayIntensity = ED.Snow_SwayIntensity;
                cube.GetComponent<CubeSnow>().SwayAmount = ED.Snow_SwayAmount;
            }

            if (ED.NatureEnabled)
            {
                cube.GetComponent<SnowSpawner>().enabled = ED.SnowSpawnEnabled;
                cube.GetComponent<SnowSpawner>().CubeSnowPrefab = s_cubePrefab;
                cube.GetComponent<SnowSpawner>().SwayAmount = ED.SnowSpawn_SwayAmount;
                cube.GetComponent<SnowSpawner>().SwayIntensity = ED.SnowSpawn_SwayIntensity;

            }

            LayerEditor.LayerObjects[selectedLayerId].GetComponent<LayerData>().HasChanged = true;

            Renderer renderer = cube.GetComponent<Renderer>();
            Material material = new Material(renderer.sharedMaterial);
            material.color = s_editorData.CubeColor;
            renderer.sharedMaterial = material;
            Undo.RegisterCreatedObjectUndo(cube, "Create Cube");
        }

    }

    public static void RemoveCube(RaycastHit hitInfo)
    {
        if (hitInfo.collider != null && hitInfo.collider.gameObject.layer == LayerMask.NameToLayer("Canvas"))
        {
            GameObject cube = hitInfo.collider.gameObject;
            Undo.DestroyObjectImmediate(cube);

            //TODO : ID로 관리 필요
            cube.transform.parent.GetComponent<LayerData>().HasChanged = true;
        }
    }

    public static void EnablePlacing()
    {
        s_isPlacing = true;
        Tools.current = Tool.None;
    }

    public static void DisablePlacing()
    {
        s_isPlacing = false;
        Tools.current = Tool.Move;
    }
}
