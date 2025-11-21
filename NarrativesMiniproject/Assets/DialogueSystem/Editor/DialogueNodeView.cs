// Assets/DialogueSystem/Editor/DialogueNodeView.cs
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using DialogueSystem;

namespace DialogueSystem.Editor
{
    public class DialogueNodeView : Node
    {
        public DialogueLine NodeData { get; private set; }

        private Port _inputPort;
        private Port _nextPort;
        private List<Port> _choicePorts = new List<Port>();
        private VisualElement _choicesContainer;
        private VisualElement _conditionActionContainer;
        private bool _isChoiceMode => NodeData.choices != null && NodeData.choices.Length > 0;

        public DialogueNodeView(DialogueLine data)
        {
            NodeData = data ?? DialogueGraphSaveUtility.CreateDialogueLine();
            title = string.IsNullOrEmpty(NodeData.textKey) ? "New Node" : NodeData.textKey;
            viewDataKey = NodeData.guid;

            InitializePorts();
            BuildMainContent();
            RefreshExpandedState();
            RefreshPorts();
        }

        #region Ports
        private void InitializePorts()
        {
            _inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            _inputPort.portName = "In";
            inputContainer.Add(_inputPort);

            _nextPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            _nextPort.portName = "Next";
            outputContainer.Add(_nextPort);
        }

        public Port GetInputPort() => _inputPort;
        public Port GetNextPort() => _nextPort;
        public Port GetChoicePort(int index) => (index >= 0 && index < _choicePorts.Count) ? _choicePorts[index] : null;
        #endregion

        private void BuildMainContent()
        {
            // Title is already set
            var speakerField = new TextField("Speaker ID")
            {
                value = NodeData.speakerId
            };
            speakerField.RegisterValueChangedCallback(evt =>
            {
                NodeData.speakerId = evt.newValue;
            });
            mainContainer.Add(speakerField);

            var textKeyField = new TextField("Text Key")
            {
                value = NodeData.textKey
            };
            textKeyField.RegisterValueChangedCallback(evt =>
            {
                NodeData.textKey = evt.newValue;
                title = string.IsNullOrEmpty(NodeData.textKey) ? "New Node" : NodeData.textKey;
            });
            mainContainer.Add(textKeyField);

            // Mode toggle explanation (Yarn-style only one allowed)
            var modeHelp = new Label("Yarn-style: node may have EITHER Next OR Choices.");
            modeHelp.style.unityFontStyleAndWeight = FontStyle.Italic;
            mainContainer.Add(modeHelp);

            // Next vs Choices toggle area
            var modeContainer = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var nextToggle = new Button(() => SwitchToNextMode()) { text = "Use Next (linear)" };
            var choicesToggle = new Button(() => SwitchToChoiceMode()) { text = "Use Choices (branching)" };
            modeContainer.Add(nextToggle);
            modeContainer.Add(choicesToggle);
            mainContainer.Add(modeContainer);

            // Next target label + dropdown (serialized)
            var nextTargetField = new TextField("Next Target GUID")
            {
                value = NodeData.nextNodeId
            };
            nextTargetField.RegisterValueChangedCallback(evt =>
            {
                NodeData.nextNodeId = evt.newValue;
            });
            mainContainer.Add(nextTargetField);

            // Choices container
            _choicesContainer = new VisualElement();
            _choicesContainer.style.flexDirection = FlexDirection.Column;
            mainContainer.Add(_choicesContainer);

            // Condition/action container (shared for node-level commands)
            _conditionActionContainer = new VisualElement();
            _conditionActionContainer.style.flexDirection = FlexDirection.Column;
            mainContainer.Add(_conditionActionContainer);

            // Fill current mode UI
            RefreshModeUI();
        }

