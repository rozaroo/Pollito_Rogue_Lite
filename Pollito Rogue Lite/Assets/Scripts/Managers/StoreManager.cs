using System;
using System.Collections.Generic;
using System.Linq;
using Scriptables.Abilities;
using UnityEngine;
using Hub;
using Interfaces.Interfaces;

namespace Managers
{
    public class StoreManager : MonoBehaviour
    {
        [SerializeField] private MutationManager _mutationManager;

        // Track purchased abilities and their upgrade levels
        private Dictionary<string, int> _purchasedAbilities = new();
        
        // List to track all HubUpgradeButtons in the scene - exposed as serialized field
        [SerializeField] private List<HubUpgradeButton> _upgradeButtons = new();

        public event Action OnAbilityPurchased;
        public event Action OnAbilityUpgraded;

        private void Awake()
        {
            // Load saved purchase data if available
            LoadPurchaseData();
        }
        
        // Modify the Start method to call ResetDefaultAbilityTiers after getting MutationManager
        private void Start()
        {
            // Get MutationManager from GameManager instead of using serialized field
            _mutationManager = GameManager.Instance.MutationManager;
    
            // Reset default ability tiers now that we have the _mutationManager
            ResetDefaultAbilityTiers();

            // Find all existing hub upgrade buttons in the scene if none have been registered
            if (_upgradeButtons.Count == 0)
            {
                FindAllHubUpgradeButtons();
            }
    
            // Update all buttons to reflect proper tier levels
            UpdateAllButtons();
        }
        
        // Find all existing HubUpgradeButtons in the scene
        private void FindAllHubUpgradeButtons()
        {
            HubUpgradeButton[] buttons = FindObjectsOfType<HubUpgradeButton>();
            foreach (var button in buttons)
            {
                RegisterButton(button);
            }
            
            Debug.Log($"[StoreManager] Found and registered {_upgradeButtons.Count} upgrade buttons in the scene");
        }
        
        // Method for HubUpgradeButtons to register themselves with the store manager
        public void RegisterButton(HubUpgradeButton button)
        {
            if (!_upgradeButtons.Contains(button))
            {
                _upgradeButtons.Add(button);
                Debug.Log($"[StoreManager] Registered upgrade button: {button.gameObject.name}");
            }
        }
        
        // Method to unregister a button (e.g., when it's destroyed)
        public void UnregisterButton(HubUpgradeButton button)
        {
            if (_upgradeButtons.Contains(button))
            {
                _upgradeButtons.Remove(button);
                Debug.Log($"[StoreManager] Unregistered upgrade button: {button.gameObject.name}");
            }
        }
        
        // Update all registered buttons
        public void UpdateAllButtons()
        {
            foreach (var button in _upgradeButtons)
            {
                if (button != null)
                    button.UpdateUI();
            }
            
            Debug.Log($"[StoreManager] Updated {_upgradeButtons.Count} upgrade buttons");
        }
        
