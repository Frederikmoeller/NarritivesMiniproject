using UnityEngine;
using DialogueSystem;
public class DialogueManager : MonoBehaviour
{
    public DialogueAsset startAsset;
    public IDialogueUI ui;

    private DialogueRunner runner;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LocalizationSystem.Load("Dialogue.csv");
        LocalizationSystem.SetLanguage("EN");
        runner = new DialogueRunner();
        runner.OnLineDisplayed += ui.ShowLine;
        runner.OnChoicesDisplayed += ui.ShowChoices;
        runner.OnDialogueEnd += ui.HideAll;
        
        runner.StartDialogue(startAsset);
    }
}
