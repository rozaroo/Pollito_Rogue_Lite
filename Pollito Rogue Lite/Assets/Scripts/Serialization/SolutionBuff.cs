using System;
using System.Collections.Generic;
using System.Linq;
using Enums;
using UnityEngine;

namespace Serialization
{
    [Serializable]
    public class SolutionBuffs
    {
        [HideInInspector] public string _displayName;
        [SerializeField] private List<BuffRequirement> _requiredBuffs = new();

        public List<BuffType> RequiredBuffs 
        { 
            get
            {
                List<BuffType> buffs = new List<BuffType>();
                foreach (var req in _requiredBuffs)
                {
                    if (req.required)
                        buffs.Add(req.type);
                }
                return buffs;
            }
        }

        // This will update the display name based on requirements
        public void UpdateDisplayName()
        {
            var activeBuffs = new List<string>();
            if (_requiredBuffs != null)
            {
                activeBuffs.AddRange(from req in _requiredBuffs where req.required select req.type.ToString());
            }

            var requirementsText = activeBuffs.Count > 0
                ? string.Join(" + ", activeBuffs)
                : "None";

            _displayName = $"Solution: {requirementsText}";
        }

        /// <summary>
        /// Check if this solution can be solved with the provided buffs
        /// </summary>
        public bool CanSolveWith(List<BuffType> availableBuffs)
        {
            return _requiredBuffs.All(req => !req.required || availableBuffs.Contains(req.type));
        }

        /// <summary>
        /// Check if this solution requires a specific buff
        /// </summary>
        public bool RequiresBuff(BuffType buffType)
        {
            if (_requiredBuffs == null || _requiredBuffs.Count == 0) return false;
            return _requiredBuffs.Any(req => req.required && req.type == buffType);
        }

        public void OnValidate()
        {
            UpdateDisplayName();
        }
        
        public int RequirementCount()
        {
            return _requiredBuffs.Count(req => req.required);
        }
    }

    [Serializable]
    public class BuffRequirement
    {
        public BuffType type;
        public bool required = true;
    }
}