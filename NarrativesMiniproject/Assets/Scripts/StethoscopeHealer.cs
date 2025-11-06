using UnityEngine;

public class StethoscopeHealer : MonoBehaviour
{
    public float healAmount = 34f;
    public float hitRange = 2f;
    public LayerMask healableMask;
    public Transform hitOrigin; // point in front of player or weapon tip

    private bool isHitting;
    private Animator anim;

    void Start()
    {
        anim = GetComponentInParent<Animator>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isHitting)
            StartCoroutine(HealSwing());
    }

    private System.Collections.IEnumerator HealSwing()
    {
        isHitting = true;
        anim.SetTrigger("Swing"); // uses existing rotation animation
        yield return new WaitForSeconds(0.3f); // timing when swing lands
        TryHeal();
        yield return new WaitForSeconds(0.4f);
        isHitting = false;
    }

    private void TryHeal()
    {
        if (Physics.Raycast(hitOrigin.position, hitOrigin.forward, out RaycastHit hit, hitRange, healableMask))
        {
            var health = hit.collider.GetComponentInParent<SimpleEnemy>();
            if (health != null && health.isDead)
            {
                health.Heal(healAmount);
            }
        }
    }
}
