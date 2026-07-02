namespace Interfaces
{
    using UnityEngine;

    namespace Interfaces
    {
        /// <summary>
        /// Interface for any object that can be upgraded in the store
        /// </summary>
        public interface IUpgradable
        {
            /// <summary>
            /// Unique identifier for this upgradable item
            /// </summary>
            string Id { get; }

            /// <summary>
            /// Display name of the upgradable item
            /// </summary>
            string DisplayName { get; }

            /// <summary>
            /// Icon representing the upgradable item
            /// </summary>
            Sprite Icon { get; }

            /// <summary>
            /// Initial cost to purchase this item
            /// </summary>
            int InitialPurchaseCost { get; }

            /// <summary>
            /// Maximum upgrade level for this item
            /// </summary>
            int MaxLevel { get; }

            /// <summary>
            /// Whether this item is unlocked by default
            /// </summary>
            bool UnlockedByDefault { get; }

            /// <summary>
            /// Get the cost to upgrade to the specified level
            /// </summary>
            int GetUpgradeCostForLevel(int level);

            /// <summary>
            /// Get the effect value (strength) at the specified level
            /// </summary>
            float GetEffectValueForLevel(int level);
        }
    }
}