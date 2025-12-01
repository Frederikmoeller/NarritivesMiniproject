using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class ArenaSceneManager : MonoBehaviour
{
    [Header("Fighter Root")]
    public Transform fightersParent;

    [Header("Player")]
    public Health playerHealth;

    [Header("Weapons")]
    public SimpleWeaponController weapons;
    public GameObject stethoscopeObject;   // assign your stethoscope GameObject here
    public bool allowHealing = false;      // ONLY true in final fight

    [Header("Scene Flow")]
    public string nextSceneName;
    public bool isFinalFight = false;

    bool outcomeTriggered = false;
    bool anyEnemyDied = false;     // prevents early “all healed”

    void Start()
    {
        // Always start with the axe, do NOT equip stethoscope automatically.
        weapons.EnableAxe(true);

        // Only final fight allows healing tool to be enabled.
        stethoscopeObject.SetActive(allowHealing);
    }

    void Update()
    {
        // PLAYER DEATH → restart scene
        if (playerHealth != null && playerHealth.isDead)
        {
            if (ArenaResult.Instance)
            {
                ArenaResult.Instance.allKilled = false;
                ArenaResult.Instance.allHealed = false;
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        var fighters = fightersParent.GetComponentsInChildren<SimpleEnemy>();
        if (fighters.Length == 0 || outcomeTriggered) return;

        bool allDead = fighters.All(f => f.isDead);
        bool allAlive = fighters.All(f => !f.isDead);

        // Track if ANY enemy died so we can validate the heal ending.
        if (!anyEnemyDied && fighters.Any(f => f.isDead))
            anyEnemyDied = true;

        // -----------------------------
        // NON-FINAL FIGHTS (1 & 2)
        // -----------------------------
        if (!isFinalFight)
        {
            if (allDead)
            {
                outcomeTriggered = true;
                SceneManager.LoadScene(nextSceneName);
            }
            return;
        }

        // -----------------------------
        // FINAL FIGHT LOGIC
        // -----------------------------
        // KILL ENDING
        if (allDead)
        {
            outcomeTriggered = true;
            ArenaResult.Instance.allKilled = true;
            ArenaResult.Instance.allHealed = false;
            SceneManager.LoadScene(nextSceneName);
        }

        // HEAL ENDING — VALID ONLY IF an enemy died at least once
        if (anyEnemyDied && allAlive)
        {
            outcomeTriggered = true;
            ArenaResult.Instance.allHealed = true;
            ArenaResult.Instance.allKilled = false;
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
