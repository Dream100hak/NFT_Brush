using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeRotator : MonoBehaviour
{
    // ȸ�� �� ���� (X, Y, Z)
    public Vector3 _rotationAxisRange = new Vector3(1f, 1f, 1f);

    // ť���� ȸ�� ��
    private Vector3 _rotationAxis;

    //���ǵ�
    private float _rotationSpeed = 50.0f;
    public float RotationSpeed { get =>_rotationSpeed; set => _rotationSpeed = value; }

    void Start()
    {
        // ť���� ȸ�� ���� �����ϰ� ����
        float x = Random.Range(-_rotationAxisRange.x, _rotationAxisRange.x);
        float y = Random.Range(-_rotationAxisRange.y, _rotationAxisRange.y);
        float z = Random.Range(-_rotationAxisRange.z, _rotationAxisRange.z);
        _rotationAxis = new Vector3(x, y, z).normalized;
    }

    void Update()
    {
        // ť�긦 ȸ����Ŵ
        transform.Rotate(_rotationAxis * Time.deltaTime * _rotationSpeed);
    }
}
