using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;
    public bool isDead => currentHealth <= 0f;

    // NEW:
    float lastDamageTime = -999f;
    public float damageCooldown = 1.2f;

    void Awake() { currentHealth = maxHealth; }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        // NEW: cooldown check
        if (Time.time < lastDamageTime + damageCooldown)
            return;

        lastDamageTime = Time.time;

        currentHealth -= damage;
        if (currentHealth <= 0f) Die();
    }

    void Die()
    {
        currentHealth = 0f;
        SendMessage("OnDeath", SendMessageOptions.DontRequireReceiver);
    }
}
