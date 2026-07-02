using System.Collections.Generic;
using Scriptables;
using UnityEngine;

namespace Player
{
    [System.Serializable]
    public class ChildOption
    {
        [SerializeField] public List<BuffSO> Buffs { get; private set; }
        [SerializeField] public List<DebuffSO> Debuffs { get; private set; }
        public string Description;

        public ChildOption(List<BuffSO> buffs, List<DebuffSO> debuffs)
        {
            Buffs = buffs ?? new List<BuffSO>();
            Debuffs = debuffs ?? new List<DebuffSO>();
        }
    }
}