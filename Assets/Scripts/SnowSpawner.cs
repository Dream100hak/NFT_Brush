using UnityEngine;
using UnityEngine.UIElements;

public class SnowSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject _cubeSnowPrefab;
    public GameObject CubeSnowPrefab { get => _cubeSnowPrefab; set => _cubeSnowPrefab = value; }

    [SerializeField]
    private float _spawnInterval = 1.0f;
    public float SpawnInterval { get => _spawnInterval; set => _spawnInterval = value; }
    [SerializeField]
    private float _swayAmount = 1.0f;
    public float SwayAmount { get => _swayAmount; set => _swayAmount = value; }
    private float _swayIntensity = 1.0f;
    public float SwayIntensity { get => _swayIntensity; set => _swayIntensity = value; }

    private float _timeSinceLastSpawn;

    private void Awake()
    {
        _spawnInterval = Random.Range(3, 10f);
    }

    private void Update()
    {
        _timeSinceLastSpawn += Time.deltaTime;

        if (_timeSinceLastSpawn >= _spawnInterval)
        {
            SpawnCubeSnow();
            _timeSinceLastSpawn = 0f;
            _spawnInterval = Random.Range(3, 10f);
        }
    }

    private void SpawnCubeSnow()
    {
        Vector3 spawnPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);

        GameObject newCubeSnow = Instantiate(_cubeSnowPrefab, spawnPosition, Quaternion.identity);
        newCubeSnow.transform.localScale = transform.localScale;
        Renderer parentRenderer = GetComponent<Renderer>();
        Renderer cubeSnowRenderer = newCubeSnow.GetComponent<Renderer>();

        if (parentRenderer != null && cubeSnowRenderer != null)
        {
            cubeSnowRenderer.material = parentRenderer.material;
        }

        if(GetComponent<CubeRotator>().enabled)
        {
            newCubeSnow.GetComponent<CubeRotator>().enabled = true;
            newCubeSnow.GetComponent<CubeRotator>().RotationSpeed = GetComponent<CubeRotator>().RotationSpeed;
        }

        newCubeSnow.GetComponent<CubeSnow>().enabled = true;
        newCubeSnow.GetComponent<CubeSnow>().SwayAmount = SwayAmount;
        newCubeSnow.GetComponent<CubeSnow>().SwayIntensity = _swayIntensity;
        newCubeSnow.GetComponent<CubeSnow>().DestroyMode = true;

        newCubeSnow.GetComponent<BoxCollider>().isTrigger = false;
        newCubeSnow.AddComponent<CubeResizer>();
    }
}
