using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private LayerMask _RotationMask; // The layer the mouse casts at to look around
    [SerializeField] private LayerMask _IgnoreLayer;

    [Header("Shooting")]
    [SerializeField] private float _LookForwardAmount = 2; 
    [SerializeField] private float _LookForwardHeight = 2;
    [SerializeField] private float _HeightCorFL = 1; // amount the guns should aim at above the groundcheck (flintlock)
    [SerializeField] private float _HeightCorBB = 1.5f; // ^^ (blunderbus)
    [SerializeField] private float _MinHeight = .3f;
    [SerializeField] private float _MaxHeight = 10;

    [Header("Shooting")]
    [SerializeField] private Gun[] _Guns;
    [SerializeField] private MeshRenderer[] _GunMeshes;
    [SerializeField] private Animator[] _Animators;
    [SerializeField] private int _CurrentGunIndex;

    [SerializeField] private float _GunSwitchDuration = .5f; // "Reloading" Flintlock duration
    [SerializeField] private float _BlunderbusReloadDuration = 1.2f;

    [SerializeField] private AudioClip _ReloadSFX;
    private AudioManager _audioManager;

    private bool _reloading;
    private bool _hasBlunderbus;

    private WeaponUi _weaponUI;

    private void Awake()
    {
        _audioManager = FindObjectOfType<AudioManager>();
        _weaponUI = FindObjectOfType<WeaponUi>();
    }

    void Update()
    {
        HandleKBInput();
    }

    private void FixedUpdate()
    {
        // Calculate the angle the gun should aim based on the angle of the island
        Vector3 forwardPos = transform.position + transform.parent.forward.normalized * _LookForwardAmount;
        forwardPos.y += _LookForwardHeight;
        Vector3 heightCorPos;
        if (Physics.Raycast(forwardPos, Vector3.down, out RaycastHit hit, 100, ~_IgnoreLayer))
        {
            heightCorPos = hit.point;
            if (_CurrentGunIndex != 2) heightCorPos.y += _HeightCorFL;
            if (_CurrentGunIndex == 2) heightCorPos.y += _HeightCorBB;
            LookAt(heightCorPos);
        }
        else
        {
            forwardPos.y = _MinHeight;
            LookAt(forwardPos);
        }
    }

    private void LookAt(Vector3 lookPoint)
    {
        transform.LookAt(lookPoint);
    }

    private void HandleKBInput()
    {
        // Shoot and reload
        if (Input.GetMouseButton(0) && !_reloading)
        {
            _Guns[_CurrentGunIndex].StartCoroutine(_Guns[_CurrentGunIndex].Shoot());
            _reloading = true;
            Invoke("SwitchGun",0.05f);
        }

        // Switch the gun the player is holding
        if (Input.GetKeyDown(KeyCode.Alpha1) && _CurrentGunIndex != 0 && _CurrentGunIndex != 1)
        {
            _CurrentGunIndex = 0;
            _weaponUI.SwapGunImage(_CurrentGunIndex);
            SwitchGun();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) && _CurrentGunIndex != 2 && _hasBlunderbus)
        {
            _CurrentGunIndex = 2;
            _weaponUI.SwapGunImage(1);
            SwitchGun();
        }
    }

    private IEnumerator Reload()
    {
        _reloading = true;
        if (_audioManager != null && _ReloadSFX != null) _audioManager.PlayOneShotSFX(_ReloadSFX);

        if (_CurrentGunIndex != 2) yield return new WaitForSeconds(_GunSwitchDuration);
        else yield return new WaitForSeconds(_BlunderbusReloadDuration);
        
        _reloading = false;
    }

    private void SwitchGun()
    {
        StopAllCoroutines();
        switch (_CurrentGunIndex)
        {
            case 0:
                _CurrentGunIndex = 1;
                GrabRightHandGun();
                StartCoroutine(Reload());
                break;
            case 1:
                _CurrentGunIndex = 0;
                GrabLeftHandGun();
                StartCoroutine(Reload());
                break;
            case 2:
                GrabBlunderbus();
                StartCoroutine(Reload());
                break;
        }
    }

    // Dropping and grabbing a new gun (flintlock)
    private void GrabRightHandGun()
    {
        _GunMeshes[0].enabled = false; // left off
        _GunMeshes[1].enabled = true; // right on

        _Guns[0].enabled = false; // left off
        _Guns[1].enabled = true; // right on

        _GunMeshes[2].enabled = false;
        _Guns[2].enabled = false;

        _Animators[1].SetTrigger("Draw");
    }

    // Dropping and grabbing a new gun (flintlock)
    private void GrabLeftHandGun()
    {
        _GunMeshes[0].enabled = true; // left on
        _GunMeshes[1].enabled = false; // right off

        _Guns[0].enabled = true; // left on
        _Guns[1].enabled = false; // right off

        _GunMeshes[2].enabled = false;
        _Guns[2].enabled = false;

        _Animators[0].SetTrigger("Draw");
    }

    private void GrabBlunderbus()
    {
        _GunMeshes[0].enabled = false;
        _GunMeshes[1].enabled = false;

        _Guns[0].enabled = false;
        _Guns[1].enabled = false;

        _GunMeshes[2].enabled = true;
        _Guns[2].enabled = true;
    }

    public void ResetAfterRespawn()
    {
        if (_CurrentGunIndex != 2)
        {
            GrabRightHandGun();
            StartCoroutine(Reload());
            _CurrentGunIndex = 1;
        }
    }

    public void PickupBlunderBus()
    {
        _hasBlunderbus = true;
        _CurrentGunIndex = 2;
        _weaponUI.SwapGunImage(1);
        SwitchGun();
    }
}
