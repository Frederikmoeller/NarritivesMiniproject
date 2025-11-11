using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class SimpleEnemy : MonoBehaviour
{
    [Header("Stats")]
    public float health = 100f;
    public float detectRange = 15f;
    public float attackRange = 2f;
    public float moveSpeed = 3.5f;
    public float attackCooldown = 2f;
    public float hitCooldown = 0.5f;

    [Header("Refs")]
    public Animator animator;
    public SimpleEnemy partner;
    public bool fakeFighter = true;

    static int globalDeathsTriggered = 0;

    enum State { FakeFight, Aggro, Dead }
    State state = State.FakeFight;

    Transform player;
    NavMeshAgent agent;
    bool isAttacking;
    bool nextAttackLeft = true;
    float nextAttackTime;
    float nextHitTime;
    public bool isDead;

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
            yield return new WaitForSeconds(1.2f);
        }
    }

    void TriggerAggro()
    {
        state = State.Aggro;
        fakeFighter = false;

        // only the first three fake-fight interruptions kill one random fighter
        if (globalDeathsTriggered < 3 && partner != null && !partner.isDead)
        {
            if (Random.value < 0.5f) partner.ForceDie();
            else ForceDie();

            globalDeathsTriggered++;
            return;
        }
        // beyond first three interruptions both survive
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;
        agent.isStopped = true;
        animator.SetBool("isRunning", false);

        if (nextAttackLeft) animator.SetTrigger("attackLeft");
        else animator.SetTrigger("attackRight");

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

        if (health <= 0f)
        {
            health = 0f;
            isDead = true;
            DoDeathSequence();
            return;
        }

        animator.ResetTrigger("die");
        animator.SetTrigger("damage");
    }

    // --- central death sequence ---
    void DoDeathSequence()
    {
        state = State.Dead;

        animator.ResetTrigger("damage");
        animator.SetTrigger("die");
        animator.SetBool("isRunning", false);

        if (agent)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;

        var rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    // normal death path
    void DieOnce()
    {
        if (isDead) return;
        isDead = true;
        DoDeathSequence();
    }

    // forced fake-fight kill
    void ForceDie()
    {
        if (isDead) return;
        isDead = true;
        DoDeathSequence();
    }

    public void Heal(float amount)
    {
        if (!isDead) return;

        health += amount;
        if (health > 100f) health = 100f;

        if (health >= 100f)
        {
            isDead = false;
            state = State.Aggro;
            fakeFighter = false;

            animator.ResetTrigger("die");
            animator.SetTrigger("revive");
            animator.SetBool("isRunning", false);

            var col = GetComponent<Collider>();
            if (col) { col.isTrigger = false; col.enabled = true; }

            var rb = GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.constraints = RigidbodyConstraints.FreezeRotation; // lock rotation axes
            }

            if (agent)
            {
                agent.enabled = true;
                agent.isStopped = true;
                agent.speed = 0f;
                agent.angularSpeed = 0f;
                agent.acceleration = 0f;
                agent.updatePosition = true;
                agent.updateRotation = false;
            }
        }
    }
}
