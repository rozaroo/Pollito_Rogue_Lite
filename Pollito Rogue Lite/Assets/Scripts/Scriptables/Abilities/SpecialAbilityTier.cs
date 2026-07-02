using System;
using UnityEngine;

namespace Scriptables.Abilities
{
    [Serializable]
    public class SpecialAbilityTier
    {
        [Tooltip("The level of this tier")]
        public int level;
        
        [Tooltip("Cost to purchase this upgrade tier")]
        public int cost;
        
        [Tooltip("Effect strength at this tier")]
        public float effectStrength;
    }
}