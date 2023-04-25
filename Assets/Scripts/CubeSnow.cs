using UnityEngine;

public class CubeSnow : MonoBehaviour
{

    private bool _destroyMode = false;
    public bool DestroyMode { get => _destroyMode; set { _destroyMode = value; } }



    private E_Direction _moveDirection = E_Direction.Down;
    [SerializeField]
    private float _snowSpeed = 2.0f;
    public float SnowSpeed { get => _snowSpeed; set { _snowSpeed = value; } }

    [SerializeField]
    private float _swayIntensity = 1f;
    public float SwayIntensity { get => _swayIntensity; set => _swayIntensity = value; }

    [SerializeField]
    private float _swayAmount = 0.1f;
    public float SwayAmount { get => _swayAmount; set { _swayAmount = value; } }

    private Vector3 _startPosition;
    private Camera _mainCamera;
    private float _noiseOffset;

    void Start()
    {
        _startPosition = transform.position;
        _mainCamera = Camera.main;
        _noiseOffset = Random.Range(0f, 100f);

        _snowSpeed = Random.Range(1.5f, 2f);
    }

    void Update()
    {
        Vector3 moveVector = Vector3.zero;

        switch (_moveDirection)
        {
            case E_Direction.Down:
                moveVector = new Vector3(0, -_snowSpeed * Time.deltaTime, 0);
                break;
        }

        float sway = Mathf.PerlinNoise(Time.time * _swayAmount + _noiseOffset, 0f) * 2 - 1;
        moveVector.x += sway * _swayIntensity * Time.deltaTime;

        transform.position += moveVector;

        Vector3 viewportPos = _mainCamera.WorldToViewportPoint(transform.position);

        if (viewportPos.y < -0.1f)
        {
            if (DestroyMode == false)
                transform.position = _startPosition;
            else
                Destroy(gameObject);
        }
    }
}