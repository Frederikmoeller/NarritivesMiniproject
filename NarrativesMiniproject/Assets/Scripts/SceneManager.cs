using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class ArenaSceneManager : MonoBehaviour
{
    [Header("Fighter Root")]
    public Transform fightersParent;

    [Header("Player")]
    public Health playerHealth;

    [Header("Scene Flow")]
    public string nextSceneName;
    public bool isFinalFight = false;

    bool outcomeTriggered = false;

    void Update()
    {
        if (outcomeTriggered) return;

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
        if (fighters.Length == 0) return;

        // -----------------------------
        // NON-FINAL FIGHTS
        // -----------------------------
        if (!isFinalFight)
        {
            if (fighters.All(f => f.isDead))
            {
                outcomeTriggered = true;
                SceneManager.LoadScene(nextSceneName);
            }
            return;
        }

// -----------------------------
// FINAL FIGHT LOGIC
// -----------------------------

bool allDead = true;
bool allHealed = true;

foreach (var f in fighters)
{
    // KILL ENDING (everyone dead)
    if (!f.isDead)
        allDead = false;

    // HEAL ENDING:
    // - must have died at least once
    // - must be alive now
    // - must be fully healed
    if (!f.hasDiedOnce) allHealed = false;
    if (f.isDead)       allHealed = false;
    if (f.health < 100) allHealed = false;
}

// KILL ENDING
if (allDead)
{
    outcomeTriggered = true;
    ArenaResult.Instance.allKilled = true;
    ArenaResult.Instance.allHealed = false;
    SceneManager.LoadScene(nextSceneName);
    return;
}

// HEAL ENDING
if (allHealed)
{
    outcomeTriggered = true;
    ArenaResult.Instance.allKilled = false;
    ArenaResult.Instance.allHealed = true;
    SceneManager.LoadScene(nextSceneName);
    return;
}

    }
}
