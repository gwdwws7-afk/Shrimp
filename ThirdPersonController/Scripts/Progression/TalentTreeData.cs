using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public enum TalentBranch
    {
        Offense,
        Control,
        Survival
    }

    [Serializable]
    public class TalentNodeData
    {
        public string id;
        public string title;
        public TalentBranch branch;
        public int cost = 1;
        public List<string> prerequisites = new List<string>();
        public List<StatModifier> modifiers = new List<StatModifier>();
    }

    [CreateAssetMenu(fileName = "TalentTree_", menuName = "Progression/Talent Tree")]
    public class TalentTreeData : ScriptableObject
    {
        public List<TalentNodeData> nodes = new List<TalentNodeData>();
    }
}
