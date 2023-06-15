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
        // ť�긦 �߽������� �������� ��
        Vector3 directionToCenter = (_blackholeCenter.position - transform.position).normalized;
        transform.position += directionToCenter * _attractionForce * Time.deltaTime;

        // �Ÿ��� ���� ũ�⸦ ����
        float distanceToCenter = Vector3.Distance(transform.position, _blackholeCenter.position);
        float scaleMultiplier = distanceToCenter / (_startPosition - _blackholeCenter.position).magnitude;
        transform.localScale = _startScale * scaleMultiplier;

        // �Ÿ��� ���� ������ ����
        //float alpha = Mathf.Pow(scaleMultiplier, 2); 
      //  float alpha = scaleMultiplier;
        float alpha = Mathf.Sqrt(scaleMultiplier); // ��Ʈ�� �����Ͽ� ������ �� õõ�� �����ϵ��� ��
        Color currentColor = _cubeRenderer.material.color;
        currentColor.a = alpha;
        _cubeRenderer.material.color = currentColor;

        // ũ�Ⱑ �ʹ� �۾������� �ʱ� ��ġ�� ���ư��� ��
        if (transform.localScale.magnitude < _minScale)
        {
            transform.position = _startPosition;
            transform.localScale = _startScale;

            // ���� �ʱ�ȭ
            currentColor.a = 1.0f;
            _cubeRenderer.material.color = currentColor;
        }
    }

 
}