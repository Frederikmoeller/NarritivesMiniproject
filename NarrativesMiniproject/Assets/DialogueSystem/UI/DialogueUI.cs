using System.Collections.Generic;
using DialogueSystem.Data;
using DialogueSystem.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueSystem.UI
{
    public class DialogueUI : MonoBehaviour, IDialogueUI
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private Transform choicesContainer;
        [SerializeField] private Button choiceButtonPrefab;

        public void ShowLine(DialogueLine line)
        {
            text.text = LocalizationSystem.Get(line.TextKey);
        }

        public void ShowChoices(List<DialogueChoice> choices)
        {
            // create buttons here
        }

        public void HideAll()
        {
            text.gameObject.SetActive(false);
            choicesContainer.gameObject.SetActive(false);
        }
    }
}
