using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueSystem.Editor
{
    public class DialogueGraphView : GraphView
    {
        private DialogueGraphEditor _editorWindow;
        private MiniMap _miniMap;

        public DialogueGraphView(DialogueGraphEditor editorWindow)
        {
            _editorWindow = editorWindow;
            
            //styleSheets.Add(Resources.Load<StyleSheet>("DialogueGraphStyle"));
            
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new ContentZoomer());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
            
            AddElement(GenerateEntryPointNode());
            
            this.AddManipulator(new ContextualMenuManipulator(evt => BuildContextualMenu(evt)));
            
            RegisterCallback<DragPerformEvent>(OnDragPerform);
        }
        
        private void OnDragPerform(DragPerformEvent evt)
        {
            // Future support for drag & drop
        }
        
        private void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Create Node", a => CreateNodeAtPosition(evt.localMousePosition));
        }

        public void CreateNodeAtPosition(Vector2 position)
        {
            var nodeData = DialogueGraphSaveUtility.CreateDialogueLine();
            var nodeView = new DialogueNodeView(nodeData);
            nodeView.SetPosition(new Rect(position, new Vector2(200, 150)));
            AddElement(nodeView);
        }

        public void LoadFromAsset(DialogueAsset asset)
        {
            graphElements.ForEach(e => RemoveElement(e));
            AddElement(GenerateEntryPointNode());
            
            // Create node views for each DialogueLine
            foreach (var node in asset.nodes)
            {
                var nv = new DialogueNodeView(node);
                nv.SetPosition(new Rect(node.position, new Vector2(200, 150)));
                AddElement(nv);
            }
            
            // Recreate connections based on nextNodeId and choices
            var nodeViews = this.nodes;
            foreach (var nv in nodeViews)
            {
                var view = nv as DialogueNodeView;
                if (view == null) continue;
                
                // Next node edge
                if (!string.IsNullOrEmpty(view.NodeData.nextNodeId))
                {
                    var targetNode = FindNodeViewByGuid(view.NodeData.nextNodeId);
                    if (targetNode != null)
                    {
                        CreateEdge(view.GetNextPort(), targetNode.GetInputPort());
                    }
                    
                    // Choices edges
                    for (int i = 0; i < view.NodeData.choices?.Length; i++)
                    {
                        var choiceTargetGuid = view.NodeData.choices[i].nextNodeId;
                        if (!string.IsNullOrEmpty(choiceTargetGuid))
                        {
                            var target = FindNodeViewByGuid(choiceTargetGuid);
                            if (target != null)
                            {
                                var choicePort = view.GetChoicePort(i);
                                if (choicePort != null)
                                {
                                    CreateEdge(choicePort, target.GetInputPort());
                                }
                            }
                        }
                    }
                }
            }
        }

        private DialogueNodeView FindNodeViewByGuid(string guid)
        {
            foreach (var n in nodes)
            {
                var dv = n as DialogueNodeView;
                if (dv == null) continue;
                if (dv.NodeData.guid == guid) return dv;
            }

            return null;
        }

        public void ClearGraph()
        {
            graphElements.ForEach(e => RemoveElement(e));
            AddElement(GenerateEntryPointNode());
        }

        private void CreateEdge(Port outPort, Port inPort)
        {
            var edge = outPort.ConnectTo(inPort);
            AddElement(edge);
            edge.MarkDirtyRepaint();
        }

        private Node GenerateEntryPointNode()
        {
            var entry = new Node { title = "START" };
            entry.SetPosition(new Rect(10, 10, 150, 50));
            entry.capabilities &= ~Capabilities.Movable;
            var outPort = entry.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single,
                typeof(bool));
            outPort.portName = "Next";
            entry.outputContainer.Add(outPort);
            entry.RefreshExpandedState();
            entry.RefreshPorts();
            return entry;
        }

        public void ToggleMinimap()
        {
            if (_miniMap == null)
            {
                _miniMap = new MiniMap { anchored = true };
                _miniMap.SetPosition(new Rect(10, 30, 200, 140));
                Add(_miniMap);
            }
            else
            {
                Remove(_miniMap);
                _miniMap = null;
            }
        }
    }
}
