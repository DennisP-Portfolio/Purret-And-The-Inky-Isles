using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider))]
public class MortarSpawner : MonoBehaviour
{
    [SerializeField] private int _Level;

    [SerializeField] private GameObject _MortarPrefab;

    [SerializeField] private LayerMask _IgnoreLayer;

    [Header("Values")]
    [SerializeField] private float _AmountToKill; // Set how many mortars the level has in inspector
    [SerializeField] private int _AmountLeftToKill;
    [SerializeField] private int _ShouldSpawnAmount;
    [SerializeField] private Vector3 _HeighCorrection;
    [SerializeField] private float _SeaLevel; // under what height they should never spawn
    [SerializeField] private float _ClearanceRange; // how much free space the mortar needs to be placed

    [SerializeField] private Dock _ConnectedDock;

    private int _currentSpawnedAmount;

    private BoxCollider _spawnRange;
    private bool _Spawning;
    private bool _Spawned;
    private bool _DepartureDespawn;
    private bool _cleared;

    private List<Vector3> _SavedLevelSpawnPositions = new();
    private List<GameObject> _spawnedObjects = new();

    private LevelManager _levelManager;
    private BoatMovement _boatMovement;

    private InfoTextHandler _infoTextHandler;
    private TextMeshProUGUI _mortarAmountText;
    private Image _mortarIcon;
    private Image _backBoard;

    private Compass _compass;

    private void Start()
    {
        // Collect all needed data
        _spawnRange = GetComponent<BoxCollider>();
        _levelManager = FindObjectOfType<LevelManager>();
        _boatMovement = FindObjectOfType<BoatMovement>();
        _infoTextHandler = FindObjectOfType<InfoTextHandler>();
        _ConnectedDock = transform.parent.GetComponentInChildren<Dock>();
        _mortarAmountText = GameObject.FindGameObjectWithTag("MortarAmountText").GetComponent<TextMeshProUGUI>();
        _mortarIcon = _mortarAmountText.transform.parent.GetChild(2).GetComponent<Image>();
        _backBoard = _mortarAmountText.transform.parent.GetChild(0).GetComponent<Image>();
        _compass = FindObjectOfType<Compass>();
    }

    public void StartSpawningMortars()
    {
        if (!_Spawning && !_Spawned)
        {
            StartCoroutine(SpawnMortars());
            _Spawning = true;
        }
    }

    public void StartRespawningLevelMortars()
    {
        StartCoroutine(RespawnLevelMortars());
    }

    private IEnumerator SpawnMortars()
    {
        _DepartureDespawn = false;

        _ShouldSpawnAmount = (int)(_AmountToKill * 1.5f);
        _AmountLeftToKill = (int)_AmountToKill;

        if (_currentSpawnedAmount >= _ShouldSpawnAmount) yield return null;

        _SavedLevelSpawnPositions.Clear();

        Vector3 rayOrigin = RandomPointInBounds(_spawnRange.bounds);

        bool objectsWithinClearanceRange = false;

        while (_currentSpawnedAmount < _ShouldSpawnAmount)
        {
            Vector3 possibleSpawnPoint;

            // Cast a ray down towards the island and check if the point follows all rules if so spawn a mortar
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 100, ~_IgnoreLayer))
            {
                if (hit.transform.gameObject.layer == 7 && hit.point.y > _SeaLevel)
                {
                    objectsWithinClearanceRange = false;
                    // check if it isnt too crowded at this position
                    Collider[] hitColliders = Physics.OverlapSphere(hit.point, _ClearanceRange, ~_IgnoreLayer);

                    for (int i = 0; i < hitColliders.Length; i++)
                    {
                        if (hitColliders[i].gameObject.layer != 7)
                        {
                            objectsWithinClearanceRange = true;
                        }
                    }
                    if (!objectsWithinClearanceRange)
                    {
                        possibleSpawnPoint = hit.point;
                        possibleSpawnPoint += _HeighCorrection; // place the mortar above the sand
                        GameObject spawnedObj = Instantiate(_MortarPrefab, possibleSpawnPoint, Quaternion.Euler(0, Random.Range(0, 359), 0), transform);
                        spawnedObj.GetComponentInChildren<Mortar>().RememberSpawner(this);
                        _spawnedObjects.Add(spawnedObj);
                        _SavedLevelSpawnPositions.Add(spawnedObj.transform.position);
                        _currentSpawnedAmount++;
                    }
                }
            }

            // pick next destination to raycast from
            rayOrigin = RandomPointInBounds(_spawnRange.bounds);

            yield return new WaitForEndOfFrame();
        }

        UpdateText(true);

        _Spawned = true;
        _Spawning = false;
    }

    // Respawn mortars at the same positions if restarting level
    private IEnumerator RespawnLevelMortars()
    {
        KillAllMortars();

        _Spawning = true;
        foreach (Vector3 pos in _SavedLevelSpawnPositions)
        {
            GameObject spawnedObj = Instantiate(_MortarPrefab, pos, Quaternion.Euler(0, Random.Range(0, 359), 0), transform);
            _spawnedObjects.Add(spawnedObj);
            spawnedObj.GetComponentInChildren<Mortar>().RememberSpawner(this);
            yield return new WaitForEndOfFrame();
        }

        _currentSpawnedAmount = _spawnedObjects.Count;
        _AmountLeftToKill = (int)_AmountToKill;

        UpdateText(true);

        _Spawned = true;
        _Spawning = false;

        yield return null;
    }

    // Remove all mortars from the island for restarting or leaving the level
    private void KillAllMortars()
    {
        if (_Spawning) return;

        foreach (GameObject obj in _spawnedObjects)
        {
            Destroy(obj);
        }

        _spawnedObjects.Clear();

        UpdateText(false);
        _currentSpawnedAmount = 0;
        _Spawning = false;
        _Spawned = false;
    }

    public void RemoveMortarOnDeath(GameObject mortar)
    {
        if (_DepartureDespawn) return;
        if (_spawnedObjects.Contains(mortar)) _spawnedObjects.Remove(mortar);
        _currentSpawnedAmount--;
        _AmountLeftToKill--;
        if (!_cleared)
        {
            UpdateText(true);
        }

        // If last mortar, clear the level
        if (_currentSpawnedAmount <= 0 && !_Spawning && !_cleared || _AmountLeftToKill <= 0 && !_Spawning && !_cleared)
        {
            KillAllMortars();
            UpdateText(false);
            _compass.FindBoat(true);
            _ConnectedDock.SetAsClearedIsland();
            _levelManager.ClearLevel(_ConnectedDock.transform.parent.gameObject);
            _infoTextHandler.SetTimedText("Well done! \r\n Follow the compass to the next island");
            _Spawned = false;
            _cleared = true;
        }
    }

    public void DestroyAllOnBoatDeparture()
    {
        _DepartureDespawn = true;
        KillAllMortars();
    }

    private void UpdateText(bool isEnabled)
    {
        _mortarAmountText.enabled = isEnabled;
        _mortarIcon.enabled = isEnabled;
        _backBoard.enabled = isEnabled;
        _mortarAmountText.text = _AmountLeftToKill.ToString();
    }

    /// <summary>
    /// Creates a random Vector3 position within set bounds
    /// </summary>
    private Vector3 RandomPointInBounds(Bounds bounds)
    {
        return new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z)
        );
    }
}
