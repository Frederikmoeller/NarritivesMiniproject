using UnityEngine;

public class ShadowBlobFollow : MonoBehaviour
{
    public Transform player;
    public float offsetY = 0.02f;

    void LateUpdate()
    {
        if (Physics.Raycast(player.position, Vector3.down, out RaycastHit hit, 5f))
            transform.position = hit.point + Vector3.up * offsetY;
    }
}
