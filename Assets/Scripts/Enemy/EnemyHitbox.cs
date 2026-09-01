using UnityEngine;

public enum BodyPart
{
    Torso,
    Arms,
    Legs,
    Feet,
    Head
}

public class EnemyHitbox : MonoBehaviour
{
    public BodyPart BodyPart = BodyPart.Torso;
    public Enemy Enemy;
    public float DamageMultiplier = 1f;

    private void Awake()
    {
        if (!Enemy)
            Enemy = GetComponentInParent<Enemy>();
    }

    public void ProcessDamage(float baseDamage, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (!Enemy)
        {
            Enemy = GetComponentInParent<Enemy>();
            if (!Enemy) return;
        }
        float dmg = baseDamage * DamageMultiplier;
        Debug.Log($"EnemyHitbox {gameObject.name}: dmg={dmg} to {Enemy.name} hp_before={Enemy.Health}");
        Enemy.TakeDamage(dmg, hitPoint, hitNormal);
    }
}
