using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static UnityEditor.Progress;

public class BrushInfo
{
    private static BrushInfoData s_editorData => GetBrushData();
    public static BrushInfoData ED { get => s_editorData; }

    private static bool s_isPlacing;
    public static bool IsPlacing { get => s_isPlacing; }
    public static GameObject CurrentBrush { get => ED.Brushes[ED.GetSelectedBrushId()].TargetObj ; }
    private static Transform s_parent => GetBrushParent();
    public static Transform BrushParent { get => s_parent; }
    public static BrushInfoData GetBrushData() { return Resources.Load<BrushInfoData>("Data/BrushInfoData");  }

    public static Transform GetBrushParent()
    { 
        var parent = UnityEngine.Object.FindObjectOfType<BrushPlacer>();
        return parent != null ? parent.transform : null;
    }

    public static void DrawGridBrush(float areaWidth , Vector2 slotSize)
    {
        if (ED == null && ED.Brushes.Count == 0)
            return;

        int selectedBrushId = ED.GetSelectedBrushId();
        selectedBrushId = selectedBrushId == -1 ? 0 : selectedBrushId;
 
        EditorHelper.DrawGridBrushItems( 5, ED.Brushes.Count, (idx) =>
        {
            bool selected = DrawGridBrushItems(slotSize, selectedBrushId == idx, ED.Brushes[idx]);

            if (selected)
            {
                selectedBrushId = idx;
            }
        });

        ED.SetSelectedBrushId(selectedBrushId);
    }
    private static bool DrawGridBrushItems(Vector2 slotSize, bool isSelected, Brush item)
    {
        var area = GUILayoutUtility.GetRect(slotSize.x, slotSize.y, GUIStyle.none, GUILayout.MaxWidth(slotSize.x), GUILayout.MaxHeight(slotSize.y));
        item.Selected = Utils.EditPropertyWithUndo(item.Name, item.Selected, newSelected => item.Selected = newSelected, (label, value) => GUI.Button(area, label, EditorHelper.SelectedBrushButton(item.TargetObj, value)), ED);

        if (isSelected)
        {
            var selectMarkArea = area;
            selectMarkArea.x = selectMarkArea.center.x - 20f;
            selectMarkArea.width = 20;
            selectMarkArea.height = 20;
            GUI.DrawTexture(selectMarkArea, EditorGUIUtility.FindTexture("d_FilterSelectedOnly@2x"));
        }
        return item.Selected;
    }

    public static void PaintBrush(Vector3 position)
    {
        Transform cubeParent = GetBrushParent();
        var layerWindow = EditorWindow.GetWindow<LayerWindow>();

        if (LayerInfo.ED.SelectedLayerIds.Any() == false)
            return;

        foreach(int selectedLayerId in LayerInfo.ED.SelectedLayerIds)
        {
            GameObject brush = GameObject.Instantiate(CurrentBrush, position, Quaternion.identity) as GameObject;
            Transform target = cubeParent.Cast<Transform>().FirstOrDefault(t => t.GetComponent<Layer>() != null && t.GetComponent<Layer>().Id == selectedLayerId);

            if (target == null)
                return;

            brush.name = "Brush";
            brush.layer = LayerMask.NameToLayer("Canvas");
            brush.transform.SetParent(target);
            brush.transform.localScale = Vector3.one * s_editorData.BrushSize;

            EffectStraight effStraight = brush.GetOrAddComponent<EffectStraight>();
            EffectBlackhole effBlackhole = brush.GetOrAddComponent<EffectBlackhole>();
            EffectSnow effSnow = brush.GetOrAddComponent<EffectSnow>();
            SpawnerSnow spawnerSnow = brush.GetOrAddComponent<SpawnerSnow>();

            if (ED.MoverEnabled)
            {
                effStraight.ApplyEffect(ED);
                effBlackhole.ApplyEffect(ED);
                effSnow.ApplyEffect(ED);
            }

            if (ED.NatureEnabled)
            {
                spawnerSnow.ApplySpawner(ED , CurrentBrush);
            }

            LayerInfo.LayerObjects[selectedLayerId].GetComponent<Layer>().HasChanged = true;

            Renderer renderer = brush.GetComponent<Renderer>();
            Material material = new Material(renderer.sharedMaterial);
            material.color = s_editorData.BrushColor;
            renderer.sharedMaterial = material;
            Undo.RegisterCreatedObjectUndo(brush, "Create Brush");
        }

    }

    public static void RemoveBrush(RaycastHit hitInfo)
    {
        if (hitInfo.collider != null && hitInfo.collider.gameObject.layer == LayerMask.NameToLayer("Canvas"))
        {
            GameObject cube = hitInfo.collider.gameObject;
            Undo.DestroyObjectImmediate(cube);
            cube.transform.parent.GetComponent<Layer>().HasChanged = true;
        }
    }
}
