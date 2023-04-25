using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeRotator : MonoBehaviour
{
    // 회전 축 범위 (X, Y, Z)
    public Vector3 _rotationAxisRange = new Vector3(1f, 1f, 1f);

    // 큐브의 회전 축
    private Vector3 _rotationAxis;

    //스피드
    private float _rotationSpeed = 50.0f;
    public float RotationSpeed { get =>_rotationSpeed; set => _rotationSpeed = value; }

    void Start()
    {
        // 큐브의 회전 축을 랜덤하게 지정
        float x = Random.Range(-_rotationAxisRange.x, _rotationAxisRange.x);
        float y = Random.Range(-_rotationAxisRange.y, _rotationAxisRange.y);
        float z = Random.Range(-_rotationAxisRange.z, _rotationAxisRange.z);
        _rotationAxis = new Vector3(x, y, z).normalized;
    }

    void Update()
    {
        // 큐브를 회전시킴
        transform.Rotate(_rotationAxis * Time.deltaTime * _rotationSpeed);
    }
}
