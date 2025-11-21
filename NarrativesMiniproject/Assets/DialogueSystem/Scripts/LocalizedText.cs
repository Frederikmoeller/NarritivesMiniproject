using UnityEngine;
using TMPro;

public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string localizationKey;

    void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        GetComponent<TMP_Text>().text = LocalizationSystem.Get(localizationKey);
    }
}
