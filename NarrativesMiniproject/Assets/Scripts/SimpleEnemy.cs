using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class SimpleEnemy : MonoBehaviour
{
    public float health = 100f;
    public float detectRange = 15f;
    public float attackRange = 2f;
    public float moveSpeed = 3.5f;
    public float attackCooldown = 2f;
    public float hitCooldown = 0.5f;
    public Animator animator;
    public SimpleEnemy partner;          // assign in Inspector
    public bool fakeFighter = true;      // true = fake fighting pair

    enum State { FakeFight, Aggro, Dead }
    State state = State.FakeFight;

    Transform player;
    NavMeshAgent agent;
    bool isAttacking;
    bool nextAttackLeft = true;
    float nextAttackTime;
    float nextHitTime;
    bool isDead;


void Start()
{
    player = GameObject.FindGameObjectWithTag("Player")?.transform;
    agent = GetComponent<NavMeshAgent>();
    animator = GetComponent<Animator>();
    agent.speed = moveSpeed;

    if (fakeFighter)
        StartCoroutine(FakeFightRoutine());
}


void Update()
{
    if (isDead || player == null) return;

    if (state == State.FakeFight)
    {
        float playerDist = Vector3.Distance(transform.position, player.position);
        if (playerDist <= detectRange)
            TriggerAggro();
        return;
    }

    if (state == State.Aggro)
    {
        float dist = Vector3.Distance(transform.position, player.position);

        if (!isAttacking)
        {
            agent.SetDestination(player.position);
            animator.SetBool("isRunning", dist > attackRange);
        }

        if (dist <= attackRange && Time.time >= nextAttackTime)
            StartCoroutine(PerformAttack());
    }
}

    IEnumerator FakeFightRoutine()
    {
        while (!isDead && state == State.FakeFight)
        {
            if (!isAttacking)
            {
                animator.SetTrigger(nextAttackLeft ? "attackLeft" : "attackRight");
                nextAttackLeft = !nextAttackLeft;
            }

                // small delay between each fake swing
                yield return new WaitForSeconds(1.2f);
        }
    }


    void TriggerAggro()
    {
        state = State.Aggro;
        fakeFighter = false;
        if (partner != null && !partner.isDead)
        {
            // randomly kill one of the two
            if (Random.value < 0.5f) partner.DieOnce();
            else DieOnce();
        }
    }


    IEnumerator PerformAttack()
    {
        isAttacking = true;
        agent.isStopped = true;
        animator.SetBool("isRunning", false);

        if (nextAttackLeft)
            animator.SetTrigger("attackLeft");
        else
            animator.SetTrigger("attackRight");

        nextAttackLeft = !nextAttackLeft;
        nextAttackTime = Time.time + attackCooldown;

        yield return new WaitForSeconds(1.2f);

        isAttacking = false;
        agent.isStopped = false;
    }

    public void TakeDamage(float dmg)
    {
        if (isDead || Time.time < nextHitTime) return;

        nextHitTime = Time.time + hitCooldown;

        health -= dmg;
        animator.SetTrigger("damage");

        if (health <= 0f)
        {
            DieOnce();
        }
    }

    void DieOnce()
    {
        if (isDead) return;

        isDead = true;
        animator.SetTrigger("die");
        animator.SetBool("isRunning", false);

        agent.isStopped = true;
        agent.enabled = false;

        // Disable collider and rigidbody physics so corpses don't spin
        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

}
