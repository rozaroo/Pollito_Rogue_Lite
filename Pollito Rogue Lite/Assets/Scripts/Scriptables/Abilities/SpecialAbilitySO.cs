using Interfaces;
using Interfaces.Interfaces;
using UnityEngine;

namespace Scriptables.Abilities
{
    [CreateAssetMenu(fileName = "NewSpecialAbility", menuName = "Game/Abilities/Special Ability")]
    public class SpecialAbilitySO : ScriptableObject, IUpgradable
    {
        [Header("Special Ability Settings")]
        [SerializeField] private bool _unlockedByDefault = false;
        [SerializeField] private string _abilityName;
        [SerializeField] private string _abilityDescription;
        [SerializeField] private Sprite _abilityIcon;

        [Header("Upgrade Tiers")]
        [SerializeField] private int _currentTier = 1;
        [SerializeField] private int _initialPurchaseCost = 100;
        [SerializeField] private SpecialAbilityTier[] _tiers;
        
        [SerializeField] private int _defaultStartTier = 1;
        public int DefaultStartTier => _defaultStartTier;

        // IUpgradable implementation
        public string Id => _abilityName;
        public int CurrentTier {
            get {
                string key = $"SpecialAbilitySO_{_abilityName}_CurrentTier";
                if (PlayerPrefs.HasKey(key))
                {
                    int savedTier = PlayerPrefs.GetInt(key);
                    if (savedTier > 0)
                        return savedTier;
                }
                // If no PlayerPref or value is 0, reset to default if current is higher
                if (_currentTier > _defaultStartTier)
                {
                    _currentTier = _defaultStartTier;
                }
                return _defaultStartTier;
            }
            set {
                _currentTier = value;
                string key = $"SpecialAbilitySO_{_abilityName}_CurrentTier";
                PlayerPrefs.SetInt(key, value);
                PlayerPrefs.Save();
            }
        }
        public string DisplayName => _abilityName;
        public Sprite Icon => _abilityIcon;
        public int InitialPurchaseCost => _initialPurchaseCost;
        public int MaxLevel => _tiers != null ? _tiers.Length : 0;
        public bool UnlockedByDefault => _unlockedByDefault;
        
        // Current strength of the ability based on tier level
        public float CurrentStrengthLevel => GetEffectValueForLevel(CurrentTier);

        public float GetEffectValueForLevel(int level)
        {
            if (_tiers == null || _tiers.Length == 0)
                return 0f;

            int index = Mathf.Clamp(level - 1, 0, _tiers.Length - 1);
            return _tiers[index].effectStrength;
        }

        public int GetUpgradeCostForLevel(int level)
        {
            if (_tiers == null || _tiers.Length == 0 || level <= 0 || level > _tiers.Length)
                return 0;

            return _tiers[level - 1].cost;
        }
    }
}
