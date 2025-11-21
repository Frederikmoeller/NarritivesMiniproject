using UnityEngine;
using System;
using System.Collections.Generic;

namespace DialogueSystem
{
[Serializable]
public class DialogueLine
{
    public string guid;           // unique ID for editor/serialization use
    public Vector2 position;      // editor position (not used at runtime except for saving)
    public string speakerId;
    public string textKey;
    public DialogueChoice[] choices;
    public string nextNodeId;
}

[Serializable]
    public class DialogueChoice
    {
        public string textKey;
        public string nextNodeId;
        public Condition[] Conditions;
        public ActionEvent[] actions;
    }

    [Serializable]
    public class Condition
    {
        public string id;
        public string[] args;
    }

    [Serializable]
    public class ActionEvent
    {
        public string id;
        public string[] args;
    }

    [CreateAssetMenu(fileName="DialogueAsset")]
    public class DialogueAsset : ScriptableObject {
        public List<DialogueLine> nodes;
        public string startNodeId;
    }
    
}
