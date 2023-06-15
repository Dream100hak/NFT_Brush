using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StraightSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject _cubePrefab;
    public GameObject CubePrefab { get => _cubePrefab; set => _cubePrefab = value; }

    [SerializeField]
    private float _spawnInterval = 1.0f;
    public float SpawnInterval { get => _spawnInterval; set => _spawnInterval = value; }
    [SerializeField]
    private float _moveSpeed = 1.0f;
    public float MoveSpeed { get => _moveSpeed; set => _moveSpeed = value; }
    private E_Direction _moveDirection = E_Direction.Down;
    public E_Direction MoveDirection { get => _moveDirection; set => _moveDirection = value; }

    private float _timeSinceLastSpawn;

    private void Awake()
    {
        _spawnInterval = Random.Range(1, 3);
    }

    private void Update()
    {
        _timeSinceLastSpawn += Time.deltaTime;

        if (_timeSinceLastSpawn >= _spawnInterval)
        {
            SpawnCubeStaright();
            _timeSinceLastSpawn = 0f;
            _spawnInterval = Random.Range(1, 3);
        }
    }
    private void SpawnCubeStaright()
    {
        Vector3 spawnPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);

        GameObject newStraightObj = Instantiate(_cubePrefab, spawnPosition, Quaternion.identity);
        newStraightObj.transform.localScale = transform.localScale;
        Renderer parentRenderer = GetComponent<Renderer>();
        Renderer cubeSnowRenderer = newStraightObj.GetComponent<Renderer>();

        if (parentRenderer != null && cubeSnowRenderer != null)
        {
            cubeSnowRenderer.material = parentRenderer.material;
        }

        if (GetComponent<EffectRotator>().enabled)
        {
            newStraightObj.GetComponent<EffectRotator>().enabled = true;
            newStraightObj.GetComponent<EffectRotator>().RotationSpeed = GetComponent<EffectRotator>().RotationSpeed;
        }

        newStraightObj.GetComponent<EffectStraight>().enabled = true;
        newStraightObj.GetComponent<EffectStraight>().MoveSpeed = MoveSpeed;
        newStraightObj.GetComponent<EffectStraight>().MoveDirection = MoveDirection;
        newStraightObj.GetComponent<EffectStraight>().DestroyMode = true;

        newStraightObj.GetComponent<BoxCollider>().isTrigger = false;
        newStraightObj.AddComponent<EffectResizer>();
    }
}
