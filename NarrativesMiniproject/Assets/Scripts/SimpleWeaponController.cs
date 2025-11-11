using UnityEngine;
using System.Collections;

public class SimpleWeaponController : MonoBehaviour
{
    public Transform weaponHolder;
    public GameObject axe;
    public GameObject stethoscope;
    public float swingSpeed = 8f;
    public float swingAngle = 45f;

    [Header("Debug")]
    public bool debugLogs = true;

    GameObject currentWeapon;
    bool isSwinging;
    WeaponHitbox weaponHitbox;

    void Start()
    {
        EquipWeapon(axe);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isSwinging)
            StartCoroutine(SwingWeapon());

        if (Input.GetKeyDown(KeyCode.Alpha1)) EquipWeapon(axe);
        if (Input.GetKeyDown(KeyCode.Alpha2)) EquipWeapon(stethoscope);
    }

    void EquipWeapon(GameObject newWeapon)
    {
        if (currentWeapon) currentWeapon.SetActive(false);
        currentWeapon = newWeapon;
        currentWeapon.SetActive(true);
        weaponHitbox = currentWeapon.GetComponent<WeaponHitbox>();

        if (weaponHitbox == null)
            Debug.LogError($"[WeaponController] '{currentWeapon.name}' is missing WeaponHitbox. Add it and set isHealingTool accordingly.");
        else if (debugLogs)
            Debug.Log($"[WeaponController] Equipped '{currentWeapon.name}' | healing={weaponHitbox.isHealingTool}");
    }

    IEnumerator SwingWeapon()
    {
        isSwinging = true;
        Quaternion startRot = weaponHolder.localRotation;
        Quaternion downRot = startRot * Quaternion.Euler(0, 0, swingAngle);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * swingSpeed;
            weaponHolder.localRotation = Quaternion.Slerp(startRot, downRot, Mathf.Sin(t * Mathf.PI));

            if (t >= 0.45f && t <= 0.55f && weaponHitbox != null)
            {
                if (debugLogs) Debug.Log($"[WeaponController] Swing midpoint -> TryHit() on '{currentWeapon.name}'");
                weaponHitbox.TryHit();
            }

            yield return null;
        }

        weaponHolder.localRotation = startRot;
        isSwinging = false;
    }
}
