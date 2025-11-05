using UnityEngine;

public class WeaponHitbox : MonoBehaviour
{
    public float damage = 25f;
    public LayerMask hitMask;

    public void TryHit()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 1.5f, hitMask);
        foreach (var h in hits)
        {
            SimpleEnemy enemy = h.GetComponent<SimpleEnemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
    }
}
