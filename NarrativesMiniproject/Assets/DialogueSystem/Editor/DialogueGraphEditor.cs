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
        private DialogueAsset _loadedAsset;
        private VisualElement _toolbarContainer;

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
        }

        private void OnDisable()
        {
            rootVisualElement.Remove(_graphView);
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
            
            // Definitions picker
            var defField = new ObjectField("Defs")
            {
                objectType = typeof(DialogueDefinitions),
                allowSceneObjects = false,
                style = { width = 200 }
            };
            var defs = DialogueGraphSaveUtility.FindDefinitionsAsset();
            defField.value = defs;
            defField.RegisterValueChangedCallback(evt => DialogueGraphSaveUtility.SetDefinitions(evt.newValue as DialogueDefinitions));
            toolbar.Add(defField);
        }
    }
}
