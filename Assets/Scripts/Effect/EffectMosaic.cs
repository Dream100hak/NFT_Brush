using UnityEngine;

public class EffectMosaic : MonoBehaviour , IEffect
{
    public Texture2D sourceTexture;
    public float cubeSize = 0.1f;
    public float cameraWidth = 10.2f;
    public float cameraHeight = 5.7f;
    public void ApplyEffect(BrushInfoData ED)
    {
        throw new System.NotImplementedException();
    }
    private void Start()
    {
        CreateMosaic();
    }

    private void CreateMosaic()
    {
        float imageWidth = sourceTexture.width; // �̹����� ���� ũ��
        float imageHeight = sourceTexture.height; // �̹����� ���� ũ��

        int numCubesX = Mathf.RoundToInt(cameraWidth / cubeSize);
        int numCubesY = Mathf.RoundToInt(cameraHeight / cubeSize);

        int pixelGapX = Mathf.FloorToInt(imageWidth / (float)numCubesX);
        int pixelGapY = Mathf.FloorToInt(imageHeight / (float)numCubesY);

        GameObject cubePrefab = Resources.Load<GameObject>("Prefab/Cube");

        for (int y = 0; y < sourceTexture.height; y += pixelGapY)
        {
            for (int x = 0; x < sourceTexture.width; x += pixelGapX)
            {
                Color color = sourceTexture.GetPixel(x, y);
                if (color.a > 0) // alpha ���� 0���� ū ��쿡�� ť�� ����
                {
                
                    GameObject cube = Instantiate(cubePrefab); // Cube ������ �ν��Ͻ�ȭ
                    cube.transform.SetParent(transform, false);
                    cube.transform.position = new Vector3(x * cubeSize * pixelGapX / imageWidth, y * cubeSize * pixelGapY / imageHeight, 0);
                    cube.transform.localScale = new Vector3(cubeSize, cubeSize, cubeSize) * (color.r + color.g + color.b) / 3f;
                    cube.GetComponent<Renderer>().material.color = color;
                    cube.GetComponent<EffectRotator>().enabled = true;
                }
            }
        }

        transform.localPosition = new Vector3(cameraWidth / 4, cameraWidth / 4, 0);
        transform.localScale = Vector3.one * 2.5f;
    }

  
}