        private void RefreshModeUI()
        {
            // 1. Remove old next port if exists
            if (_nextPort != null)
                outputContainer.Remove(_nextPort);

            // 2. Remove old choice ports
            foreach (var p in _choicePorts)
                outputContainer.Remove(p);

            _choicePorts.Clear();
            _choicesContainer.Clear();

            // 3. Choice mode ON
            if (_isChoiceMode)
            {
                // Create a "disabled" next port (visual only, no connections allowed)
                _nextPort = InstantiatePort(
                    Orientation.Horizontal,
                    Direction.Output,
                    Port.Capacity.Single,  // still single, but we won't hook it up
                    typeof(bool)
                );
                _nextPort.portName = "Next (disabled)";
                _nextPort.SetEnabled(false); /// VISUALLY DISABLE

                outputContainer.Add(_nextPort);

                // Create choice ports
                for (int i = 0; i < NodeData.choices.Length; i++)
                {
                    AddChoiceUI(i);

                    var cp = InstantiatePort(
                        Orientation.Horizontal,
                        Direction.Output,
                        Port.Capacity.Single,
                        typeof(bool)
                    );
                    cp.portName = $"Choice {i}";

                    _choicePorts.Add(cp);
                    outputContainer.Add(cp);
                }
            }
            else
            {
                // 4. Choice mode OFF - only one next port
                _nextPort = CreateNextPort(Orientation.Horizontal);
                _nextPort.portName = "Next";

                outputContainer.Add(_nextPort);
            }

            RefreshExpandedState();
            RefreshPorts();
        }

        
        private Port CreateNextPort(Orientation orientation)
        {
            return InstantiatePort(
                orientation,
                Direction.Output,
                Port.Capacity.Single,
                typeof(bool)
            );
        }

        private void AddChoiceUI(int index)
        {
            var choice = NodeData.choices[index];

            var choiceBox = new VisualElement { style = { flexDirection = FlexDirection.Column, borderLeftWidth = 1 } };

            var choiceKey = new TextField("Choice Key") { value = choice.textKey };
            choiceKey.RegisterValueChangedCallback(evt => choice.textKey = evt.newValue);
            choiceBox.Add(choiceKey);

            // Conditions foldout
            var condFold = new Foldout { text = "Conditions" };
            BuildConditionsUI(condFold, choice);
            choiceBox.Add(condFold);

            // Actions foldout
            var actFold = new Foldout { text = "Actions" };
            BuildActionsUI(actFold, choice);
            choiceBox.Add(actFold);

            var targetField = new TextField("Target GUID") { value = choice.nextNodeId };
            targetField.RegisterValueChangedCallback(evt => choice.nextNodeId = evt.newValue);
            choiceBox.Add(targetField);

            var removeBtn = new Button(() =>
            {
                RemoveChoiceAt(index);
            }) { text = "Remove Choice" };
            choiceBox.Add(removeBtn);

            _choicesContainer.Add(choiceBox);
        }

        private void BuildConditionsUI(VisualElement parent, DialogueChoice choice)
        {
            parent.Clear();

            var defs = DialogueGraphSaveUtility.Defs;
            if (defs == null)
            {
                parent.Add(new Label("No Definitions asset assigned."));
                return;
            }

            // List each condition in the choice
            if (choice.Conditions == null) choice.Conditions = new Condition[0];

            for (int i = 0; i < choice.Conditions.Length; i++)
            {
                var cond = choice.Conditions[i];
                var container = new VisualElement { style = { flexDirection = FlexDirection.Row } };

                // Dropdown for available condition IDs
                var pop = new PopupField<string>(defs.conditions.ConvertAll(d => d.id), cond.id);
                pop.RegisterValueChangedCallback(evt =>
                {
                    cond.id = evt.newValue;
                    // adjust args array based on def
                    var def = defs.GetConditionDef(cond.id);
                    if (def != null)
                    {
                        cond.args = new string[def.args.Count];
                    }
                    BuildConditionsUI(parent, choice); // rebuild to show args
                });
                container.Add(pop);

                var remove = new Button(() =>
                {
                    var list = choice.Conditions.ToList();
                    list.RemoveAt(i);
                    choice.Conditions = list.ToArray();
                    BuildConditionsUI(parent, choice);
                }) { text = "Remove" };
                container.Add(remove);

                parent.Add(container);

                // Show arg fields
                var def2 = defs.GetConditionDef(cond.id);
                if (def2 != null)
                {
                    for (int a = 0; a < def2.args.Count; a++)
                    {
                        string argVal = (cond.args != null && a < cond.args.Length) ? cond.args[a] : "";
                        var fld = new TextField(def2.args[a].name) { value = argVal };
                        int captureA = a;
                        fld.RegisterValueChangedCallback(evt =>
                        {
                            if (cond.args == null) cond.args = new string[def2.args.Count];
                            cond.args[captureA] = evt.newValue;
                        });
                        parent.Add(fld);
                    }
                }
            }

            var addBtn = new Button(() =>
            {
                var list = choice.Conditions.ToList();
                Array.Resize(ref choice.Conditions, list.Count + 1);
                choice.Conditions[list.Count] = new Condition() { id = defs.conditions.Count > 0 ? defs.conditions[0].id : "" };
                BuildConditionsUI(parent, choice);
            }) { text = "Add Condition" };
            parent.Add(addBtn);
        }

