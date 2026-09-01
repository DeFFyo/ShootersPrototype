using UnityEngine;
using UnityEngine.InputSystem;
using KinematicCharacterController;

[RequireComponent(typeof(KinematicCharacterMotor))]
public class PlayerController : MonoBehaviour, ICharacterController, IDamagable
{
    public KinematicCharacterMotor Motor;
    public Camera PlayerCamera;

    [Header("Movement")]
    public float MaxWalkSpeed = 6.5f;
    public float GroundAcceleration = 60f;
    public float GroundFriction = 7f;
    public float AirAcceleration = 5f;
    public float MaxAirSpeed = 6.5f;
    public float AirFriction = 0.1f;
    public float Gravity = 20f;

    [Header("Jump")]
    public float JumpSpeed = 8f;

    [Header("Look")]
    public float MouseSensitivity = 1.5f;
    public float MinPitch = -90f;
    public float MaxPitch = 90f;

    [Header("Aiming")]
    public Camera WeaponCamera;
    public Transform WeaponModel;
    public Vector3 NormalWeaponPosition = new Vector3(0.5f, -0.3f, 0.6f);
    public Vector3 AimWeaponPosition = new Vector3(0f, -0.117f, 0.195f);
    public float NormalFov = 90f;
    public float AimFov = 59f;
    public float WeaponAimFov = 45f;
    public float AimSpeed = 10f;

    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _jumpAction;
    private InputAction _aimAction;
    private bool _isAiming;

    private Vector2 _moveInput;
    private float _targetYaw;
    private float _pitch;
    private bool _jumpPressed;

    public float Health = 100f;
    public float MaxHealth = 100f;
    public int ammo;
    public int hp;
    public int armor;

    private void OnEnable()
    {
        _moveAction = new InputAction("Move", InputActionType.Value);
        _moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        _moveAction.Enable();

        _lookAction = new InputAction("Look", InputActionType.Value);
        _lookAction.AddBinding("<Mouse>/delta");
        _lookAction.Enable();

        _jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
        _jumpAction.performed += OnJumpPerformed;
        _jumpAction.Enable();

        _aimAction = new InputAction("Aim", InputActionType.Button, "<Mouse>/rightButton");
        _aimAction.Enable();
    }

