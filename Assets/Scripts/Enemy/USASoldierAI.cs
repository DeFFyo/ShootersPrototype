using UnityEngine;
using UnityEngine.AI;

public class USASoldierAI : MonoBehaviour
{
    public enum AIState { Idle, Combat, ReloadRetreat, Retreat, Reload }

    [Header("References")]
    public USASoldier Soldier;
    public NavMeshAgent Agent;
    public Transform Player;
    public Transform EyeTransform;

    [Header("Combat")]
    public float DetectRange = 30f;
    public float RetreatRange = 6f;
    public float ReloadRetreatRange = 15f;
    public float FireRange = 20f;
    public float FireCooldown = 0.3f;
    public float Damage = 10f;
    public float LostSightTime = 10f;

    [Header("Idle")]
    public float IdleWanderRadius = 10f;
    public float IdleWanderInterval = 3f;

private AIState _state = AIState.Idle;
    private float _stateTimer;
    private float _fireTimer;
    private Vector3 _wanderTarget;
    private bool _playerVisible;
    private float _lastSeenTime = -100f;
    private Vector3 _lastKnownPosition;

    private void Awake()
    {
        if (!Soldier) Soldier = GetComponent<USASoldier>();
        if (!Agent) Agent = GetComponent<NavMeshAgent>();

        if (!Player)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) Player = p.transform;
        }

        if (!EyeTransform)
        {
            var anim = Soldier.Animator;
            if (anim) EyeTransform = anim.transform;
        }

        Agent.updateRotation = true;
        Agent.stoppingDistance = 1f;

        PickWanderTarget();
        _fireTimer = 0f;
    }

    private void Update()
    {
        if (!Player || !Agent || !Soldier || !Soldier.Animator) return;

        _playerVisible = HasLineOfSight();

        if (_playerVisible)
        {
            _lastSeenTime = Time.time;
            _lastKnownPosition = Player.position;
        }

        _stateTimer -= Time.deltaTime;
        _fireTimer -= Time.deltaTime;

        Soldier.IsCalm = !_playerVisible;

        UpdateState();
        ExecuteState();
        UpdateAnimator();
    }

    private bool HasLineOfSight()
    {
        if (!Player || !EyeTransform) return false;

        Vector3 origin = EyeTransform.position + Vector3.up * 1.5f;
        Vector3 direction = (Player.position + Vector3.up * 1f) - origin;
        float distance = direction.magnitude;

        if (distance > DetectRange) return false;

        RaycastHit[] hits = Physics.RaycastAll(origin, direction.normalized, distance);
        foreach (var hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;

            if (hit.collider.transform == Player || hit.collider.GetComponentInParent<PlayerController>())
                return true;
        }
        return false;
    }

    private void UpdateState()
    {
        float distToPlayer = Player ? Vector3.Distance(transform.position, Player.position) : float.MaxValue;

        if (!_playerVisible)
        {
            if (Time.time - _lastSeenTime >= 10f)
                _state = AIState.Idle;
            return;
        }

        if (Soldier.IsReloading && distToPlayer <= ReloadRetreatRange)
        {
            _state = AIState.ReloadRetreat;
            return;
        }

        if (distToPlayer < RetreatRange)
        {
            _state = AIState.Retreat;
            return;
        }

        if (distToPlayer <= ReloadRetreatRange && Soldier.Ammo < Soldier.MaxAmmo / 2)
        {
            _state = AIState.ReloadRetreat;
            return;
        }

        if (Soldier.IsReloading)
        {
            _state = AIState.Reload;
            return;
        }

        if (distToPlayer <= FireRange)
        {
            _state = AIState.Combat;
            return;
        }

        _state = AIState.Idle;
    }

    private void ExecuteState()
    {
        switch (_state)
        {
            case AIState.Idle: ExecuteIdle(); break;
            case AIState.Combat: ExecuteCombat(); break;
            case AIState.ReloadRetreat: ExecuteReloadRetreat(); break;
            case AIState.Retreat: ExecuteRetreat(); break;
            case AIState.Reload: ExecuteReload(); break;
        }
    }

    private void ExecuteIdle()
    {
        Soldier.IsAiming = false;

        if (!Agent.isActiveAndEnabled) return;
        if (!Agent.pathPending && Agent.remainingDistance < 1f)
        {
            if (_stateTimer <= 0f)
            {
                PickWanderTarget();
                _stateTimer = IdleWanderInterval;
            }
        }

        if (_playerVisible && Player)
            FaceTarget(Player.position);
    }

    private void ExecuteCombat()
    {
        Soldier.IsAiming = true;

        if (!Agent.isActiveAndEnabled || !Player) return;

        if (!_playerVisible)
        {
            Agent.SetDestination(_lastKnownPosition);
            Soldier.SetSpeed(Soldier.RunSpeed);
            FaceTarget(_lastKnownPosition);
            return;
        }

        Agent.ResetPath();
        Agent.velocity = Vector3.zero;
        Soldier.SetSpeed(0f);

        FaceTarget(Player.position);

        float dist = Vector3.Distance(transform.position, Player.position);
        if (_fireTimer <= 0f && dist <= FireRange)
        {
            FireAtPlayer();
            _fireTimer = FireCooldown;
        }
    }

    private void ExecuteReloadRetreat()
    {
        if (!Agent.isActiveAndEnabled || !Player) return;

        Soldier.StartReload();
        Soldier.IsAiming = false;

        Vector3 awayDir = (transform.position - Player.position).normalized;
        Vector3 target = transform.position + awayDir * 10f;

        NavMeshHit navHit;
        if (NavMesh.SamplePosition(target, out navHit, 5f, NavMesh.AllAreas))
            target = navHit.position;

        Agent.SetDestination(target);
        Soldier.SetSpeed(Soldier.RunSpeed);

        if (Player) FaceTarget(Player.position);
    }

    private void ExecuteRetreat()
    {
        Soldier.IsAiming = true;

        if (!Agent.isActiveAndEnabled || !Player) return;

        if (!_playerVisible)
        {
            Agent.SetDestination(_lastKnownPosition);
            Soldier.SetSpeed(Soldier.RunSpeed);
            FaceTarget(_lastKnownPosition);
            float distToLast = Vector3.Distance(transform.position, _lastKnownPosition);
            if (_fireTimer <= 0f && distToLast <= FireRange)
            {
                FireAtPlayer();
                _fireTimer = FireCooldown;
            }
            return;
        }

        Vector3 awayDir = (transform.position - Player.position).normalized;
        Vector3 target = transform.position + awayDir * 10f;

        NavMeshHit navHit;
        if (NavMesh.SamplePosition(target, out navHit, 5f, NavMesh.AllAreas))
            target = navHit.position;

        Agent.SetDestination(target);
        Soldier.SetSpeed(Soldier.RunSpeed);

        FaceTarget(Player.position);

        float dist = Vector3.Distance(transform.position, Player.position);
        if (_fireTimer <= 0f && dist <= FireRange)
        {
            FireAtPlayer();
            _fireTimer = FireCooldown;
        }
    }

    private void ExecuteReload()
    {
        Soldier.IsAiming = false;

        if (!Agent.isActiveAndEnabled || !Player) return;

        Agent.ResetPath();
        Agent.velocity = Vector3.zero;
        Soldier.SetSpeed(0f);

        if (Player) FaceTarget(Player.position);
    }

    private void FireAtPlayer()
    {
        if (!Soldier.TryFire()) return;

        Soldier.IsShooting = true;

        Vector3 origin = EyeTransform ? EyeTransform.position + Vector3.up * 1.5f : transform.position + Vector3.up * 1.5f;
        Vector3 direction = (Player.position + Vector3.up * 1f) - origin;
        float distance = Mathf.Min(direction.magnitude, FireRange);
        direction.Normalize();

        Debug.DrawRay(origin, direction * distance, Color.red, 1f);

        RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance);
        foreach (var hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;

            IDamagable damagable = hit.collider.GetComponentInParent<IDamagable>();
            if (damagable != null)
            {
                damagable.TakeDamage(Damage, hit.point, hit.normal);
                break;
            }
        }
    }

    private void FaceTarget(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0;
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion lookRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRot, 360f * Time.deltaTime);
        }
    }

    private void PickWanderTarget()
    {
        if (!Agent.isActiveAndEnabled) return;

        Vector3 random = Random.insideUnitSphere * IdleWanderRadius;
        random += transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(random, out hit, IdleWanderRadius, NavMesh.AllAreas))
        {
            _wanderTarget = hit.position;
            Agent.SetDestination(_wanderTarget);
        }
    }

    private void UpdateAnimator()
    {
        float speed = Agent.isActiveAndEnabled ? Agent.velocity.magnitude : 0f;
        Soldier.SetSpeed(speed);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, DetectRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, ReloadRetreatRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, RetreatRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, FireRange);

        if (_playerVisible && Player)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up * 1.5f, Player.position + Vector3.up * 1f);
        }
    }
}