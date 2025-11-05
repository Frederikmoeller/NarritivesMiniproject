using UnityEngine;
public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;
    public bool isDead => currentHealth <= 0f;

    void Awake() { currentHealth = maxHealth; }

    public void TakeDamage(float damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        if (currentHealth <= 0f) Die();
    }

    void Die()
    {
        currentHealth = 0f;
        SendMessage("OnDeath", SendMessageOptions.DontRequireReceiver);
    }
}
