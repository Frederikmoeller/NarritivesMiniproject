using System;
using System.Collections.Generic;
using System.Linq;
using DialogueSystem.Data;
using DialogueSystem.Save;
using UnityEngine;

namespace DialogueSystem
{
    public class DialogueRunner
    {
        private DialogueAsset _asset;
        private DialogueLine _currentNode;

        public event Action<DialogueLine> OnLineDisplayed;
        public event Action<List<DialogueChoice>> OnChoicesDisplayed;
        public event Action OnDialogueEnd;

        public void StartDialogue(DialogueAsset asset, bool useSavedState = true)
        {
            _asset = asset;

            if (useSavedState)
            {
                LoadState();
            }
            else
            {
                _currentNode = FindNode(asset.startNodeId);
                DisplayNode();
            }
        }

        void DisplayNode()
        {
            OnLineDisplayed?.Invoke(_currentNode);

            if (_currentNode.Choices != null && _currentNode.Choices.Length > 0)
            {
                OnChoicesDisplayed?.Invoke(GetValidChoices(_currentNode));
            }
        }

        public void Choose(int index)
        {
            var choice = _currentNode.Choices[index];
            if (_asset != null)
            {
                string choiceId = choice.TextKey ?? $"Choice_{index}";
                DialogueSaveManager.SaveChoice(_asset.name, choiceId);
            }
            RunActions(choice.Actions);
            _currentNode = FindNode(choice.NextNodeId);
            CheckForEndOrContinue();
        }

        DialogueLine FindNode(string id) => _asset.nodes.Find(n => n.NextNodeId == id || n.SpeakerId == id);

        List<DialogueChoice> GetValidChoices(DialogueLine line) =>
            line.Choices.Where(c => AreConditionsMet(c.Conditions)).ToList();

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

        public void SaveState()
        {
            if (_currentNode != null && _asset != null)
            {
                DialogueSaveManager.SaveDialogueState(_asset.name, _currentNode.Guid);
            }
        }

        public void LoadState()
        {
            if (_asset != null)
            {
                string savedNodeId = DialogueSaveManager.LoadDialogueState(_asset.name);
                if (!string.IsNullOrEmpty(savedNodeId))
                {
                    _currentNode = FindNode(savedNodeId);
                    if (_currentNode != null)
                    {
                        Debug.Log($"Loaded saved state for {_asset.name}");
                        DisplayNode();
                        return;
                    }
                }
                // No saved state, start normally
                _currentNode = FindNode(_asset.startNodeId);
                DisplayNode();
            }
        }
    }
}
