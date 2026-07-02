using Enums;
using Scriptables.Abilities;
using UnityEngine;

namespace Scriptables.Upgrades
{
    [CreateAssetMenu(fileName = "NewStoreUpgrade", menuName = "Game/Store/Store Upgrade")]
    public class StoreUpgradeSO : ScriptableObject
    {
        [Header("Basic Info")]
        [SerializeField] private string _upgradeName;
        [SerializeField] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private StoreUpgradeType _upgradeType;

        [Header("Cost and Levels")]
        [SerializeField] private int _maxUpgradeLevel = 3;
        [SerializeField] private int[] _costPerLevel;

        [Header("Effect References")]
        [SerializeField] private BaseEffectSO _baseEffect;
        [SerializeField] private SpecialAbilitySO _specialAbility;

        // For UI display and confirmation
        [SerializeField] private string[] _upgradeDescriptionByLevel;

        public string UpgradeName => _upgradeName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public StoreUpgradeType UpgradeType => _upgradeType;
        public int MaxUpgradeLevel => _maxUpgradeLevel;

        public BaseEffectSO BaseEffect => _baseEffect;
        public SpecialAbilitySO SpecialAbility => _specialAbility;

        public int GetCostForLevel(int level)
        {
            if (_costPerLevel == null || _costPerLevel.Length == 0)
                return 0;

            int index = Mathf.Clamp(level - 1, 0, _costPerLevel.Length - 1);
            return _costPerLevel[index];
        }

        public string GetUpgradeDescriptionForLevel(int level)
        {
            if (_upgradeDescriptionByLevel == null || _upgradeDescriptionByLevel.Length == 0)
                return _description;

            int index = Mathf.Clamp(level - 1, 0, _upgradeDescriptionByLevel.Length - 1);
            return _upgradeDescriptionByLevel[index];
        }

        public bool IsUnlockable()
        {
            // Base effects are always unlocked by default
            if (_upgradeType == StoreUpgradeType.BaseEffect)
                return true;

            // Special abilities might need unlocking first
            return _specialAbility != null && _specialAbility.UnlockedByDefault;
        }

        public bool IsValidForType()
        {
            // Validate that the appropriate reference is set based on type
            return (_upgradeType == StoreUpgradeType.BaseEffect && _baseEffect != null) ||
                   (_upgradeType == StoreUpgradeType.SpecialAbility && _specialAbility != null);
        }
    }
}