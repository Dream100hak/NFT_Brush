using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;

public class BrushInfo :  InfoData<BrushInfoData>
{
    public static Dictionary<int, GameBrush> brushObjects { get; set; } = new Dictionary<int, GameBrush>();

    public static GameObject CurrentBrush { get => ED.Brushes[ED.GetSelectedBrushId()].TargetObj; }
    private static Transform s_parent => GetBrushParent();
    public static Transform BrushParent { get => s_parent; }

    public static Transform GetBrushParent()
    { 
        var parent = UnityEngine.Object.FindObjectOfType<FitCanvas>();
        return parent != null ? parent.transform : null;
    }

    public static void DrawGridBrush(Vector2 slotSize)
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

        if (LayerInfo.LayerObjects.Count == 0 || LayerInfo.ED.SelectedLayerIds.Any() == false)
            return;

        foreach(int selectedLayerId in LayerInfo.ED.SelectedLayerIds)
        {
            GameObject brushObj = GameObject.Instantiate(CurrentBrush, position, Quaternion.identity) as GameObject;
            GameLayer parentLayer = cubeParent.GetComponentsInChildren<GameLayer>().FirstOrDefault(t => t != null && t.Id == selectedLayerId);

            if (parentLayer == null)
                return;

            int newBrushId = NewGenerateId(brushObjects);

            brushObj.transform.SetParent(parentLayer.transform);
            GameBrush newBrush = brushObj.GetOrAddComponent<GameBrush>();
            newBrush.Initialize(newBrushId, parentLayer.Id, ED, CurrentBrush);

            brushObjects.Add(newBrushId, newBrush);

            parentLayer.HasChanged = true;
            parentLayer.ChildBrushIds.Add(newBrushId);

            Undo.RegisterCreatedObjectUndo(newBrush, "Paint Brush");

            Utils.AddUndo("Paint Brush", () =>
            {
                var destroyedBrushes = brushObjects.Where(x => x.Value == null).Select(x => x.Key).ToList();
                if (destroyedBrushes.Count > 0)
                {
                    foreach (int id in destroyedBrushes)
                    {
                        ToDeleteIds.Add(id);
                        EmptyGenerateIds.Add(id);
                    }
                }
            });
        }

    }

    public static void RemoveBrush(RaycastHit hitInfo)
    {
        if (LayerInfo.LayerObjects.Count == 0 || LayerInfo.ED.SelectedLayerIds.Any() == false)
            return;

        if ( hitInfo.collider != null && hitInfo.collider.gameObject.layer == LayerMask.NameToLayer("Canvas"))
        {
            GameObject cube = hitInfo.collider.gameObject;
            Undo.DestroyObjectImmediate(cube);
            cube.transform.parent.GetComponent<GameLayer>().HasChanged = true;
        }
    }
}