        // Handle the purchase/upgrade for a specific upgradable
        public bool HandleUpgrade(IUpgradable upgradable)
        {
            try
            {
                if (upgradable == null)
                {
                    Debug.LogError("[StoreManager] HandleUpgrade called with null upgradable");
                    return false;
                }
                
                string id = upgradable.Id;
                Debug.Log($"[StoreManager] Handling upgrade for ability ID: {id}");
                
                // Verify we can get the SpecialAbilitySO directly from the passed upgradable
                SpecialAbilitySO directScriptable = upgradable as SpecialAbilitySO;
                if (directScriptable != null)
                {
                    Debug.Log($"[StoreManager] Direct cast to SpecialAbilitySO successful. Name: {directScriptable.name}, CurrentTier: {directScriptable.CurrentTier}");
                }
                else
                {
                    Debug.LogWarning($"[StoreManager] Direct cast to SpecialAbilitySO failed. Will try to find via button.");
                }
                
                // Find the button with this upgradable
                HubUpgradeButton targetButton = null;
                int buttonIndex = 0;
                Debug.Log($"[StoreManager] Searching through {_upgradeButtons.Count} buttons for ID: {id}");
                
                foreach (var button in _upgradeButtons)
                {
                    if (button == null)
                    {
                        Debug.LogError($"[StoreManager] Button at index {buttonIndex} is null");
                        buttonIndex++;
                        continue;
                    }
                    
                    Debug.Log($"[StoreManager] Checking button {buttonIndex}: {button.gameObject.name}");
                    
                    if (button.Upgradable == null)
                    {
                        Debug.LogError($"[StoreManager] Button {button.gameObject.name} has null Upgradable");
                        buttonIndex++;
                        continue;
                    }
                    
                    Debug.Log($"[StoreManager] Button {buttonIndex} Upgradable ID: {button.Upgradable.Id}");
                    
                    if (button.Upgradable.Id == id)
                    {
                        targetButton = button;
                        Debug.Log($"[StoreManager] Found matching button: {button.gameObject.name}");
                        break;
                    }
                    buttonIndex++;
                }
                
                // Use the direct scriptable if button lookup failed
                SpecialAbilitySO abilityScriptable = null;
                
                if (targetButton == null)
                {
                    Debug.LogWarning($"[StoreManager] No button found with upgradable ID: {id}. Using direct scriptable.");
                    abilityScriptable = directScriptable;
                }
                else
                {
                    // Get the scriptable ability from the button
                    abilityScriptable = targetButton.Upgradable as SpecialAbilitySO;
                    Debug.Log($"[StoreManager] Got scriptable from button: {(abilityScriptable != null ? abilityScriptable.name : "null")}");
                }
                
                if (abilityScriptable == null)
                {
                    Debug.LogError($"[StoreManager] Upgradable is not a SpecialAbilitySO: {id}");
                    return false;
                }
                
                int currentLevel = GetAbilityLevel(id);
                Debug.Log($"[StoreManager] Current level for {id}: {currentLevel}, Max level: {upgradable.MaxLevel}");
                
                bool success = false;
                if (currentLevel == 0)
                {
                    // Initial purchase
                    Debug.Log($"[StoreManager] Attempting initial purchase for {id}");
                    success = PurchaseAbility(id);
                    Debug.Log($"[StoreManager] Initial purchase result: {success}");
                    
                    if (success)
                    {
                        // Update the scriptable directly
                        Debug.Log($"[StoreManager] Setting CurrentTier from {abilityScriptable.CurrentTier} to 1");
                        abilityScriptable.CurrentTier = 1;
                        Debug.Log($"[StoreManager] Initial purchase successful. Set {id} to tier 1. New CurrentTier: {abilityScriptable.CurrentTier}");
                    }
                }
                else if (currentLevel < upgradable.MaxLevel)
                {
                    // Upgrade
                    Debug.Log($"[StoreManager] Attempting upgrade for {id} from level {currentLevel} to {currentLevel + 1}");
                    success = UpgradeAbility(id);
                    Debug.Log($"[StoreManager] Upgrade result: {success}");
                    
                    if (success)
                    {
                        // Update the scriptable directly
                        Debug.Log($"[StoreManager] Setting CurrentTier from {abilityScriptable.CurrentTier} to {currentLevel + 1}");
                        abilityScriptable.CurrentTier = currentLevel + 1;
                        Debug.Log($"[StoreManager] Upgrade successful. Set {id} to tier {currentLevel + 1}. New CurrentTier: {abilityScriptable.CurrentTier}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[StoreManager] Ability {id} already at max level: {currentLevel}");
                }
                
                return success;
            }
            catch (System.Exception e)
            {
                // Catch any exceptions to help diagnose issues
                Debug.LogError($"[StoreManager] Exception in HandleUpgrade: {e.Message}");
                Debug.LogError($"[StoreManager] Stack trace: {e.StackTrace}");
                return false;
            }
        }

        public List<SpecialAbilitySO> GetPurchasableAbilities()
        {
            if (_mutationManager == null) return new List<SpecialAbilitySO>();

            // Get all special abilities from mutation manager that aren't unlocked by default
            // and haven't been purchased yet
            return _mutationManager.AvailableSpecialAbilities
                .Where(a => !a.UnlockedByDefault && !_purchasedAbilities.ContainsKey(a.name))
                .ToList();
        }

        public List<SpecialAbilitySO> GetPurchasedAbilities()
        {
            if (_mutationManager == null) return new List<SpecialAbilitySO>();

            // Get all abilities that have been purchased or are unlocked by default
            List<SpecialAbilitySO> purchased = new List<SpecialAbilitySO>();

            // Add default unlocked abilities
            purchased.AddRange(_mutationManager.GetDefaultUnlockedSpecialAbilities());

            // Add abilities the player has purchased
            foreach (var abilityName in _purchasedAbilities.Keys)
            {
                var ability = _mutationManager.GetSpecialAbilityByName(abilityName);
                if (ability != null && !purchased.Contains(ability))
                {
                    purchased.Add(ability);
                }
            }

            return purchased;
        }
        public int GetAbilityLevel(string abilityName)
        {
            if (_purchasedAbilities.TryGetValue(abilityName, out int level))
            {
                return level;
            }

            // Check if it's a default unlocked ability
            var ability = _mutationManager.GetSpecialAbilityByName(abilityName);
            if (ability != null && ability.UnlockedByDefault)
            {
                return 1; // Default abilities start at level 1
            }

            return 0; // Not purchased
        }

        public bool CanPurchaseAbility(string abilityName)
        {
            var ability = _mutationManager.GetSpecialAbilityByName(abilityName);
            if (ability == null) return false;

            // If already own it, can't purchase again
            if (_purchasedAbilities.ContainsKey(abilityName)) return false;

            // If it's unlocked by default, can't purchase
            if (ability.UnlockedByDefault) return false;

            // Check if player has enough money
            int playerMoney = GameManager.Instance.CurrencyManager.GetCurrentCurrency();
            return playerMoney >= ability.InitialPurchaseCost;
        }

        public bool CanUpgradeAbility(string abilityName)
        {
            var ability = _mutationManager.GetSpecialAbilityByName(abilityName);
            if (ability == null) return false;

            // Get current level
            int currentLevel = GetAbilityLevel(abilityName);

            // If not purchased and not default, can't upgrade
            if (currentLevel == 0) return false;

            // If already at max level, can't upgrade
            if (currentLevel >= ability.MaxLevel) return false;

            // Check if player has enough money
            int playerMoney = GameManager.Instance.CurrencyManager.GetCurrentCurrency();
            return playerMoney >= ability.GetUpgradeCostForLevel(currentLevel + 1);
        }

        public bool PurchaseAbility(string abilityName)
        {
            if (!CanPurchaseAbility(abilityName)) return false;

            var ability = _mutationManager.GetSpecialAbilityByName(abilityName);
            if (ability == null) return false;

            // Attempt to spend money
            int cost = ability.InitialPurchaseCost;
            if (!GameManager.Instance.CurrencyManager.SpendCurrency(cost)) return false;

            // Mark as purchased at level 1
            _purchasedAbilities[abilityName] = 1;

            // Save purchase data
            SavePurchaseData();

            // Update all upgrade buttons
            UpdateAllButtons();
            
            OnAbilityPurchased?.Invoke();
            return true;
        }

        public bool UpgradeAbility(string abilityName)
        {
            if (!CanUpgradeAbility(abilityName)) return false;

            var ability = _mutationManager.GetSpecialAbilityByName(abilityName);
            if (ability == null) return false;

            int currentLevel = GetAbilityLevel(abilityName);
            int upgradeCost = ability.GetUpgradeCostForLevel(currentLevel + 1);

            // Attempt to spend money
            if (!GameManager.Instance.CurrencyManager.SpendCurrency(upgradeCost)) return false;

            // If it was a default ability that's being upgraded for first time
            if (!_purchasedAbilities.ContainsKey(abilityName))
            {
                _purchasedAbilities[abilityName] = currentLevel + 1;
            }
            else
            {
                // Increment level
                _purchasedAbilities[abilityName] = currentLevel + 1;
            }

            // Save purchase data
            SavePurchaseData();
            
            // Update all upgrade buttons
            UpdateAllButtons();

            OnAbilityUpgraded?.Invoke();
            return true;
        }

        // Store UI can call this to get effect strength for given level
        public float GetAbilityEffectStrength(string abilityName, int level)
        {
            var ability = _mutationManager.GetSpecialAbilityByName(abilityName);
            if (ability == null) return 0f;

            return ability.GetEffectValueForLevel(level);
        }

        // Save purchase data to PlayerPrefs
        private void SavePurchaseData()
        {
            // Convert purchase dictionary to JSON string
            string purchaseJson = JsonUtility.ToJson(new SerializableDictionary<string, int>(_purchasedAbilities));
            PlayerPrefs.SetString("PurchasedAbilities", purchaseJson);
            PlayerPrefs.Save();
            
            // Make sure to update the UI after saving purchase data
            if (GameManager.Instance?.CurrencyManager != null)
            {
                GameManager.Instance.CurrencyManager.UpdateCurrencyUI();
            }
        }

        // Load purchase data from PlayerPrefs
        private void LoadPurchaseData()
        {
            _purchasedAbilities = new Dictionary<string, int>();
    
            if (PlayerPrefs.HasKey("PurchasedAbilities"))
            {
                string purchaseJson = PlayerPrefs.GetString("PurchasedAbilities");
                var loadedData = JsonUtility.FromJson<SerializableDictionary<string, int>>(purchaseJson);
                _purchasedAbilities = loadedData.ToDictionary();
                Debug.Log($"[StoreManager] Loaded {_purchasedAbilities.Count} purchased abilities from PlayerPrefs");
            }
            else
            {
                Debug.Log("[StoreManager] No saved purchase data found, using default values");
            }
    
            // After loading (or failing to load) from PlayerPrefs, ensure default abilities have their correct tiers
            ResetDefaultAbilityTiers();
        }
        
        // New method to reset default abilities to their correct tier levels
        private void ResetDefaultAbilityTiers()
        {
            if (_mutationManager == null)
            {
                // If we're in Awake, _mutationManager might not be available yet
                // We'll handle this in Start instead
                return;
            }

            // Get all default abilities and ensure their CurrentTier is at least 1
            var defaultAbilities = _mutationManager.GetDefaultUnlockedSpecialAbilities();
            foreach (var ability in defaultAbilities)
            {
                if (ability == null) continue;
        
                // Ensure the ability is set to at least tier 1
                if (ability.CurrentTier < 1)
                {
                    Debug.Log($"[StoreManager] Resetting default ability {ability.name} tier from {ability.CurrentTier} to 1");
                    ability.CurrentTier = 1;
                }
        
                // Special case for abilities that should start at higher tiers
                if (ability.DefaultStartTier > 1 && ability.CurrentTier < ability.DefaultStartTier)
                {
                    Debug.Log($"[StoreManager] Setting default ability {ability.name} to its DefaultStartTier: {ability.DefaultStartTier}");
                    ability.CurrentTier = ability.DefaultStartTier;
                }
            }
        }

        // Helper class to serialize dictionaries (since Unity's JsonUtility doesn't support them directly)
        [Serializable]
        private class SerializableDictionary<TKey, TValue>
        {
            [SerializeField] private List<TKey> keys = new List<TKey>();
            [SerializeField] private List<TValue> values = new List<TValue>();

            public SerializableDictionary() { }

            public SerializableDictionary(Dictionary<TKey, TValue> dictionary)
            {
                foreach (var kvp in dictionary)
                {
                    keys.Add(kvp.Key);
                    values.Add(kvp.Value);
                }
            }

            public Dictionary<TKey, TValue> ToDictionary()
            {
                Dictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();
                for (int i = 0; i < Mathf.Min(keys.Count, values.Count); i++)
                {
                    dict[keys[i]] = values[i];
                }
                return dict;
            }
        }
    }
}