    private void OnDisable()
    {
        _jumpAction.performed -= OnJumpPerformed;
        _moveAction?.Disable();
        _lookAction?.Disable();
        _jumpAction?.Disable();
        _aimAction?.Disable();
    }

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        _jumpPressed = true;
    }

    private void Awake()
    {
        Motor = GetComponent<KinematicCharacterMotor>();
        Motor.CharacterController = this;

        if (!PlayerCamera)
            PlayerCamera = GetComponentInChildren<Camera>();

        Transform wc = PlayerCamera.transform.Find("WeaponCamera");
        if (wc)
        {
            if (!WeaponCamera)
                WeaponCamera = wc.GetComponent<Camera>();
            if (!WeaponModel)
            {
                Transform wm = wc.Find("Weapon_Pistol");
                if (wm) WeaponModel = wm;
            }
        }

        if (!DeathScreen.Instance)
            new GameObject("DeathScreenManager", typeof(DeathScreen));

        Cursor.lockState = CursorLockMode.Locked;
        _targetYaw = transform.eulerAngles.y;
    }

    private void Start()
    {
        if (PlayerCamera)
            PlayerCamera.fieldOfView = NormalFov;

        if (WeaponCamera)
            WeaponCamera.fieldOfView = NormalFov;

        if (WeaponModel)
            WeaponModel.localPosition = NormalWeaponPosition;
    }

    private void Update()
    {
        _moveInput = _moveAction.ReadValue<Vector2>();

        Vector2 lookDelta = _lookAction.ReadValue<Vector2>();
        _targetYaw += lookDelta.x * MouseSensitivity;
        _pitch -= lookDelta.y * MouseSensitivity;
        _pitch = Mathf.Clamp(_pitch, MinPitch, MaxPitch);

        if (PlayerCamera)
            PlayerCamera.transform.localRotation = Quaternion.Euler(_pitch, 0, 0);

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            Cursor.lockState = CursorLockMode.None;
        if (Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState == CursorLockMode.None)
            Cursor.lockState = CursorLockMode.Locked;

        _isAiming = _aimAction.IsPressed();

        UpdateAiming(Time.deltaTime);
    }

    private void UpdateAiming(float deltaTime)
    {
        if (PlayerCamera)
        {
            float targetFov = _isAiming ? AimFov : NormalFov;
            PlayerCamera.fieldOfView = Mathf.Lerp(PlayerCamera.fieldOfView, targetFov, AimSpeed * deltaTime);
        }

        if (WeaponCamera)
        {
            float targetFov = _isAiming ? WeaponAimFov : NormalFov;
            WeaponCamera.fieldOfView = Mathf.Lerp(WeaponCamera.fieldOfView, targetFov, AimSpeed * deltaTime);
        }

        if (WeaponModel)
        {
            Vector3 targetPos = _isAiming ? AimWeaponPosition : NormalWeaponPosition;
            WeaponModel.localPosition = Vector3.Lerp(WeaponModel.localPosition, targetPos, AimSpeed * deltaTime);
        }
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        currentRotation = Quaternion.Euler(0, _targetYaw, 0);
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        if (Motor.GroundingStatus.IsStableOnGround)
        {
            Vector3 forward = PlayerCamera.transform.forward;
            Vector3 right = PlayerCamera.transform.right;
            forward.y = 0; right.y = 0;
            forward.Normalize(); right.Normalize();

            Vector3 desiredMove = forward * _moveInput.y + right * _moveInput.x;

            Vector3 horizontalVel = new Vector3(currentVelocity.x, 0, currentVelocity.z);

            if (desiredMove.sqrMagnitude > 0f)
            {
                desiredMove.Normalize();
                desiredMove *= MaxWalkSpeed;

                Vector3 velocityDiff = desiredMove - horizontalVel;
                float accel = GroundAcceleration * deltaTime;
                if (velocityDiff.magnitude < accel)
                    horizontalVel = desiredMove;
                else
                    horizontalVel += velocityDiff.normalized * accel;
            }
            else
            {
                float speed = horizontalVel.magnitude;
                if (speed > 0f)
                {
                    float drop = speed * GroundFriction * deltaTime;
                    horizontalVel *= Mathf.Max(speed - drop, 0f) / speed;
                }
            }

            currentVelocity = new Vector3(horizontalVel.x, currentVelocity.y, horizontalVel.z);

            Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal;
            if (effectiveGroundNormal.y > 0f)
            {
                float currentVelMagnitude = currentVelocity.magnitude;
                currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentVelMagnitude;
            }
        }
        else
        {
            Vector3 forward = PlayerCamera.transform.forward;
            Vector3 right = PlayerCamera.transform.right;
            forward.y = 0; right.y = 0;
            forward.Normalize(); right.Normalize();

            Vector3 desiredMove = forward * _moveInput.y + right * _moveInput.x;

            if (desiredMove.sqrMagnitude > 0f)
            {
                desiredMove.Normalize();
                Vector3 horizontalVel = new Vector3(currentVelocity.x, 0, currentVelocity.z);
                Vector3 targetVel = desiredMove * MaxAirSpeed;
                Vector3 velDiff = targetVel - horizontalVel;

                float accel = AirAcceleration * deltaTime;
                if (velDiff.magnitude < accel)
                {
                    currentVelocity.x = targetVel.x;
                    currentVelocity.z = targetVel.z;
                }
                else
                {
                    currentVelocity.x += velDiff.normalized.x * accel;
                    currentVelocity.z += velDiff.normalized.z * accel;
                }

                Vector3 hVel = new Vector3(currentVelocity.x, 0, currentVelocity.z);
                if (hVel.magnitude > MaxAirSpeed)
                {
                    hVel = hVel.normalized * MaxAirSpeed;
                    currentVelocity.x = hVel.x;
                    currentVelocity.z = hVel.z;
                }
            }

            Vector3 airHoriz = new Vector3(currentVelocity.x, 0, currentVelocity.z);
            float airSpeed = airHoriz.magnitude;
            if (airSpeed > 0f)
            {
                float drop = airSpeed * AirFriction * deltaTime;
                airHoriz *= Mathf.Max(airSpeed - drop, 0f) / airSpeed;
                currentVelocity.x = airHoriz.x;
                currentVelocity.z = airHoriz.z;
            }

            currentVelocity.y -= Gravity * deltaTime;
        }

        if (_jumpPressed)
        {
            _jumpPressed = false;
            if (Motor.GroundingStatus.IsStableOnGround)
            {
                Motor.ForceUnground();
                currentVelocity.y = JumpSpeed;
            }
        }
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        Health -= amount;
        Debug.Log($"Player took {amount} damage. Health: {Health}");

        if (Health <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (DeathScreen.Instance)
            DeathScreen.Instance.Show();
        else
            Debug.Log("Player died");
    }

    public void BeforeCharacterUpdate(float deltaTime) { }
    public void PostGroundingUpdate(float deltaTime) { }
    public void AfterCharacterUpdate(float deltaTime) { }
    public bool IsColliderValidForCollisions(Collider coll) => true;
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }
    public void OnDiscreteCollisionDetected(Collider hitCollider) { }
}
