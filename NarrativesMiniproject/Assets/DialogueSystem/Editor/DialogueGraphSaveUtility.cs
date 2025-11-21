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
            // iterate nodes in graphView and write to asset.nodes
            var nodeViews = graphView.nodes.ToList().Where(n => n is DialogueNodeView).Select(n => n as DialogueNodeView).ToList();
            var lines = new List<DialogueLine>();

            foreach (var nv in nodeViews)
            {
                var data = nv.NodeData;
                // Save position
                var rect = nv.GetPosition();
                data.position = rect.position;
                // Ensure guid exists
                if (string.IsNullOrEmpty(data.guid)) data.guid = Guid.NewGuid().ToString();
                lines.Add(data);
            }

            asset.nodes = lines;
            EditorUtility.SetDirty(asset);
        }
    }
}
