using System.Collections;
using UnityEngine;

public class MortarProjectile : MonoBehaviour
{
    [SerializeField] private LayerMask _IgnoreLayer;
    [SerializeField] private LayerMask _PlayerLayer;

    [Header("Damage")]
    [SerializeField] private int _Damage = 5;
    [SerializeField] private float _SplashRange = 1.5f;

    [Header("Travel")]
    [SerializeField] private float _PredictionIntensity = 1f;
    [SerializeField] private float _PredictionRange = .5f;
    [SerializeField] private Vector3[] _Points = new Vector3[3];
    [SerializeField] private float _Height = 5; // Height of the parabola middle  point
    [SerializeField] private float _TravelTime = 2f; // The time before it reaches its end

    private GameObject _particleSystem;

    private Transform _playerPos;

    private float _timer;

    private bool _firing;
    private bool _wentBoom;

    Vector3 _targetRotation;
    private float _rotationSpeed;

    private Coroutine _currentCoroutine;

    void Start()
    {
        _particleSystem = transform.GetChild(0).gameObject;
        CalculateParabola();
        _firing = true;
    }

    void Update()
    {
        if (_firing)
        {
            FollowParabola();
        }
    }

    // Have the projectile travel between the three points of the parabola
    private void FollowParabola()
    {
        if (_timer < 1)
        {
            _timer += 1.0f * Time.deltaTime / _TravelTime;

            Vector3 m1 = Vector3.Lerp(_Points[0], _Points[1], _timer);
            Vector3 m2 = Vector3.Lerp(_Points[1], _Points[2], _timer);
            transform.position = Vector3.Lerp(m1, m2, _timer);

            transform.Rotate(_targetRotation * Time.deltaTime * _rotationSpeed);
        }
        
        // Explode if reached the end while not hitting anything
        if (transform.position == _Points[2] && !_wentBoom)
        {
            StartCoroutine(GoBoom());
        }
    }

    // Calculate the path the projectile should follow
    private void CalculateParabola()
    {
        _Points[0] = transform.position;
        CalculateEndPoint();
        _Points[1] = _Points[0] + (_Points[2] - _Points[0]) / 2 + Vector3.up * _Height; // Set middle point

        _targetRotation = new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)); // give the projectile a rotation
        _rotationSpeed = Random.Range(0.1f, 10f);
    }

    // Calculate the target of the projectile with some prediction of where the player will go
    private void CalculateEndPoint()
    {
        _playerPos = FindObjectOfType<PlayerMovement>().transform;
        PlayerMovement playerVel = _playerPos.GetComponent<PlayerMovement>();
        
        float randomizedPredictionIntensity = Random.Range(0, _PredictionIntensity); // set the intensity of the prediction

        _Points[2] = _playerPos.position + playerVel.ReturnVelocity() * randomizedPredictionIntensity;
        _Points[2].x += Random.Range(-_PredictionRange, _PredictionRange); // Add small inaccuracy
        _Points[2].z += Random.Range(-_PredictionRange, _PredictionRange); // "
        _Points[2].y += 5;
        Physics.Raycast(_Points[2], Vector3.down, out RaycastHit hit, 100, ~_IgnoreLayer); // Check y level of ground
        _Points[2].y = hit.point.y - .2f; // Set Y of endpoint at ground
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            StartCoroutine(GoBoom());
        }
        else if (!other.gameObject.CompareTag("Player") && other.gameObject.layer != 7 && other.gameObject.layer != 3 && !other.gameObject.CompareTag("Enemy"))
        {
            StartCoroutine(GoBoom());
        }
    }

    private IEnumerator GoBoom()
    {
        _wentBoom = true;
        GetComponent<MeshRenderer>().enabled = false;
        GetComponent<SphereCollider>().enabled = false;

        // Deal damage to anything with health within a certain range
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _SplashRange, _PlayerLayer);
        if (hitColliders.Length > 0) hitColliders[0].transform.gameObject.GetComponent<Health>().TakeDamage(_Damage);

        _particleSystem.SetActive(true);

        yield return new WaitForSeconds(2f);

        Destroy(gameObject);
    }

    // Visualize the path with gizmos
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(_Points[0], _Points[1]);
        Gizmos.DrawLine(_Points[1], _Points[2]);

        Gizmos.DrawRay(_Points[2], Vector3.down * 2);

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(_Points[0], .2f);
        Gizmos.DrawSphere(_Points[1], .2f);
        Gizmos.DrawSphere(_Points[2], .2f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _SplashRange);
    }
}
