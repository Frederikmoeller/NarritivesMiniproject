using DialogueSystem.UI;
using DialogueSystem.Data;
using DialogueSystem.Localization;
using UnityEngine;

namespace DialogueSystem
{
    public class DialogueManager : MonoBehaviour
    {
        public DialogueAsset StartAsset;
        public IDialogueUI Ui;

        private DialogueRunner _runner;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            LocalizationSystem.Load("Dialogue.csv");
            LocalizationSystem.SetLanguage("EN");
            _runner = new DialogueRunner();
            _runner.OnLineDisplayed += Ui.ShowLine;
            _runner.OnChoicesDisplayed += Ui.ShowChoices;
            _runner.OnDialogueEnd += Ui.HideAll;
        
            _runner.StartDialogue(StartAsset);
        }
    }
}
