using UnityEngine;

public class EffectResizer : MonoBehaviour , IEffect
{
    private const float SHRINK_TIME = 0.2f; // 큐브 충돌 후 축소하는 시간

    public void ApplyEffect(BrushInfoData ED)
    {
        throw new System.NotImplementedException();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Cube"))
        {
            StartCoroutine(Shrink());
        }
    }

    private System.Collections.IEnumerator Shrink()
    {
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