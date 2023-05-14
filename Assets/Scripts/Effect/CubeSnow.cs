using UnityEngine;
using UnityEngine.EventSystems;

public class CubeSnow : MonoBehaviour
{

    private bool _destroyMode = false;
    public bool DestroyMode { get => _destroyMode; set { _destroyMode = value; } }

    [SerializeField]
    private float _snowSpeed = 2.0f;
    public float SnowSpeed { get => _snowSpeed; set { _snowSpeed = value; } }

    [SerializeField]
    private float _swayIntensity = 1f;
    public float SwayIntensity { get => _swayIntensity; set => _swayIntensity = value; }

    [SerializeField]
    private float _swayAmount = 0.1f;
    public float SwayAmount { get => _swayAmount; set { _swayAmount = value; } }

    [SerializeField]
    private Vector3 _windDirection = Vector3.zero;
    public Vector3 WindDirection { get => _windDirection; set { _windDirection = value; } }

    [SerializeField]
    private float _windStrength = 0f;
    public float WindStrength { get => _windStrength; set { _windStrength = value; } }

    private Vector3 _startPosition;
    private Camera _mainCamera;
    private float _noiseOffset;

    private Vector3 _moveVector = Vector3.zero;

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

        moveVector = new Vector3(0, -_snowSpeed * Time.deltaTime, 0);

        float sway = Mathf.PerlinNoise(Time.time * _swayAmount + _noiseOffset, 0f) * 2 - 1;
        moveVector.x += sway * _swayIntensity * Time.deltaTime;

        // 바람에 따라서 x 방향으로도 이동
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            moveVector.x -= .5f * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            moveVector.x += .5f * Time.deltaTime;
        }

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