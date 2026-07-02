using Effects;
using Enums;
using System.Collections.Generic;
using UnityEngine;

namespace Player
{
    public class PlayerEffectManager : MonoBehaviour
    {
        private Dictionary<BuffType, BaseEffect> activeEffects = new();

        /// <summary>
        /// Registers the effect with the manager, but does NOT activate it.
        /// </summary>
        public void RegisterEffect(BaseEffect effect)
        {
            if (effect == null || activeEffects.ContainsKey(effect.Type))
                return;

            activeEffects.Add(effect.Type, effect);
            effect.OnEffectRegistered();
        }

        /// <summary>
        /// Fully adds and activates an effect.
        /// </summary>
        public void AddEffect(BaseEffect effect)
        {
            if (effect == null || activeEffects.ContainsKey(effect.Type))
                return;

            RegisterEffect(effect);
            effect.ActivateEffect();
        }

        /// <summary>
        /// Removes and deactivates an effect.
        /// </summary>
        public void RemoveEffect(BuffType type)
        {
            if (!activeEffects.TryGetValue(type, out var effect)) return;

            effect.DeactivateEffect();
            effect.OnEffectUnregistered();
            activeEffects.Remove(type);
        }

        /// <summary>
        /// Checks if the player currently has a specific effect.
        /// </summary>
        public bool HasEffect(BuffType type)
        {
            return activeEffects.ContainsKey(type);
        }

        /// <summary>
        /// Get the current effect instance (if active).
        /// </summary>
        public BaseEffect GetEffect(BuffType type)
        {
            activeEffects.TryGetValue(type, out var effect);
            return effect;
        }

        /// <summary>
        /// Removes all effects from the player.
        /// </summary>
        public void ClearAllEffects()
        {
            foreach (var effect in activeEffects.Values)
            {
                effect.DeactivateEffect();
                effect.OnEffectUnregistered();
            }

            activeEffects.Clear();
        }
    }

}