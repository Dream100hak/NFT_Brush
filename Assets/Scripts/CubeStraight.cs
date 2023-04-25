using UnityEngine;

public class CubeStraight : MonoBehaviour
{
    [SerializeField]
    private E_Direction _moveDirection = E_Direction.Down;
    public E_Direction MoveDirection { get => _moveDirection; set => _moveDirection = value; }
    [SerializeField]
    private float _moveSpeed = 2.0f;
    public float MoveSpeed { get => _moveSpeed; set { _moveSpeed = value; } }

    private Vector3 _startPosition;
    private Camera _mainCamera;

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

        // 월드 좌표를 카메라 뷰포트 좌표로 변환
        Vector3 viewportPos = _mainCamera.WorldToViewportPoint(transform.position);

        // 큐브가 카메라 영역을 벗어났는지 확인
        if (viewportPos.y < -0.1f || viewportPos.y > 1.1f || viewportPos.x < -0.1f || viewportPos.x > 1.1f)
        {
            // 원래 위치로 돌아가기
            transform.position = _startPosition;
        }
    }
}