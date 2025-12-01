using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem.Data
{
    [CreateAssetMenu(fileName = "DialogueAsset", menuName = "Scriptable Objects/DialogueAsset")]
    public class DialogueAsset : ScriptableObject
    {
        public List<DialogueLine> nodes;
        public string startNodeId;
    }
}

