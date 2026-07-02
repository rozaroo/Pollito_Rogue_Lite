using System;
using System.Collections.Generic;
using Enums;
using UnityEngine;

namespace Serialization
{
    [Serializable]
    public class DebuffRequiredSprite
    {
        [HideInInspector] public string displayName;
        public Sprite sprite;
        [SerializeField] private BodyPart bodyPart;
        [SerializeField] private List<DebuffRequirement> requirements = new();

        // Public accessor for the body part
        public BodyPart BodyPart => bodyPart;

        // This will update the display name based on requirements and body part
        public void UpdateDisplayName()
        {
            string bodyPartName = bodyPart.ToString();

            List<string> activeReqs = new List<string>();
            if (requirements != null)
            {
                foreach (var req in requirements)
                {
                    if (req.required)
                        activeReqs.Add(req.type.ToString());
                }
            }

            // Rename this variable to avoid conflict with the class field
            string requirementsText = activeReqs.Count > 0
                ? string.Join(" + ", activeReqs)
                : "Default";

            displayName = $"{bodyPartName}: {requirementsText}";
        }

        public bool MatchesActiveDebuffs(List<DebuffType> activeDebuffs)
        {
            foreach (var requirement in requirements)
            {
                if (requirement.required && !activeDebuffs.Contains(requirement.type))
                    return false;
            }
            return true;
        }
        
        /// <summary>
        /// Checks if this sprite requires a specific debuff type
        /// </summary>
        /// <param name="debuffType">The debuff type to check for</param>
        /// <returns>True if this sprite requires the specified debuff type</returns>
        public bool RequiresDebuff(DebuffType debuffType)
        {
            if (requirements == null || requirements.Count == 0)
                return false;
        
            foreach (var requirement in requirements)
            {
                if (requirement.required && requirement.type == debuffType)
                    return true;
            }
    
            return false;
        }

        public int RequirementCount()
        {
            int count = 0;
            foreach (var req in requirements)
            {
                if (req.required) count++;
            }
            return count;
        }
    }

    [Serializable]
    public class DebuffRequirement
    {
        public DebuffType type;
        public bool required = true;
    }
}