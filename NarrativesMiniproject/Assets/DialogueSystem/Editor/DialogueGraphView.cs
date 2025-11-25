using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueSystem.Editor
{
    public class DialogueGraphView : GraphView
    {
        private DialogueGraphEditor _editorWindow;
        private MiniMap _miniMap;
        private EdgeConnector<Edge> _edgeConnector;

        public DialogueGraphView(DialogueGraphEditor editorWindow)
            {
                _editorWindow = editorWindow;

                SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
                this.AddManipulator(new ContentDragger());
                this.AddManipulator(new SelectionDragger());
                this.AddManipulator(new RectangleSelector());

                // EdgeConnector for drag & connect
                _edgeConnector = new EdgeConnector<Edge>(new DialogueEdgeConnectorListener(this));
                this.AddManipulator(_edgeConnector);

                var grid = new GridBackground();
                Insert(0, grid);
                grid.StretchToParentSize();
                graphViewChanged = OnGraphViewChanged;
            }
        
        // <<< Critical for making ports connectable >>>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();

            foreach (var port in ports)
            {
                if (port == startPort) continue;                     // cannot connect to itself
                if (port.direction == startPort.direction) continue; // must be opposite direction
                compatiblePorts.Add(port);
            }

            return compatiblePorts;
        }
        
        private void OnDragPerform(DragPerformEvent evt)
        {
            // Future support for drag & drop
        }
        
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
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
            graphElements.ForEach(RemoveElement);
            AddElement(GenerateEntryPointNode());
            
            // Create node views for each DialogueLine
            if (asset.nodes != null)
            {
                foreach (var node in asset.nodes)
                {
                    var nv = new DialogueNodeView(node);
                    nv.SetPosition(new Rect(node.position, new Vector2(200, 150)));
                    AddElement(nv);
                }
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
            graphElements.ForEach(RemoveElement);
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
        
        public GraphViewChange OnGraphViewChanged(GraphViewChange change) 
        {
            // Handle newly created edges
            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate)
                {
                    var outNode = edge.output.node as DialogueNodeView;
                    var inNode = edge.input.node as DialogueNodeView;

                    if (outNode == null || inNode == null)
                        continue;

                    // Determine if it's the main Next port
                    if (edge.output == outNode.GetNextPort())
                    {
                        outNode.NodeData.nextNodeId = inNode.NodeData.guid;
                        Debug.Log($"Set nextNodeId for {outNode.NodeData.guid} -> {inNode.NodeData.guid}");
                    }
                    else
                    {
                        // Otherwise, it's a choice port
                        int choiceIndex = outNode.GetChoicePortIndex(edge.output);
                        if (choiceIndex >= 0)
                        {
                            outNode.NodeData.choices[choiceIndex].nextNodeId = inNode.NodeData.guid;
                            Debug.Log($"Set choice {choiceIndex} nextNodeId for {outNode.NodeData.guid} -> {inNode.NodeData.guid}");
                        }
                    }

                    // Optional: store edge userData for debugging
                    edge.userData = new { from = outNode.NodeData.guid, to = inNode.NodeData.guid };
                }
            }

            // Handle edge removal similarly
            if (change.elementsToRemove != null)
            {
                foreach (var element in change.elementsToRemove)
                {
                    if (element is Edge edge)
                    {
                        var outNode = edge.output.node as DialogueNodeView;
                        if (outNode == null) continue;

                        if (edge.output == outNode.GetNextPort())
                            outNode.NodeData.nextNodeId = null;
                        else
                        {
                            int idx = outNode.GetChoicePortIndex(edge.output);
                            if (idx >= 0)
                                outNode.NodeData.choices[idx].nextNodeId = null;
                        }
                    }
                }
            }
            EditorUtility.SetDirty(_editorWindow.CurrentAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return change;
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