        private void BuildActionsUI(VisualElement parent, DialogueChoice choice)
        {
            parent.Clear();

            var defs = DialogueGraphSaveUtility.Defs;
            if (defs == null)
            {
                parent.Add(new Label("No Definitions asset assigned."));
                return;
            }

            if (choice.actions == null) choice.actions = new ActionEvent[0];

            for (int i = 0; i < choice.actions.Length; i++)
            {
                var act = choice.actions[i];
                var container = new VisualElement { style = { flexDirection = FlexDirection.Row } };

                var pop = new PopupField<string>(defs.actions.ConvertAll(d => d.id), act.id);
                pop.RegisterValueChangedCallback(evt =>
                {
                    act.id = evt.newValue;
                    var def = defs.GetActionDef(act.id);
                    if (def != null)
                        act.args = new string[def.args.Count];
                    BuildActionsUI(parent, choice);
                });
                container.Add(pop);

                var remove = new Button(() =>
                {
                    var list = choice.actions.ToList();
                    list.RemoveAt(i);
                    choice.actions = list.ToArray();
                    BuildActionsUI(parent, choice);
                }) { text = "Remove" };
                container.Add(remove);

                parent.Add(container);

                var def2 = defs.GetActionDef(act.id);
                if (def2 != null)
                {
                    for (int a = 0; a < def2.args.Count; a++)
                    {
                        string argVal = (act.args != null && a < act.args.Length) ? act.args[a] : "";
                        var fld = new TextField(def2.args[a].name) { value = argVal };
                        int captureA = a;
                        fld.RegisterValueChangedCallback(evt =>
                        {
                            if (act.args == null) act.args = new string[def2.args.Count];
                            act.args[captureA] = evt.newValue;
                        });
                        parent.Add(fld);
                    }
                }
            }

            var addBtn = new Button(() =>
            {
                var list = choice.actions.ToList();
                Array.Resize(ref choice.actions, list.Count + 1);
                choice.actions[list.Count] = new ActionEvent() { id = defs.actions.Count > 0 ? defs.actions[0].id : "" };
                BuildActionsUI(parent, choice);
            }) { text = "Add Action" };
            parent.Add(addBtn);
        }

        #region Choice modifications
        private void RemoveChoiceAt(int index)
        {
            var list = NodeData.choices.ToList();
            list.RemoveAt(index);
            NodeData.choices = list.ToArray();
            RefreshModeUI();
        }

        private void SwitchToChoiceMode()
        {
            if (_isChoiceMode) return;
            // convert nextNodeId -> single default choice if desired, or clear it.
            NodeData.choices = new DialogueChoice[0];
            NodeData.nextNodeId = null;
            RefreshModeUI();
        }

        private void SwitchToNextMode()
        {
            if (!_isChoiceMode) return;
            // convert choices into a single linear continuation? we'll simply clear choices
            NodeData.choices = new DialogueChoice[0];
            RefreshModeUI();
        }

        public void AddChoice()
        {
            var list = (NodeData.choices ?? new DialogueChoice[0]).ToList();
            list.Add(new DialogueChoice { textKey = "choice", nextNodeId = "" , Conditions = new Condition[0], actions = new ActionEvent[0]});
            NodeData.choices = list.ToArray();
            RefreshModeUI();
        }
        #endregion
    }
}
