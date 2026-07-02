using System.Collections.Generic;
using Enums;
using Serialization;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(fileName = "New Buff", menuName = "Effects/Buffs/Generic Buff")]
    public class BuffSO : BaseEffectSO
    {
        [SerializeField] private BuffType _type;
        [SerializeField] private Color _color;

        [Header("Default Body Part Sprites")]
        [SerializeField] private Sprite _defaultHeadSprite;
        [SerializeField] private Sprite _defaultBodySprite;
        [SerializeField] private Sprite _defaultBackFeetSprite;
        [SerializeField] private Sprite _defaultFrontFeetSprite;

        [Header("Special Condition Sprites")]
        [SerializeField] private List<DebuffRequiredSprite> _specialSprites = new();

        public override EffectType EffectType => EffectType.Buff;
        public BuffType Type => _type;
        public Color Color => _color;

        // Default sprite accessors
        public Sprite DefaultHeadSprite => _defaultHeadSprite;
        public Sprite DefaultBodySprite => _defaultBodySprite;
        public Sprite DefaultBackFeetSprite => _defaultBackFeetSprite;
        public Sprite DefaultFrontFeetSprite => _defaultFrontFeetSprite;

        /// <summary>
        /// Gets the appropriate sprite based on body part and active debuffs
        /// </summary>
        public Sprite GetSpriteForBodyPart(BodyPart part, List<DebuffType> activeDebuffs)
        {
            // Determine if we have any sprite for this part
            Sprite defaultSprite = part switch
            {
                BodyPart.Head => _defaultHeadSprite,
                BodyPart.Body => _defaultBodySprite,
                BodyPart.BackFeet => _defaultBackFeetSprite,
                BodyPart.FrontFeet => _defaultFrontFeetSprite,
                _ => null
            };

            // If we don't have a default sprite, this buff doesn't affect that part
            if (defaultSprite == null)
                return null;

            // Look for special sprites that match the current debuff conditions AND body part
            DebuffRequiredSprite bestMatch = null;
            int highestRequirementCount = -1;

            foreach (var specialSprite in _specialSprites)
            {
                if (specialSprite.sprite != null && 
                    specialSprite.BodyPart == part && 
                    specialSprite.MatchesActiveDebuffs(activeDebuffs))
                {
                    int reqCount = specialSprite.RequirementCount();
                    if (reqCount > highestRequirementCount)
                    {
                        bestMatch = specialSprite;
                        highestRequirementCount = reqCount;
                    }
                }
            }

            // Return special sprite if found, otherwise the default
            return bestMatch?.sprite ?? defaultSprite;
        }
        
        /// <summary>
        /// Gets a special sprite that requires the specific debuff for a given body part
        /// </summary>
        /// <param name="requiredDebuff">The specific debuff type to look for</param>
        /// <param name="bodyPart">The body part to get the sprite for</param>
        /// <returns>The sprite that requires the specific debuff, or null if not found</returns>
        public Sprite GetSpecialSpriteForDebuff(DebuffType requiredDebuff, BodyPart bodyPart)
        {
            foreach (var specialSprite in _specialSprites)
            {
                if (specialSprite.sprite != null && 
                    specialSprite.BodyPart == bodyPart && 
                    specialSprite.RequiresDebuff(requiredDebuff))
                {
                    return specialSprite.sprite;
                }
            }
    
            return null;
        }

        /// <summary>
        /// Gets all special sprites that require a specific debuff type
        /// </summary>
        /// <param name="requiredDebuff">The specific debuff type to look for</param>
        /// <returns>List of all sprites that require the specific debuff</returns>
        public List<DebuffRequiredSprite> GetAllSpecialSpritesForDebuff(DebuffType requiredDebuff)
        {
            List<DebuffRequiredSprite> matchingSprites = new();
    
            foreach (var specialSprite in _specialSprites)
            {
                if (specialSprite.sprite != null && specialSprite.RequiresDebuff(requiredDebuff))
                {
                    matchingSprites.Add(specialSprite);
                }
            }
    
            return matchingSprites;
        }

        /// <summary>
        /// Provides access to all special sprites in this buff
        /// </summary>
        public List<DebuffRequiredSprite> GetAllSpecialSprites()
        {
            return new List<DebuffRequiredSprite>(_specialSprites);
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            // Update the display names for all sprites
            foreach (var sprite in _specialSprites)
            {
                sprite.UpdateDisplayName();
            }
        }
        #endif
    }
}