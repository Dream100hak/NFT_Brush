using System.IO.IsolatedStorage;
using UnityEngine;

public class CubeStraight : MonoBehaviour
{
    private bool _destroyMode = false;
    public bool DestroyMode { get => _destroyMode; set { _destroyMode = value; } }

    [SerializeField]
    private E_Direction _moveDirection = E_Direction.Down;
    public E_Direction MoveDirection { get => _moveDirection; set => _moveDirection = value; }
    [SerializeField]
    private float _moveSpeed = 2.0f;
    public float MoveSpeed { get => _moveSpeed; set { _moveSpeed = value; } }

    private Vector3 _startPosition;
    private Camera _mainCamera;

    public bool _exception; 

    void Start()
    {
        _startPosition = transform.position;
        _mainCamera = Camera.main;
    }
    void Update()
    {
        Vector3 moveVector = Vector3.zero;

        switch (_moveDirection)
        {
            case E_Direction.Up:
                moveVector = new Vector3(0, _moveSpeed * Time.deltaTime, 0);
                break;
            case E_Direction.Down:
                moveVector = new Vector3(0, -_moveSpeed * Time.deltaTime, 0);
                break;
            case E_Direction.Left:
                moveVector = new Vector3(-_moveSpeed * Time.deltaTime, 0, 0);
                break;
            case E_Direction.Right:
                moveVector = new Vector3(_moveSpeed * Time.deltaTime, 0, 0);
                break;
        }

        transform.position += moveVector;

        Vector3 viewportPos = _mainCamera.WorldToViewportPoint(transform.position);


        if(_exception == false)
        {
            if (viewportPos.y < -0.1f || viewportPos.y > 1.1f || viewportPos.x < -0.1f || viewportPos.x > 1.1f)
            {
                if (DestroyMode == false)
                    transform.position = _startPosition;
                else
                    Destroy(gameObject);
            }
        }

    }
}