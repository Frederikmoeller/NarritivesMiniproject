using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
public class NPCController : MonoBehaviour
{
    public int pairId = 0;                          // set in inspector per pair
    public float aggroRadius = 6f;                 // when player close, trigger
    public float attackRange = 1.6f;
    public float attackCooldown = 1.0f;
    public float attackDamage = 25f;
    public bool isAlive = true;

    NavMeshAgent agent;
    Health hp;
    Transform player;
    NPCController partner;
    enum State { FakeFight, Idle, Chasing, Attacking, Dead }
    State state = State.FakeFight;
    float lastAttackTime;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        hp = GetComponent<Health>();
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        // find partner by pairId (first other Enemy with same pairId)
        var all = FindObjectsOfType<NPCController>();
        foreach (var n in all)
            if (n != this && n.pairId == pairId)
            { partner = n; break; }

        StartCoroutine(FakeFightLoop());
    }

    IEnumerator FakeFightLoop()
    {
        // local animation placeholder: simple rotate or step
        while (isAlive && state == State.FakeFight)
        {
            // simple "sway" to fake fighting; replace with animator triggers if present
            transform.Rotate(0, 20f * Time.deltaTime, 0);
            // check for player proximity
            if (player && Vector3.Distance(transform.position, player.position) < aggroRadius)
            {
                DecideOutcome();
                yield break;
            }
            yield return null;
        }
    }

    void DecideOutcome()
    {
        // deterministic pick: the one with lower random wins, create variety
        float mine = Random.value;
        float theirs = partner ? Random.value : 1f;
        if (mine < theirs)
        {
            // this one loses (dies)
            hp.TakeDamage(hp.currentHealth + 1f);
            // partner will chase - notify partner
            if (partner) partner.OnPartnerWins(transform.position);
        }
        else
        {
            // partner dies, this one chases
            if (partner) partner.GetComponent<Health>().TakeDamage(partner.GetComponent<Health>().currentHealth + 1f);
            OnPartnerWins(transform.position);
        }
    }

    // called on winner
    public void OnPartnerWins(Vector3 loserPos)
    {
        if (!isAlive) return;
        state = State.Chasing;
        agent.isStopped = false;
        StopAllCoroutines();
    }

    void Update()
    {
        if (!isAlive) return;
        if (hp.isDead && state != State.Dead) { OnDeath(); return; }

        if (state == State.Chasing)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist > aggroRadius * 1.5f) { state = State.Idle; agent.isStopped = true; return; }
            if (dist > attackRange)
            {
                agent.SetDestination(player.position);
            }
            else
            {
                agent.isStopped = true;
                if (Time.time - lastAttackTime > attackCooldown)
                {
                    lastAttackTime = Time.time;
                    StartCoroutine(DoAttack());
                }
            }
        }
    }

    IEnumerator DoAttack()
    {
        state = State.Attacking;
        // simple forward lunge
        Vector3 orig = transform.position;
        Vector3 toward = (player.position - transform.position).normalized;
        float t = 0f;
        float dur = 0.4f;
        while (t < dur)
        {
            t += Time.deltaTime;
            agent.Move(toward * (3f * Time.deltaTime)); // small step
            yield return null;
        }
        // apply damage if still in range
        if (Vector3.Distance(transform.position, player.position) <= attackRange + 0.5f)
        {
            var pHealth = player.GetComponent<Health>();
            if (pHealth) pHealth.TakeDamage(attackDamage);
        }
        state = State.Chasing;
    }

    void OnDeath()
    {
        isAlive = false;
        state = State.Dead;
        agent.isStopped = true;
        GetComponent<Collider>().enabled = false;
        // optional: play death animation / enable ragdoll
        // remove enemy tag so further logic doesn't target it
        gameObject.tag = "Untagged";
    }

    // receive external damage
    void OnDamageTaken(float dmg)
    {
        hp.TakeDamage(dmg);
    }
}
