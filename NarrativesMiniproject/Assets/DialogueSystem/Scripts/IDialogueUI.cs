using System.Collections.Generic;
using DialogueSystem;
using UnityEngine;

public interface IDialogueUI
{
    void ShowLine(DialogueLine line);
    void ShowChoices(List<DialogueChoice> choices);
    void HideAll();
}
