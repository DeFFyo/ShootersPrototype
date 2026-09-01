using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    public Camera PlayerCamera;
    public float Damage = 25f;
    public float MaxRange = 200f;
    public float FireRate = 0.1f;

    private InputAction _shootAction;
    private float _lastFireTime;

    private void OnEnable()
    {
        _shootAction = new InputAction("Shoot", InputActionType.Button, "<Mouse>/leftButton");
        _shootAction.Enable();
    }

    private void OnDisable()
    {
        _shootAction?.Disable();
    }

    private void Awake()
    {
        if (!PlayerCamera)
            PlayerCamera = GetComponentInChildren<Camera>();
    }

    private void Update()
    {
        if (_shootAction.IsPressed() && Time.time >= _lastFireTime + FireRate)
        {
            _lastFireTime = Time.time;
            Fire();
        }
    }

    private void Fire()
    {
        Vector3 origin = PlayerCamera.transform.position;
        Vector3 direction = PlayerCamera.transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, MaxRange))
        {
            Debug.DrawLine(origin, hit.point, Color.red, 1f);

            EnemyHitbox hitbox = hit.collider.GetComponent<EnemyHitbox>();
            if (hitbox != null)
            {
                hitbox.ProcessDamage(Damage, hit.point, hit.normal);
                return;
            }

            IDamagable damagable = hit.collider.GetComponentInParent<IDamagable>();
            if (damagable != null)
                damagable.TakeDamage(Damage, hit.point, hit.normal);
        }
        else
        {
            Debug.DrawRay(origin, direction * MaxRange, Color.red, 1f);
        }
    }

    private string GetGOpath(GameObject go)
    {
        string path = go.name;
        Transform p = go.transform.parent;
        while (p != null) { path = p.name + "/" + path; p = p.parent; }
        return path;
    }
}
