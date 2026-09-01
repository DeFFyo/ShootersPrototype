using UnityEngine;

public class USASoldier : Enemy
{
    [Header("Ammo")]
    public int MaxAmmo = 30;
    public int Ammo;
    public float ReloadTime = 2f;

    [Header("Animations")]
    public Animator Animator;
    public float WalkSpeed = 2f;
    public float RunSpeed = 5f;

    private float _currentSpeed;
    private bool _isReloading;
    public bool IsReloading => _isReloading;
    private float _reloadTimer;

    public bool IsAiming { get; set; }
    public bool IsShooting { get; set; }

    protected override void Awake()
    {
        base.Awake();
        Ammo = MaxAmmo;
        if (!Animator)
            Animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        UpdateReload();
        UpdateAnimator();
    }

    private void UpdateReload()
    {
        if (!_isReloading) return;

        _reloadTimer -= Time.deltaTime;
        if (_reloadTimer <= 0f)
        {
            Ammo = MaxAmmo;
            _isReloading = false;
        }
    }

    private void UpdateAnimator()
    {
        if (!Animator) return;

        Animator.SetFloat("Speed", _currentSpeed, 0.1f, Time.deltaTime);
        Animator.SetBool("IsAiming", IsAiming);
        Animator.SetBool("IsReloading", _isReloading);

        if (IsShooting)
        {
            Animator.SetTrigger("IsShooting");
            IsShooting = false;
        }
    }

    public void StartReload()
    {
        if (_isReloading || Ammo == MaxAmmo) return;
        _isReloading = true;
        _reloadTimer = ReloadTime;
    }

    public bool TryFire()
    {
        if (_isReloading || Ammo <= 0) return false;
        Ammo--;

        if (Ammo <= 0)
            StartReload();

        return true;
    }

    public void SetSpeed(float speed)
    {
        _currentSpeed = speed;
    }
}