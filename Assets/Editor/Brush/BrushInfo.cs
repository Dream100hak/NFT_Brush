using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
public class BrushInfo :  InfoData<BrushInfoData>
{
    public static GameObject CurrentBrush { get => ED.TypeBrushes[ED.GetTypeBrushId()].TargetObj; }

    public static void ClearHandler()
    {
        ED.DataBrushes.Clear();
        ED.BrushObjects.Clear();
    }
    public static void DrawGridBrush(Vector2 slotSize)
    {
        if (ED == null && ED.TypeBrushes.Count == 0)
            return;

        int selectedBrushId = ED.GetTypeBrushId();
        selectedBrushId = selectedBrushId == -1 ? 0 : selectedBrushId;
 
        EditorHelper.DrawGridBrushItems( 5, ED.TypeBrushes.Count, (idx) =>
        {
            bool selected = DrawGridBrushItems(slotSize, selectedBrushId == idx, ED.TypeBrushes[idx]);

            if (selected)
            {
                selectedBrushId = idx;
            }
        });

        ED.SetTypeBrushById(selectedBrushId);
    }
    private static bool DrawGridBrushItems(Vector2 slotSize, bool isSelected, DrawingBrush item)
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
        Transform gameCanvas = DrawingInfo.GameCanvas.transform;

        if (LayerInfo.ED.LayerObjects.Count == 0 || LayerInfo.ED.SelectedLayerIds.Any() == false)
            return;

        foreach(int selectedLayerId in LayerInfo.ED.SelectedLayerIds)
        {
            GameObject brushObj = GameObject.Instantiate(CurrentBrush, position, Quaternion.identity) as GameObject;
            GameLayer gameLayer = gameCanvas.GetComponentsInChildren<GameLayer>().FirstOrDefault(t => t != null && t.Id == selectedLayerId);

            if (gameLayer == null)
                return;

            int newBrushId = NewGenerateId(ED.BrushObjects);

            brushObj.transform.SetParent(gameLayer.transform);
            GameBrush newBrush = brushObj.GetOrAddComponent<GameBrush>();
            newBrush.Initialize(newBrushId, ED.GetTypeBrushId(), gameLayer.Id , ED, CurrentBrush);

            ED.BrushObjects.Add(newBrushId, newBrush);

            gameLayer.HasChanged = true;

            Undo.RegisterCreatedObjectUndo(brushObj, "Paint Brush");
        }
    }
    public static void PaintBrush(DataBrush brush)
    {
        Vector3 newPos = new Vector3(brush.PosX, brush.PosY, brush.PosZ);
        GameObject selectedBrush = ED.TypeBrushes[brush.TypeId].TargetObj;

        GameObject brushObj = GameObject.Instantiate(selectedBrush, newPos, Quaternion.identity) as GameObject;
        GameLayer gameLayer = LayerInfo.ED.LayerObjects[brush.ParentLayer];

        if (gameLayer == null)
            return;

        brushObj.transform.SetParent(gameLayer.transform);
        GameBrush newBrush = brushObj.GetOrAddComponent<GameBrush>();
        newBrush.Initialize(brush);

        ED.BrushObjects.Add(brush.Id, newBrush);

        gameLayer.HasChanged = true;

    }
    public static void LoadBrush()
    {
        foreach(DataBrush brush in ED.DataBrushes)
        {
            PaintBrush(brush);
        }
    }

    public static void RemoveBrush(RaycastHit hitInfo)
    {
        if (LayerInfo.ED.LayerObjects.Count == 0 || LayerInfo.ED.SelectedLayerIds.Any() == false)
            return;

        if ( hitInfo.collider != null && hitInfo.collider.gameObject.layer == LayerMask.NameToLayer("Canvas"))
        {
            GameObject cube = hitInfo.collider.gameObject;
            Undo.DestroyObjectImmediate(cube);
            cube.transform.parent.GetComponent<GameLayer>().HasChanged = true;
        }
    }

    public static void ClearAllSelectedBrush()
    {
        if (LayerInfo.ED.LayerObjects.Count == 0 || LayerInfo.ED.SelectedLayerIds.Any() == false)
            return;

        foreach(var brushes in ED.BrushObjects)
        {
            brushes.Value.IsSelected = false;
        }
    }
}
