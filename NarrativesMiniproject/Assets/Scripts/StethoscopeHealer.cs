using UnityEngine;
using System.Collections;

public class StethoscopeHealer : MonoBehaviour
{
    public float healAmount = 34f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            StartCoroutine(HealSwing());
    }

    private IEnumerator HealSwing()
    {
        yield return null;
        // nothing else – all swing and hit handled by SimpleWeaponController + WeaponHitbox
    }
}
