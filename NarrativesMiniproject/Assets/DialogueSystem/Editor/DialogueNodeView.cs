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
        private DialogueGraphView _graphView;

        private Port _inputPort;
        private Port _nextPort;
        private List<Port> _choicePorts = new List<Port>();
        private VisualElement _choicesContainer;
        private VisualElement _conditionActionContainer;

        public DialogueNodeView(DialogueLine data, DialogueGraphView graphView = null)
        {
            _graphView = graphView;
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
    
            // Register with edge connector
            if (_graphView != null)
                _graphView.RegisterPortToEdgeConnector(_nextPort);
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
                UpdateNodeTitle();
            });
            mainContainer.Add(speakerField);

            var textKeyField = new TextField("Text Key")
            {
                value = NodeData.textKey
            };
            textKeyField.RegisterValueChangedCallback(evt =>
            {
                NodeData.textKey = evt.newValue;
                UpdateNodeTitle();
            });
            mainContainer.Add(textKeyField);

            // Next vs Choices toggle area
            var addChoiceBtn = new Button(AddChoice) { text = "Add Choice" };
            mainContainer.Add(addChoiceBtn);

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

        void UpdateNodeTitle()
        {
            string key = string.IsNullOrEmpty(NodeData.textKey) ? "New Node" : NodeData.textKey;
            string speaker = string.IsNullOrEmpty(NodeData.speakerId) ? "" : $"{NodeData.speakerId}: ";
            title = speaker + key;
        }
        
        private void RefreshModeUI()
        {
            // Collect edges that will be disconnected before changing ports
            var edgesToRemove = new List<Edge>();
    
            if (NodeData.choices.Length > 0 && _nextPort != null)
            {
                edgesToRemove.AddRange(_nextPort.connections);
                // Clear the nextNodeId since we're switching to choice mode
                if (!string.IsNullOrEmpty(NodeData.nextNodeId))
                {
                    Debug.Log($"Clearing nextNodeId '{NodeData.nextNodeId}' when switching to choice mode");
                    NodeData.nextNodeId = null;
                }
            }
    
            // 2. Remove edges from any choice ports that will be removed
            // This covers both reducing choice count AND switching to next port mode (where choices.Length == 0)
            for (int i = NodeData.choices.Length; i < _choicePorts.Count; i++)
            {
                edgesToRemove.AddRange(_choicePorts[i].connections);
            }
            
            // Always clear and rebuild choices UI
            _choicesContainer.Clear();
            for (int i = 0; i < NodeData.choices.Length; i++)
            {
                AddChoiceUI(i);
            }

            // Clear output container and rebuild based on choices count
            outputContainer.Clear();

            if (NodeData.choices.Length > 0)
            {
                // CHOICE MODE: Create choice ports
                _nextPort = null; // Clear next port in choice mode
        
                // Ensure we have the right number of choice ports
                while (_choicePorts.Count < NodeData.choices.Length)
                {
                    var cp = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                    cp.portName = $"Choice {_choicePorts.Count}";
                    _choicePorts.Add(cp);
                    outputContainer.Add(cp);

                    // Register with edge connector
                    if (_graphView != null)
                        _graphView.RegisterPortToEdgeConnector(cp);
                }
        
                // Remove excess choice ports
                while (_choicePorts.Count > NodeData.choices.Length)
                {
                    _choicePorts.RemoveAt(_choicePorts.Count - 1);
                }
        
                // Add current choice ports to output container with choice key as name
                for (int i = 0; i < _choicePorts.Count; i++)
                {
                    var choice = NodeData.choices[i];
                    var portName = string.IsNullOrEmpty(choice.textKey) ? $"Choice {i}" : choice.textKey;
                    _choicePorts[i].portName = portName;
                    outputContainer.Add(_choicePorts[i]);
                }
            }
            else
            {
                // NEXT PORT MODE: Use single next port
                _choicePorts.Clear(); // Clear all choice ports
        
                if (_nextPort == null)
                {
                    _nextPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                    _nextPort.portName = "Next";
                }
                outputContainer.Add(_nextPort);
            }

            RefreshExpandedState();
            RefreshPorts();
    
            // Remove the disconnected edges
            if (_graphView != null && edgesToRemove.Count > 0)
            {
                foreach (var edge in edgesToRemove)
                {
                    _graphView.RemoveElement(edge);
                }
                Debug.Log($"Removed {edgesToRemove.Count} disconnected edges due to port mode change");
            }
        }
        
        private Port GetMatchingPortForEdge(Edge edge)
        {
            if (NodeData.choices.Length == 0)
            {
                return _nextPort;
            }
            else
            {
                // Try to find which choice port this edge should connect to
                // This is complex - you might need to store which choice index an edge belongs to
                return _choicePorts.Count > 0 ? _choicePorts[0] : null;
            }
        }

        private void RefreshChoicesUi()
        {
            _choicesContainer.Clear();
            for (int i = 0; i < NodeData.choices.Length; i++)
            {
                AddChoiceUI(i);
            }
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
            choiceKey.RegisterValueChangedCallback(evt => 
            { 
                choice.textKey = evt.newValue;
                // Update the port name when choice key changes
                UpdateChoicePortName(index);
            });
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

        // Add this helper method to update individual port names
        private void UpdateChoicePortName(int index)
        {
            if (index >= 0 && index < _choicePorts.Count)
            {
                var choice = NodeData.choices[index];
                var portName = string.IsNullOrEmpty(choice.textKey) ? $"Choice {index}" : choice.textKey;
                _choicePorts[index].portName = portName;
        
                // Refresh the port visual
                _choicePorts[index].MarkDirtyRepaint();
            }
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
                int conditionIndex = i;
                var cond = choice.Conditions[i];
                var container = new VisualElement { style = { flexDirection = FlexDirection.Row } };

                var conditionChoices = defs.conditions.ConvertAll(d => d.displayName);
                var currentIndex = defs.conditions.FindIndex(c => c.id == cond.id);
                var pop = new PopupField<string>(conditionChoices, currentIndex >= 0 ? currentIndex : 0);

                pop.RegisterValueChangedCallback(evt =>
                {
                    var selectedDef = defs.conditions.Find(c => c.displayName == evt.newValue);
                    if (selectedDef != null)
                    {
                        cond.id = selectedDef.id;
                        // adjust args array based on def
                        cond.args = new string[selectedDef.args.Count];
                        BuildConditionsUI(parent, choice); // rebuild to show args
                    }
                });
                container.Add(pop);

                var remove = new Button(() =>
                {
                    var list = choice.Conditions.ToList();
                    list.RemoveAt(conditionIndex);
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
                        var argDef = def2.args[a];
                        var fld = new TextField(argDef.name) { 
                            value = argVal,
                            tooltip = argDef.placeholder // Show placeholder as tooltip
                        };
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
                int actionIndex = i;
                var act = choice.actions[i];
                var container = new VisualElement { style = { flexDirection = FlexDirection.Row } };

                var actionChoices = defs.actions.ConvertAll(d => d.displayName);
                var currentIndex = defs.actions.FindIndex(a => a.id == act.id);
                var pop = new PopupField<string>(actionChoices, currentIndex >= 0 ? currentIndex : 0);
                
                pop.RegisterValueChangedCallback(evt =>
                {
                    var selectedDef = defs.actions.Find(a => a.displayName == evt.newValue);
                    if (selectedDef != null)
                    {
                        act.id = selectedDef.id;
                        var def = defs.GetActionDef(act.id);
                        if (def != null)
                            act.args = new string[def.args.Count];
                        BuildActionsUI(parent, choice);
                    }
                });
                container.Add(pop);

                var remove = new Button(() =>
                {
                    var list = choice.actions.ToList();
                    list.RemoveAt(actionIndex);
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
                        var argDef = def2.args[a];
                        var fld = new TextField(argDef.name) { 
                            value = argVal,
                            tooltip = argDef.placeholder // Show placeholder as tooltip
                        };
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
        
        private TextField CreateArgumentField(string label, string value, string placeholder)
        {
            var field = new TextField(label) { value = value };
    
            // Use placeholder as tooltip for now
            field.tooltip = placeholder;
    
            // Optional: Add placeholder text as watermark (more complex)
            // field.AddToClassList("argument-field");
            // Then use USS to style empty fields
    
            return field;
        }

        #region Choice modifications
        private void RemoveChoiceAt(int index)
        {
            var list = NodeData.choices.ToList();
            list.RemoveAt(index);
            NodeData.choices = list.ToArray();
        
            // Simply refresh - the logic above will handle switching modes
            RefreshModeUI();
        }
        public void AddChoice()
        {
            var list = (NodeData.choices ?? new DialogueChoice[0]).ToList();
            list.Add(new DialogueChoice { 
                textKey = "choice", 
                nextNodeId = "", 
                Conditions = new Condition[0], 
                actions = new ActionEvent[0]
            });
            NodeData.choices = list.ToArray();
        
            // Simply refresh - the logic above will handle switching modes
            RefreshModeUI();
        }
        
        public int GetChoicePortIndex(Port port)
        {
            for (int i = 0; i < _choicePorts.Count; i++)
            {
                if (_choicePorts[i] == port)
                    return i;
            }
            return -1;
        }
        #endregion
    }
}
