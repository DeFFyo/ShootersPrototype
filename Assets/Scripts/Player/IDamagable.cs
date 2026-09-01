using UnityEngine;

public interface IDamagable
{
    void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal);
}
