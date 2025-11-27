// Assets/DialogueSystem/Editor/DialogueGraphSaveUtility.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DialogueSystem;

namespace DialogueSystem.Editor
{
    public static class DialogueGraphSaveUtility
    {
        public static DialogueDefinitions Defs { get; private set; }

        public static void SetDefinitions(DialogueDefinitions d)
        {
            Defs = d;
        }

        public static DialogueDefinitions FindDefinitionsAsset()
        {
            if (Defs != null) return Defs;
            string[] guids = AssetDatabase.FindAssets("t:DialogueDefinitions");
            if (guids == null || guids.Length == 0) return null;
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            Defs = AssetDatabase.LoadAssetAtPath<DialogueDefinitions>(path);
            return Defs;
        }

        public static DialogueLine CreateDialogueLine()
        {
            var dl = new DialogueLine()
            {
                speakerId = "",
                textKey = "",
                choices = new DialogueChoice[0],
                nextNodeId = "",
            };
            dl.guid = Guid.NewGuid().ToString();
            return dl;
        }

        public static void SaveGraphToAsset(DialogueGraphView graphView, DialogueAsset asset)
        {
            var nodeViews = graphView.nodes.ToList().OfType<DialogueNodeView>().ToList();
            var lines = new List<DialogueLine>();

            // Find start node and set entry point
            var startNode = graphView.nodes.ToList().FirstOrDefault(n => n is StartNodeView) as StartNodeView;
            if (startNode != null && startNode.GetOutputPort().connections.Any())
            {
                var firstEdge = startNode.GetOutputPort().connections.First();
                var targetNode = firstEdge.input.node as DialogueNodeView;
                if (targetNode != null)
                {
                    asset.startNodeId = targetNode.NodeData.guid;
                }
            }

            foreach (var nv in nodeViews)
            {
                var data = nv.NodeData;
                var rect = nv.GetPosition();
                data.position = rect.position;
                if (string.IsNullOrEmpty(data.guid)) data.guid = Guid.NewGuid().ToString();
                lines.Add(data);
            }

            asset.nodes = lines;
            EditorUtility.SetDirty(asset);
        }
    }
}
