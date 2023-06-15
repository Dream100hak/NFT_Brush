using UnityEngine;

public class EffectBlackhole : MonoBehaviour , IEffect
{
    private Transform _blackholeCenter;
    private float _attractionForce = 1.0f;
    public float AttractionForce { get => _attractionForce; set { _attractionForce = value; } }
    public float _minScale = 0.01f;

    private Vector3 _startPosition;
    private Vector3 _startScale;
    private Renderer _cubeRenderer;
    public void ApplyEffect(BrushInfoData ED)
    {
        enabled = ED.BlackholeEnabled;
        AttractionForce = ED.Blackhole_AttractionForce;
    }

    void Start()
    {
        _blackholeCenter = GameObject.Find("Circle").transform;

        _startPosition = transform.position;
        _startScale = transform.localScale;
        _cubeRenderer = GetComponent<Renderer>();
    }

    void Update()
    {
        // 큐브를 중심점으로 빨려들어가게 함
        Vector3 directionToCenter = (_blackholeCenter.position - transform.position).normalized;
        transform.position += directionToCenter * _attractionForce * Time.deltaTime;

        // 거리에 따라 크기를 조절
        float distanceToCenter = Vector3.Distance(transform.position, _blackholeCenter.position);
        float scaleMultiplier = distanceToCenter / (_startPosition - _blackholeCenter.position).magnitude;
        transform.localScale = _startScale * scaleMultiplier;

        // 거리에 따라 투명도를 조절
        //float alpha = Mathf.Pow(scaleMultiplier, 2); 
      //  float alpha = scaleMultiplier;
        float alpha = Mathf.Sqrt(scaleMultiplier); // 루트를 적용하여 투명도가 더 천천히 증가하도록 함
        Color currentColor = _cubeRenderer.material.color;
        currentColor.a = alpha;
        _cubeRenderer.material.color = currentColor;

        // 크기가 너무 작아졌으면 초기 위치로 돌아가게 함
        if (transform.localScale.magnitude < _minScale)
        {
            transform.position = _startPosition;
            transform.localScale = _startScale;

            // 투명도 초기화
            currentColor.a = 1.0f;
            _cubeRenderer.material.color = currentColor;
        }
    }

 
}