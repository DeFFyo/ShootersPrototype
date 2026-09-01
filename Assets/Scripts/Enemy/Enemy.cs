using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class Enemy : MonoBehaviour, IDamagable
{
    [Header("Model")]
    public Transform Model;
    public bool IsCalm = true;

    [Header("Health")]
    public float MaxHealth = 50f;
    public float Health;

    protected virtual void Awake()
    {
        Health = MaxHealth;
    }

    public virtual void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        Health -= amount;

        if (Health <= 0f)
            Die();
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }
}
