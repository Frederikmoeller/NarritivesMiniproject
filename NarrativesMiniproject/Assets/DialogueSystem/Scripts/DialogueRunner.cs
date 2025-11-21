using System;
using System.Collections.Generic;
using DialogueSystem;
using System.Linq;
using UnityEditor.Search;
using UnityEngine;

public class DialogueRunner
{
    private DialogueAsset _asset;
    private DialogueLine _currentNode;

    public event Action<DialogueLine> OnLineDisplayed;
    public event Action<List<DialogueChoice>> OnChoicesDisplayed;
    public event Action OnDialogueEnd;

    public void StartDialogue(DialogueAsset asset)
    {
        _asset = asset;
        _currentNode = FindNode(asset.startNodeId);
        DisplayNode();
    }

    void DisplayNode()
    {
        OnLineDisplayed?.Invoke(_currentNode);

        if (_currentNode.choices != null && _currentNode.choices.Length > 0)
        {
            OnChoicesDisplayed?.Invoke(GetValidChoices(_currentNode));
        }
    }

    public void Choose(int index)
    {
        var choice = _currentNode.choices[index];
        RunActions(choice.actions);
        _currentNode = FindNode(choice.nextNodeId);
        CheckForEndOrContinue();
    }

    DialogueLine FindNode(string id) => _asset.nodes.Find(n => n.nextNodeId == id || n.speakerId == id);

    List<DialogueChoice> GetValidChoices(DialogueLine line) =>
        line.choices.Where(c => AreConditionsMet(c.Conditions)).ToList();

    bool AreConditionsMet(Condition[] conditions)
    {
        if (conditions == null) return true;
        foreach (var condition in conditions)
        {
            if (!DialogueConditionSystem.Check(condition)) return false;
        }
        return true;
    }

    void RunActions(ActionEvent[] actions)
    {
        if (actions == null) return;
        foreach (var action in actions)
        {
            DialogueActionSystem.Run(action);
        }
    }

    void CheckForEndOrContinue()
    {
        if (_currentNode == null)
        {
            OnDialogueEnd?.Invoke();
            return;
        }
        DisplayNode();
    }
}
