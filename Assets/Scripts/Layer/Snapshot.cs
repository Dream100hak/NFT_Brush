using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Snapshot
{
    public static Texture2D CaptureLayerSnapshot()  // ALL 
    {
        Camera tempCamera = SettingSnapshotCam();
        Texture2D snapshot = GetSnapshot(tempCamera);

        return snapshot;
    }
    public static Texture2D CaptureLayerSnapshot(GameCanvas canvas, GameLayer layer) // 각각의 레이어
    {
        if (layer.HasChanged == false && layer.SnapShot != null)
            return layer.SnapShot;

        Camera tempCamera = SettingSnapshotCam();

        List<bool> layerStates = new List<bool>();

        for (int i = 0; i < canvas.transform.childCount; i++)
        {
            Transform childLayer = canvas.transform.GetChild(i);

            layerStates.Add(childLayer.gameObject.activeSelf);

            if (childLayer != layer.transform)
                childLayer.gameObject.SetActive(false);

        }

        Texture2D snapshot = GetSnapshot(tempCamera);

        for (int i = 0; i < canvas.transform.childCount; i++)
        {
            Transform childLayer = canvas.transform.GetChild(i);
            childLayer.gameObject.SetActive(layerStates[i]);
        }

        layer.HasChanged = false;
        layer.SnapShot = snapshot;

        return snapshot;
    }
    public static Camera SettingSnapshotCam()
    {
        Camera tempCamera = new GameObject("TempCamera").AddComponent<Camera>();
        Camera mainCamera = Camera.main;

        tempCamera.transform.position = mainCamera.transform.position;
        tempCamera.transform.rotation = mainCamera.transform.rotation;
        tempCamera.orthographic = true;
        tempCamera.orthographicSize = mainCamera.orthographicSize;
        tempCamera.aspect = mainCamera.aspect;
        tempCamera.clearFlags = CameraClearFlags.SolidColor;
        tempCamera.backgroundColor = Color.clear;
        tempCamera.cullingMask = mainCamera.cullingMask;

        tempCamera.cullingMask = 1 << LayerMask.NameToLayer("Canvas");

        return tempCamera;
    }
    public static RenderTexture SettingRenderTex(Camera tempCamera, out GameObject gridQuad)
    {
        int width = 50;
        int height = 50;

        RenderTexture renderTexture = new RenderTexture(width, height, 24);
        gridQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        gridQuad.layer = LayerMask.NameToLayer("Canvas");
        gridQuad.transform.position = new Vector3(0, 0, 10);
        gridQuad.transform.rotation = tempCamera.transform.rotation;
        gridQuad.transform.localScale = new Vector3(40, 40, 1.0f);

        gridQuad.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/Transparent"));
        gridQuad.GetComponent<Renderer>().sharedMaterial.mainTexture = Resources.Load<Texture2D>("Textures/Grid");
        gridQuad.GetComponent<Renderer>().sharedMaterial.color = Color.gray;
        tempCamera.targetTexture = renderTexture;
        tempCamera.Render();

        return renderTexture;
    }

    public static Texture2D GetSnapshot(Camera tempCamera)
    {
        int width = 50;
        int height = 50;

        GameObject gridQuad;
        RenderTexture renderTexture = SettingRenderTex(tempCamera, out gridQuad);
        RenderTexture.active = renderTexture;
        Texture2D snapshot = new Texture2D(width, height, TextureFormat.RGB24, false);
        snapshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        snapshot.Apply();

        RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(tempCamera.gameObject);
        UnityEngine.Object.DestroyImmediate(renderTexture);
        UnityEngine.Object.DestroyImmediate(gridQuad);
        return snapshot;
    }
}
