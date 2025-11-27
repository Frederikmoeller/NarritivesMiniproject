using System;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using DialogueSystem;
using UnityEditor.UIElements;

namespace DialogueSystem.Editor
{
    public class DialogueGraphEditor : EditorWindow
    {
        private DialogueGraphView _graphView;
        private string _currentAssetPath;
        public DialogueAsset CurrentAsset => _loadedAsset;
        private DialogueAsset _loadedAsset;
        private VisualElement _toolbarContainer;
        private Foldout _definitionsFoldout;
        private VisualElement _definitionsContainer;

        [MenuItem("Window/Dialogue System/Dialogue Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<DialogueGraphEditor>();
            window.titleContent = new GUIContent("Dialogue Editor");
        }

        private void OnEnable()
        {
            ContructGraphView();
            ContructToolbar();
            CreateDefinitionsPanel();
        }

        private void OnDisable()
        {
            rootVisualElement.Remove(_graphView);
        }
        
        private void CreateDefinitionsPanel()
        {
            _definitionsFoldout = new Foldout { 
                text = "Dialogue Definitions", 
                value = false,
                style =
                {
                    marginTop = 10,
                    marginBottom = 10,
                    marginRight = 10
                }
            };

            // Create a scroll view for the definitions
            var scrollView = new ScrollView
            {
                style =
                {
                    backgroundColor = new Color(0.22f, 0.22f, 0.22f, 1f),
                    paddingTop = 10,
                    paddingBottom = 10,
                    paddingLeft = 15,
                    paddingRight = 15,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderTopColor = new Color(0.3f, 0.3f, 0.3f, 1f),
                    borderBottomColor = new Color(0.3f, 0.3f, 0.3f, 1f)
                }
            };

            _definitionsContainer = new VisualElement();
            scrollView.Add(_definitionsContainer);
            _definitionsFoldout.Add(scrollView);
    
            rootVisualElement.Add(_definitionsFoldout);
    
            RefreshDefinitionsUI();
        }
        
        private void RefreshDefinitionsUI()
        {
            _definitionsContainer.Clear();
    
            var defs = DialogueGraphSaveUtility.Defs;
            if (defs == null)
            {
                _definitionsContainer.Add(new Label("No Definitions asset assigned."));
                return;
            }

            // Conditions section
            var conditionsLabel = new Label("Conditions") { 
                style = { 
                    unityFontStyleAndWeight = FontStyle.Bold, 
                    fontSize = 14, 
                    marginTop = 10,
                    marginBottom = 5
                } 
            };
            _definitionsContainer.Add(conditionsLabel);
    
            for (int i = 0; i < defs.conditions.Count; i++)
            {
                AddConditionDefinitionUI(i, defs.conditions[i]);
            }
    
            var addConditionBtn = new Button(() =>
            {
                defs.conditions.Add(new ConditionDefinition { 
                    id = "NEW_CONDITION", 
                    displayName = "New Condition" 
                });
                RefreshDefinitionsUI();
                EditorUtility.SetDirty(defs);
            }) { text = "Add Condition", style = { marginBottom = 15 } };
            _definitionsContainer.Add(addConditionBtn);

            // Actions section
            var actionsLabel = new Label("Actions") { 
                style = { 
                    unityFontStyleAndWeight = FontStyle.Bold, 
                    fontSize = 14, 
                    marginTop = 10,
                    marginBottom = 5
                } 
            };
            _definitionsContainer.Add(actionsLabel);
    
            for (int i = 0; i < defs.actions.Count; i++)
            {
                AddActionDefinitionUI(i, defs.actions[i]);
            }
    
            var addActionBtn = new Button(() =>
            {
                defs.actions.Add(new ActionDefinition { 
                    id = "NEW_ACTION", 
                    displayName = "New Action" 
                });
                RefreshDefinitionsUI();
                EditorUtility.SetDirty(defs);
            }) { text = "Add Action" };
            _definitionsContainer.Add(addActionBtn);
        }
        
        private void AddConditionDefinitionUI(int index, ConditionDefinition condition)
        {       
            var container = new VisualElement 
        { 
            style = 
            { 
                flexDirection = FlexDirection.Column,
                marginBottom = 10, 
                paddingBottom = 10,
                paddingTop = 10,
                paddingRight = 10,
                paddingLeft = 10,
                backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.2f),
                borderLeftWidth = 3,
                borderLeftColor = Color.cyan
            } 
        };
    
        var header = new VisualElement { style = { flexDirection = FlexDirection.Row } };
    
        var idField = new TextField("ID") { value = condition.id, style = { flexGrow = 1 } };
        idField.RegisterValueChangedCallback(evt => 
        { 
            condition.id = evt.newValue; 
            EditorUtility.SetDirty(DialogueGraphSaveUtility.Defs);
        });
        header.Add(idField);
    
        var nameField = new TextField("Display Name") { value = condition.displayName, style = { flexGrow = 1, marginLeft = 5 } };
        nameField.RegisterValueChangedCallback(evt => 
        { 
            condition.displayName = evt.newValue; 
            EditorUtility.SetDirty(DialogueGraphSaveUtility.Defs);
        });
        header.Add(nameField);
    
        container.Add(header);
    
        // Arguments
        var argsLabel = new Label("Arguments:") { style = { marginTop = 5, unityFontStyleAndWeight = FontStyle.Bold } };
        container.Add(argsLabel);
    
        var argsContainer = new VisualElement();
        container.Add(argsContainer);
    
        for (int i = 0; i < condition.args.Count; i++)
        {
            int argIndex = i; // Capture the index
            AddArgumentDefinitionUI(argsContainer, i, condition.args[i], (() =>
            {
                condition.args.RemoveAt(argIndex);
                RefreshDefinitionsUI();
                EditorUtility.SetDirty(DialogueGraphSaveUtility.Defs);
            }));
        }
    
        var addArgBtn = new Button(() =>
        {
            condition.args.Add(new ArgumentDefinition { name = "new_arg", placeholder = "value" });
            RefreshDefinitionsUI();
            EditorUtility.SetDirty(DialogueGraphSaveUtility.Defs);
        }) { text = "Add Argument" };
        container.Add(addArgBtn);
    
        var removeBtn = new Button(() =>
        {
            DialogueGraphSaveUtility.Defs.conditions.RemoveAt(index);
            RefreshDefinitionsUI();
            EditorUtility.SetDirty(DialogueGraphSaveUtility.Defs);
        }) { text = "Remove Condition", style = { marginTop = 5 } };
        container.Add(removeBtn);
    
        _definitionsContainer.Add(container);
        }
        
