using UnityEngine;

public class WeaponHitbox : MonoBehaviour
{
    [Header("Weapon")]
    public float damage = 50f;
    public bool isHealingTool = false;   // true for stethoscope
    public float healAmount = 34f;

    [Header("Debug")]
    public bool debugLogs = true;
    public float debugHitRadius = 1f;

    // Called from SimpleWeaponController during swing midpoint
    public void TryHit()
    {
        if (debugLogs) Debug.Log($"[WeaponHitbox] TryHit() on '{name}' | healing={isHealingTool} | pos={transform.position}");

        Collider[] hits = Physics.OverlapSphere(transform.position, debugHitRadius);
        if (debugLogs) Debug.Log($"[WeaponHitbox] OverlapSphere count={hits.Length}");

        foreach (var hit in hits)
        {
            var enemy = hit.GetComponentInParent<SimpleEnemy>();
            if (enemy == null)
            {
                if (debugLogs) Debug.Log($"[WeaponHitbox] Skipped non-enemy collider '{hit.name}'");
                continue;
            }

            if (isHealingTool)
            {
                float pre = enemy.health;
                bool wasDead = enemy.isDead;
                enemy.Heal(healAmount);
                if (debugLogs) Debug.Log($"[WeaponHitbox] HEAL -> '{enemy.name}' pre={pre} +{healAmount} -> {enemy.health} | wasDead={wasDead} nowDead={enemy.isDead}");
            }
            else
            {
                float pre = enemy.health;
                enemy.TakeDamage(damage);
                if (debugLogs) Debug.Log($"[WeaponHitbox] DAMAGE -> '{enemy.name}' pre={pre} -{damage} -> {enemy.health}");
            }
        }
    }

    // Visualize hit volume
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, debugHitRadius);
    }
}
