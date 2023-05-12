using UnityEngine;

public class CubeResizer : MonoBehaviour
{
    private const float SHRINK_TIME = 0.2f; // ť�� �浹 �� ����ϴ� �ð�

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Cube"))
        {
            StartCoroutine(Shrink());
        }
    }

    private System.Collections.IEnumerator Shrink()
    {
        const float SHRINK_SPEED = 1.0f / SHRINK_TIME; // ť�� ��� �ӵ�

        float time = 0f;
        Vector3 originalScale = transform.localScale;

        while (time < SHRINK_TIME)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / SHRINK_TIME);
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t * t * t * (6f * t * t - 15f * t + 10f));
            yield return null;
        }
    }
}