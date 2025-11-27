using System;
using System.Collections.Generic;
using System.Linq;
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

            // Manually add the edge connector to the graph view
            this.AddManipulator(_edgeConnector);

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
            graphViewChanged = OnGraphViewChanged;

            Debug.Log("DialogueGraphView initialized with edge connector");
        }
        
        public void RegisterPortToEdgeConnector(Port port)
        {
            if (port == null) return;

            // Create a new edge connector for this specific port
            var edgeConnectorListener = new DialogueEdgeConnectorListener(this);
            var edgeConnector = new EdgeConnector<Edge>(edgeConnectorListener);
            port.AddManipulator(edgeConnector);

            Debug.Log($"Registered edge connector for port: {port.portName}");
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
            var nodeView = new DialogueNodeView(nodeData, this);
            nodeView.SetPosition(new Rect(position, new Vector2(200, 150)));
            AddElement(nodeView);
        }

        public void LoadFromAsset(DialogueAsset asset)
        {
            graphElements.ForEach(RemoveElement);
            var startNode = GenerateEntryPointNode();
            AddElement(startNode);
    
            // Create node views for each DialogueLine
            if (asset.nodes != null)
            {
                foreach (var node in asset.nodes)
                {
                    var nv = new DialogueNodeView(node, this);
                    nv.SetPosition(new Rect(node.position, new Vector2(200, 150)));
                    AddElement(nv);
                }
            }
    
            // Force a refresh of all nodes to ensure ports are created
            RefreshAllNodes();
    
            // Now reconnect edges
            ReconnectEdgesSimple(asset, startNode);
        }

        private void ReconnectEdgesSimple(DialogueAsset asset, StartNodeView startNode)
        {
            if (asset == null) return;
            
            var nodeViews = nodes.ToList().Where(n => n is DialogueNodeView).Cast<DialogueNodeView>().ToList();
            
            // Reconnect start node first
            if (!string.IsNullOrEmpty(asset.startNodeId))
            {
                var startTarget = FindNodeViewByGuid(asset.startNodeId);
                if (startTarget != null && startNode != null)
                {
                    CreateEdge(startNode.GetOutputPort(), startTarget.GetInputPort());
                    startNode.StartNodeId = asset.startNodeId; // Update the StartNodeView's internal state
                }
            }
    
            foreach (var nv in nodeViews)
            {
                // Next node edge
                if (!string.IsNullOrEmpty(nv.NodeData.nextNodeId))
                {
                    var targetNode = FindNodeViewByGuid(nv.NodeData.nextNodeId);
                    if (targetNode != null && nv.GetNextPort() != null)
                    {
                        CreateEdge(nv.GetNextPort(), targetNode.GetInputPort());
                    }
                }
        
                // Choices edges
                if (nv.NodeData.choices == null) continue;
                for (int i = 0; i < nv.NodeData.choices.Length; i++)
                {
                    var choice = nv.NodeData.choices[i];
                    if (string.IsNullOrEmpty(choice.nextNodeId)) continue;
                    var target = FindNodeViewByGuid(choice.nextNodeId);
                    if (target == null) continue;
                    var choicePort = nv.GetChoicePort(i);
                    if (choicePort != null)
                    {
                        CreateEdge(choicePort, target.GetInputPort());
                    }
                }
            }
        }

        public void RefreshAllNodes()
        {
            foreach (var node in nodes.ToList())
            {
                if (node is DialogueNodeView nodeView)
                {
                    // This will force the node to rebuild its ports
                    nodeView.MarkDirtyRepaint();
                    nodeView.RefreshPorts();
                }
            }
        }

        private void ConnectChoiceEdge(DialogueNodeView sourceNode, int choiceIndex, DialogueNodeView targetNode)
        {
            var choicePort = sourceNode.GetChoicePort(choiceIndex);
            if (choicePort != null && targetNode.GetInputPort() != null)
            {
                CreateEdge(choicePort, targetNode.GetInputPort());
            }
            else
            {
                // If ports aren't ready yet, try one more time
                EditorApplication.delayCall += () => ConnectChoiceEdge(sourceNode, choiceIndex, targetNode);
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

        private StartNodeView GenerateEntryPointNode()
        {
            var entry = new StartNodeView(this);
            entry.SetPosition(new Rect(10, 10, 150, 50));
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
        
        public void CreateNodeFromEdgeDrop(Edge edge, Vector2 screenPosition)
        {
            Debug.Log($"CreateNodeFromEdgeDrop called at screen position: {screenPosition}");
    
            // Convert screen position to graph coordinates
            var graphPosition = this.contentViewContainer.WorldToLocal(screenPosition);
            Debug.Log($"Converted to graph position: {graphPosition}");
    
            // Create new node
            var nodeData = DialogueGraphSaveUtility.CreateDialogueLine();
            var nodeView = new DialogueNodeView(nodeData, this);
            nodeView.SetPosition(new Rect(graphPosition, new Vector2(200, 150)));
            AddElement(nodeView);
    
            Debug.Log($"Created new node with GUID: {nodeData.guid}");
    
            // Connect the edge to the new node
            var newInputPort = nodeView.GetInputPort();
            if (newInputPort != null && edge.output != null)
            {
                Debug.Log("Connecting edge to new node...");
        
                // Create the edge properly
                var newEdge = edge.output.ConnectTo(newInputPort);
                AddElement(newEdge);
        
                // Update node data connections
                UpdateNodeConnections(newEdge);
        
                // Remove the temporary edge
                RemoveElement(edge);
                Debug.Log("Edge connected successfully!");
            }
            else
            {
                Debug.LogError("Failed to connect edge - null ports detected");
                Debug.Log($"newInputPort: {newInputPort}, edge.output: {edge.output}");
            }
        }

        public void UpdateNodeConnections(Edge edge)
        {
            var outputNode = edge.output.node as DialogueNodeView;
            var startNode = edge.output.node as StartNodeView; 
            var inputNode = edge.input.node as DialogueNodeView;
    
            if (outputNode != null && inputNode != null)
            {
                if (edge.output == outputNode.GetNextPort())
                {
                    outputNode.NodeData.nextNodeId = inputNode.NodeData.guid;
                }
                else
                {
                    int choiceIndex = outputNode.GetChoicePortIndex(edge.output);
                    if (choiceIndex >= 0)
                    {
                        outputNode.NodeData.choices[choiceIndex].nextNodeId = inputNode.NodeData.guid;
                    }
                }
            }
            else if (startNode != null && inputNode != null)
            {
                startNode.StartNodeId = inputNode.NodeData.guid;
                if (_editorWindow.CurrentAsset != null)
                {
                    _editorWindow.CurrentAsset.startNodeId = inputNode.NodeData.guid;
                }
            }
    
            // Mark asset as dirty
            if (_editorWindow.CurrentAsset != null)
            {
                EditorUtility.SetDirty(_editorWindow.CurrentAsset);
            }
}
    }
    public class StartNodeView : Node
    {
        public string StartNodeId { get; set; }
        private Port _outputPort;
        private DialogueGraphView _graphView;

        public StartNodeView(DialogueGraphView graphView = null)
        {
            _graphView = graphView;
        
            title = "START";
            viewDataKey = "START_NODE";
            StartNodeId = "";
    
            capabilities &= ~Capabilities.Movable;
            capabilities &= ~Capabilities.Deletable;
    
            _outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            _outputPort.portName = "Next";
            outputContainer.Add(_outputPort);
        
            // Register with edge connector
            if (_graphView != null)
                _graphView.RegisterPortToEdgeConnector(_outputPort);
    
            RefreshExpandedState();
            RefreshPorts();
        }

        public Port GetOutputPort() => _outputPort;
    }
}
