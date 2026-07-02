using System.Collections.Generic;
using System.Linq;
using Enums;
using Scriptables;
using Scriptables.Abilities;
using UnityEngine;

namespace Managers
{
    /// <summary>
    /// Manages the loading, retrieval, and random selection of Buffs, Debuffs, and Special Abilities in the game.

    //Este MutationManager es el catálogo central de efectos. Carga desde Resources, filtra lo válido y te da acceso para: 
    // Buscar por tipo/nombre. Seleccionar aleatorios. Diferenciar entre lo desbloqueado y lo que hay que conseguir.
    /// </summary>
    public class MutationManager : MonoBehaviour
    {
        [Header("Effect Collections")]
        [SerializeField] private List<BuffSO> _availableBuffs = new(); // List of available Buffs.
        [SerializeField] private List<DebuffSO> _availableDebuffs = new(); // List of available Debuffs.
        [SerializeField] private List<SpecialAbilitySO> _availableSpecialAbilities = new(); // List of available Special Abilities.

        /// <summary>
        /// Gets the list of available Buffs as a read-only collection.
        /// </summary>
        public IReadOnlyList<BuffSO> AvailableBuffs => _availableBuffs;

        /// <summary>
        /// Gets the list of available Debuffs as a read-only collection.
        /// </summary>
        public IReadOnlyList<DebuffSO> AvailableDebuffs => _availableDebuffs;

        /// <summary>
        /// Gets the list of available Special Abilities as a read-only collection.
        /// </summary>
        public IReadOnlyList<SpecialAbilitySO> AvailableSpecialAbilities => _availableSpecialAbilities;

        /// <summary>
        /// Unity's Awake method. Loads Buffs, Debuffs, and Special Abilities from resources.
        /// </summary>
        private void Awake() => LoadEffects(); //Cargamos todos los efectos desde Resources.

        /// <summary>
        /// Retrieves a Debuff by its type.
        /// </summary>
        /// <param name="type">The type of the Debuff to retrieve.</param>
        /// <returns>The DebuffSO matching the specified type, or null if not found.</returns>
        public DebuffSO GetDebuffByType(DebuffType type) =>
            _availableDebuffs.FirstOrDefault(d => d.Type == type); //Devuelve un debuff por su tipo enum

        /// <summary>
        /// Retrieves a Buff by its type.
        /// </summary>
        /// <param name="type">The type of the Buff to retrieve.</param>
        /// <returns>The BuffSO matching the specified type, or null if not found.</returns>
        public BuffSO GetBuffByType(BuffType type) =>
            _availableBuffs.FirstOrDefault(b => b.Type == type);

        /// <summary>
        /// Retrieves a Special Ability by its name.
        /// </summary>
        /// <param name="abilityName">The name of the Special Ability to retrieve.</param>
        /// <returns>The SpecialAbilitySO matching the specified name, or null if not found.</returns>
        public SpecialAbilitySO GetSpecialAbilityByName(string abilityName) =>
            _availableSpecialAbilities.FirstOrDefault(a => a.name == abilityName);

        /// <summary>
        /// Loads Buffs, Debuffs, and Special Abilities from the Resources folder.
        /// </summary>
        /// Carga Buffs, Debuffs y Habilidades Especiales desde la carpeta Resources.
        /// Rutas esperadas (relativas a una carpeta "Resources" en el proyecto):
        private void LoadEffects()
        {
            // Cargamos y filtramos Buffs y Debuffs con un método genérico que además aplica filtros comunes (enabled, developmentOnly).
            _availableBuffs = LoadEffectsFromResources<BuffSO>("Scriptables/Effects/Buffs");
            _availableDebuffs = LoadEffectsFromResources<DebuffSO>("Scriptables/Effects/Debuffs");
            // Para Habilidades Especiales no aplicamos el filtro de BaseEffectSO (pueden tener otra base),
            // solo nos aseguramos de ignorar nulls.
            _availableSpecialAbilities = Resources.LoadAll<SpecialAbilitySO>("Scriptables/Abilities")
                .Where(a => a != null)
                .ToList();
        }

        /// <summary>
        /// Loads effects of a specific type from a given Resources path.
        /// </summary>
        /// <typeparam name="T">The type of ScriptableObject to load.</typeparam>
        /// <param name="path">The path in the Resources folder to load from.</param>
        /// <returns>A list of loaded effects of the specified type.</returns>
        private static List<T> LoadEffectsFromResources<T>(string path) where T : ScriptableObject =>
            Resources.LoadAll<T>(path)
                .OfType<BaseEffectSO>()
                .Where(e => e.enabled && (!e.developmentOnly || Debug.isDebugBuild))
                .Cast<T>()
                .ToList();

        /// <summary>
        /// Retrieves a random selection of effects from a given list.
        /// </summary>
        /// <typeparam name="T">The type of effects in the list.</typeparam>
        /// <param name="effects">The list of effects to select from.</param>
        /// <param name="count">The number of random effects to retrieve.</param>
        /// <returns>A list of randomly selected effects.</returns>
        public List<T> GetRandomEffects<T>(List<T> effects, int count = 1) =>
            effects.OrderBy(_ => Random.value)
                .Take(Mathf.Clamp(count, 1, effects.Count))
                .ToList();

        /// <summary>
        /// Retrieves a random selection of Buffs.
        /// </summary>
        /// <param name="count">The number of random Buffs to retrieve. Defaults to 1.</param>
        /// <returns>A list of randomly selected Buffs.</returns>
        public List<BuffSO> GetRandomBuffs(int count = 1) => GetRandomEffects(_availableBuffs, count);

        /// <summary>
        /// Retrieves a random selection of Debuffs.
        /// </summary>
        /// <param name="count">The number of random Debuffs to retrieve. Defaults to 1.</param>
        /// <returns>A list of randomly selected Debuffs.</returns>
        public List<DebuffSO> GetRandomDebuffs(int count = 1) => GetRandomEffects(_availableDebuffs, count);

        /// <summary>
        /// Retrieves a random selection of Special Abilities.
        /// </summary>
        /// <param name="count">The number of random Special Abilities to retrieve. Defaults to 1.</param>
        /// <returns>A list of randomly selected Special Abilities.</returns>
        /// Devuelve Habilidades Especiales aleatorias.
        public List<SpecialAbilitySO> GetRandomSpecialAbilities(int count = 1) => 
            GetRandomEffects(_availableSpecialAbilities.ToList(), count);

        /// <summary>
        /// Retrieves all unlockable special abilities.
        /// </summary>
        /// <returns>A list of special abilities that can be unlocked.</returns>
        public List<SpecialAbilitySO> GetUnlockableSpecialAbilities() =>
            _availableSpecialAbilities.Where(a => !a.UnlockedByDefault).ToList();

        /// <summary>
        /// Retrieves all special abilities that are unlocked by default.
        /// </summary>
        /// <returns>A list of special abilities that are unlocked by default.</returns>
        public List<SpecialAbilitySO> GetDefaultUnlockedSpecialAbilities() =>
            _availableSpecialAbilities.Where(a => a.UnlockedByDefault).ToList();
    }
}