        private void AddActionDefinitionUI(int index, ActionDefinition action)
        {
            var container = new VisualElement 
            { 
                style = 
                { 
                    flexDirection = FlexDirection.Column,
                    marginBottom = 10, 
                    paddingTop = 10, 
                    paddingRight = 10, 
                    paddingLeft = 10, 
                    paddingBottom = 10, 
                    backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.2f),
                    borderLeftWidth = 3,
                    borderLeftColor = Color.green
                } 
            };
    
            var header = new VisualElement { style = { flexDirection = FlexDirection.Row } };
    
            var idField = new TextField("ID") { value = action.id, style = { flexGrow = 1 } };
            idField.RegisterValueChangedCallback(evt => 
            { 
                action.id = evt.newValue; 
                EditorUtility.SetDirty(DialogueGraphSaveUtility.Defs);
            });
            header.Add(idField);
    
            var nameField = new TextField("Display Name") { value = action.displayName, style = { flexGrow = 1, marginLeft = 5 } };
            nameField.RegisterValueChangedCallback(evt => 
            { 
                action.displayName = evt.newValue; 
                EditorUtility.SetDirty(DialogueGraphSaveUtility.Defs);
            });
            header.Add(nameField);
    
            container.Add(header);
    
            // Arguments
            var argsLabel = new Label("Arguments:") { style = { marginTop = 5, unityFontStyleAndWeight = FontStyle.Bold } };
            container.Add(argsLabel);
    
            var argsContainer = new VisualElement();
            container.Add(argsContainer);
    
            for (int i = 0; i < action.args.Count; i++)
            {
                int argIndex = i; // Capture the index
                AddArgumentDefinitionUI(argsContainer, i, action.args[i], () =>
                {
                    action.args.RemoveAt(argIndex);
                    RefreshDefinitionsUI();
                    EditorUtility.SetDirty(DialogueGraphSaveUtility.Defs);
                });
            }
    
            var addArgBtn = new Button(() =>
            {
                action.args.Add(new ArgumentDefinition { name = "new_arg", placeholder = "value" });
                RefreshDefinitionsUI();
                EditorUtility.SetDirty(DialogueGraphSaveUtility.Defs);
            }) { text = "Add Argument" };
            container.Add(addArgBtn);
    
            var removeBtn = new Button(() =>
            {
                DialogueGraphSaveUtility.Defs.actions.RemoveAt(index);
                RefreshDefinitionsUI();
                EditorUtility.SetDirty(DialogueGraphSaveUtility.Defs);
            }) { text = "Remove Action", style = { marginTop = 5 } };
            container.Add(removeBtn);
    
            _definitionsContainer.Add(container);
        }
        
