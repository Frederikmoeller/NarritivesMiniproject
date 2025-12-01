using UnityEngine;

public class ArenaResult : MonoBehaviour
{
    public static ArenaResult Instance;

    public bool allKilled = false;
    public bool allHealed = false;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
