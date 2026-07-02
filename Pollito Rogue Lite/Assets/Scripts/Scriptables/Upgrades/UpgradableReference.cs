using Interfaces.Interfaces;
using UnityEngine;

namespace Scriptables.Upgrades
{
    [System.Serializable]
    public class UpgradableReference
    {
        [SerializeField] private ScriptableObject _upgradableScriptable;

        /// <summary>
        /// Returns the IUpgradable interface implementation from the serialized ScriptableObject
        /// </summary>
        /// <returns>The IUpgradable interface or null if invalid</returns>
        public IUpgradable GetUpgradable()
        {
            if (_upgradableScriptable == null)
            {
                Debug.LogError("[UpgradableReference] No upgradable scriptable assigned");
                return null;
            }

            if (!(_upgradableScriptable is IUpgradable))
            {
                Debug.LogError($"[UpgradableReference] The assigned ScriptableObject '{_upgradableScriptable.name}' does not implement IUpgradable");
                return null;
            }

            return _upgradableScriptable as IUpgradable;
        }

        /// <summary>
        /// Allows setting the upgradable via code
        /// </summary>
        /// <param name="upgradable">ScriptableObject that implements IUpgradable</param>
        public void SetUpgradable(ScriptableObject upgradable)
        {
            if (upgradable == null)
            {
                Debug.LogError("[UpgradableReference] Attempted to set null upgradable");
                return;
            }
            
            if (!(upgradable is IUpgradable))
            {
                Debug.LogError($"[UpgradableReference] The ScriptableObject '{upgradable.name}' does not implement IUpgradable");
                return;
            }
            
            _upgradableScriptable = upgradable;
        }

        /// <summary>
        /// Checks if the reference is valid without triggering error logs
        /// </summary>
        /// <returns>True if the reference exists and implements IUpgradable</returns>
        public bool HasValidReference()
        {
            return _upgradableScriptable != null && _upgradableScriptable is IUpgradable;
        }
    }
}