        private void AddArgumentDefinitionUI(VisualElement parent, int index, ArgumentDefinition arg, Action onRemove)
        {
            var container = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 5, alignItems = Align.Center } };
    
            var nameField = new TextField() { value = arg.name, style = { flexGrow = 1 } };
            nameField.RegisterValueChangedCallback(evt => 
            { 
                arg.name = evt.newValue; 
                EditorUtility.SetDirty(DialogueGraphSaveUtility.Defs);
            });
            container.Add(nameField);
    
            var placeholderField = new TextField() { value = arg.placeholder, style = { flexGrow = 1, marginLeft = 5 } };
            placeholderField.RegisterValueChangedCallback(evt => 
            { 
                arg.placeholder = evt.newValue; 
                EditorUtility.SetDirty(DialogueGraphSaveUtility.Defs);
            });
            container.Add(placeholderField);
    
            var removeBtn = new Button(() =>
            {
                onRemove?.Invoke(); // Call the removal callback
            }) { text = "X", style = { width = 25, height = 20, marginLeft = 5, unityTextAlign = TextAnchor.MiddleCenter } };
            container.Add(removeBtn);
    
            parent.Add(container);
        }

        private void ContructGraphView()
        {
            _graphView = new DialogueGraphView(this)
            {
                name = "Dialogue Graph"
            };
            _graphView.StretchToParentSize();
            rootVisualElement.Add(_graphView);
        }

        private void ContructToolbar()
        {
            _toolbarContainer = new VisualElement();
            _toolbarContainer.style.flexDirection = FlexDirection.Row;
            _toolbarContainer.style.height = 24;
            rootVisualElement.Add(_toolbarContainer);

            var toolbar = new Toolbar();
            rootVisualElement.Add(toolbar);
            
            // Asset field to load DialogueAsset
            var assetField = new ObjectField("Dialogue Asset")
            {
                objectType = typeof(DialogueAsset),
                allowSceneObjects = false,
                style = { width = 300 }
            };
            assetField.RegisterValueChangedCallback(evt =>
            {
                _loadedAsset = evt.newValue as DialogueAsset;
                if (_loadedAsset != null)
                {
                    _currentAssetPath = AssetDatabase.GetAssetPath(_loadedAsset);
                    _graphView.LoadFromAsset(_loadedAsset);
                }
                else
                {
                    _currentAssetPath = null;
                    _graphView.ClearGraph();
                }
            });
            toolbar.Add(assetField);
            
            // Create new DialogueAsset
            var newButton = new Button(() =>
            {
                string path = EditorUtility.SaveFilePanelInProject("Create Dialogue Asset", "NewDialogue", "asset",
                    "Create Dialogue asset");
                if (string.IsNullOrEmpty(path)) return;
                var newAsset = ScriptableObject.CreateInstance<DialogueAsset>();
                AssetDatabase.CreateAsset(newAsset, path);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = newAsset;
                assetField.value = newAsset;
            }) { text = "Create New" };
            toolbar.Add(newButton);
            
            // Save
            var saveButton = new Button(() =>
            {
                if (_loadedAsset == null) return;
                DialogueGraphSaveUtility.SaveGraphToAsset(_graphView, _loadedAsset);
                EditorUtility.SetDirty(_loadedAsset);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }) { text = "Save" };
            toolbar.Add(saveButton);
            
            // Refresh
            var refreshButton = new Button(() =>
            {
                if (_loadedAsset == null) return;
                _graphView.LoadFromAsset(_loadedAsset);
            }) { text = "Reload" };
            toolbar.Add(refreshButton);
            
            // Zoom to fit
            var fitButton = new Button(() => _graphView.FrameAll()) { text = "Frame All" };
            toolbar.Add(fitButton);

            // Minimap toggle
            var mmButton = new Button(() => _graphView.ToggleMinimap()) { text = "Toggle Minimap" };
            toolbar.Add(mmButton);
            
            // In your ContructToolbar method, in the definitions section:
            var defContainer = new VisualElement();
            defContainer.style.flexDirection = FlexDirection.Row;
            defContainer.style.alignItems = Align.Center;

            var defField = new ObjectField("Defs")
            {
                objectType = typeof(DialogueDefinitions),
                allowSceneObjects = false,
                style = { width = 180 } // Slightly smaller to fit the button
            };
            var defs = DialogueGraphSaveUtility.FindDefinitionsAsset();
            defField.value = defs;
            defField.RegisterValueChangedCallback(evt => DialogueGraphSaveUtility.SetDefinitions(evt.newValue as DialogueDefinitions));
            defContainer.Add(defField);

            // Simple create button - that's Option 4!
            var createDefsBtn = new Button(() =>
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "Create Dialogue Definitions",
                    "DialogueDefinitions",
                    "asset",
                    "Create new Dialogue Definitions");
    
                if (!string.IsNullOrEmpty(path))
                {
                    var newDefs = CreateInstance<DialogueDefinitions>();
                    AssetDatabase.CreateAsset(newDefs, path);
                    AssetDatabase.SaveAssets();
                    defField.value = newDefs;
                    DialogueGraphSaveUtility.SetDefinitions(newDefs);
                    RefreshDefinitionsUI(); // Refresh the definitions panel
                }
            }) { text = "New", style = { width = 50, marginLeft = 5 } };
            defContainer.Add(createDefsBtn);

            toolbar.Add(defContainer);
        }
    }
}
