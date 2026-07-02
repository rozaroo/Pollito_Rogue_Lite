using System.Collections.Generic;
using System.Linq;
using Enums;
using Scriptables;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Managers
{
    ///Maneja el orden y la selección de niveles
    ///Se asegura de que los niveles que se carguen sean compatibles con los buffs que tiene el jugador
    public class LevelSequencer : MonoBehaviour
    {
        [SerializeField] private LevelSequenceSO _levelSequenceSO; //Guarda la secuencia de niveles
        private string _cachedHubSceneName;

        private void Start()
        {
            if (GameManager.Instance == null) return;

            var currentScene = SceneManager.GetActiveScene();
            var isLoaderScene = currentScene.buildIndex == 0 ||
                currentScene.name.Equals("Loader", System.StringComparison.OrdinalIgnoreCase);

            if (!isLoaderScene) return;
            Debug.Log("[LevelSequencer] In loader scene, initializing first level");
            GameManager.Instance.LoadHub();
        }

        /// <summary>
        /// Obtiene el nombre de la escena hub desde los build settings
        /// </summary>
        public string GetHubSceneName()
        {
            // Si ya lo encontramos antes, devolver el cacheado
            if (!string.IsNullOrEmpty(_cachedHubSceneName)) return _cachedHubSceneName;
            
            // Buscar hub en todas las escenas del build
            for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                if (string.IsNullOrEmpty(scenePath)) continue;
                var sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (sceneName.ToLower() != "hub") continue;
                _cachedHubSceneName = sceneName;
                return _cachedHubSceneName;
            }

            // Si no existe hub, usar indice 1 como fallback
            if (SceneManager.sceneCountInBuildSettings > 1)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(1);
                if (!string.IsNullOrEmpty(scenePath))
                {
                    _cachedHubSceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                    return _cachedHubSceneName;
                }
            }

            // Si falla usar indice 0
            if (SceneManager.sceneCountInBuildSettings > 0)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(0);
                if (!string.IsNullOrEmpty(scenePath))
                {
                    _cachedHubSceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                    return _cachedHubSceneName;
                }
            }

            // Ultimo recurso asumir que se llama Hub
            _cachedHubSceneName = "Hub";
            return _cachedHubSceneName;
        }

        /// <summary>
        /// Devuelve niveles que pueden resolverse con los buffs actuales del jugador.
        /// </summary>
        /// <param name="playerBuffs">List of buff types the player currently has</param>
        /// <returns>List of puzzle levels that match the player's buffs</returns>
        public List<PuzzleSO> GetLevelsMatchingBuffs(List<BuffType> playerBuffs)
        {
            if (_levelSequenceSO == null || _levelSequenceSO.Levels == null || playerBuffs == null) return new List<PuzzleSO>();
            // Filtra niveles donde los buffs del jugador coincidan con alguna solución posible.
            return _levelSequenceSO.Levels
                .Where(level => level != null && DoBuffsMatchSolution(level, playerBuffs))
                .ToList();
        }

        /// <summary>
        /// Revisa si los buffs del jugador permiten resolver un nivel.
        /// </summary>
        private bool DoBuffsMatchSolution(PuzzleSO level, List<BuffType> playerBuffs)
        {
            if (level.PossibleSolutions == null || level.PossibleSolutions.Count == 0) return true; // Si no pide buffs específicos, cualquier jugador puede.
            // Revisa si alguna de las soluciones posibles está cubierta por los buffs del jugador.
            return level.PossibleSolutions.Any(solution => solution.RequiredBuffs.All(playerBuffs.Contains));
        }

        /// <summary>
        /// Busca el índice del siguiente nivel que coincida con los buffs del jugador.
        /// </summary>
        /// <param name="currentIndex">Current level index</param>
        /// <param name="playerBuffs">Player's current buffs</param>
        /// <returns>Index of next matching level or -1 if none found</returns>
        public int GetNextMatchingLevelIndex(int currentIndex, List<BuffType> playerBuffs)
        {
            if (_levelSequenceSO == null || _levelSequenceSO.Levels == null) return -1;
                

            for (int i = currentIndex + 1; i < _levelSequenceSO.Levels.Count; i++)
            {
                if (DoBuffsMatchSolution(_levelSequenceSO.Levels[i], playerBuffs)) return i;
            }

            return -1; // Si no encuentra, devuelve -1
        }

        public PuzzleSO GetFirstLevel() => GetLevelAtIndex(0);

        public PuzzleSO GetLevelAtIndex(int index)
        {
            if (_levelSequenceSO == null || _levelSequenceSO.Levels == null || _levelSequenceSO.Levels.Count == 0) return null;
            if (index < 0 || index >= _levelSequenceSO.Levels.Count) return null;
            return _levelSequenceSO.Levels[index];
        }

        public PuzzleSO GetNextLevel(int currentIndex) =>
            GetLevelAtIndex(currentIndex + 1);

        public string GetSceneNameAtIndex(int index)
        {
            PuzzleSO puzzleData = GetLevelAtIndex(index);
            return puzzleData?.name;
        }

        public string GetNextLevelSceneName(int currentIndex) =>
            GetSceneNameAtIndex(currentIndex + 1);

        public int GetLevelCount() =>
            (_levelSequenceSO?.Levels != null) ? _levelSequenceSO.Levels.Count : 0;

        public bool IsValidLevelIndex(int index) =>
            _levelSequenceSO != null &&
            _levelSequenceSO.Levels != null &&
            index >= 0 &&
            index < _levelSequenceSO.Levels.Count;

        public int GetLevelIndexByName(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName) || _levelSequenceSO?.Levels == null) return -1;
            for (int i = 0; i < _levelSequenceSO.Levels.Count; i++)
            {
                if (_levelSequenceSO.Levels[i]?.name == sceneName) return i;       
            }
            return -1;
        }
        /// Devuelve un nivel aleatorio que coincida con los buffs del jugador.
        /// Evita repetir niveles recientes (si se pasan en excludeIndices).
        public PuzzleSO GetRandomLevelMatchingBuffs(List<BuffType> playerBuffs = null, List<int> excludeIndices = null)
        {
            if (playerBuffs == null) playerBuffs = new List<BuffType>();
            if (_levelSequenceSO == null || _levelSequenceSO.Levels == null) return null;
            
            // Find all levels that match the player's current buffs
            var exactMatchLevels = new List<PuzzleSO>();
            var exactMatchIndices = new List<int>();
            var noSolutionLevels = new List<PuzzleSO>();
            var noSolutionIndices = new List<int>();
            // Revisa todos los niveles de la secuencia.
            for (int i = 0; i < _levelSequenceSO.Levels.Count; i++)
            {
                // Saltar los excluidos
                if (excludeIndices != null && excludeIndices.Contains(i)) continue;
                var level = _levelSequenceSO.Levels[i];
                if (level == null) continue;
                    

                // Check if level has no solutions defined
                bool hasNoSolutions = level.PossibleSolutions == null || level.PossibleSolutions.Count == 0;

                if (hasNoSolutions)
                {
                    noSolutionLevels.Add(level);
                    noSolutionIndices.Add(i);
                    continue;
                }

                if (DoesLevelMatchPlayerBuffs(level, playerBuffs))
                {
                    exactMatchLevels.Add(level);
                    exactMatchIndices.Add(i);
                }
            }

            // First try to use exact matches
            var matchingLevels = exactMatchLevels;
            var matchingIndices = exactMatchIndices;

            // If no exact matches, fall back to no-solution levels as last resort
            if (matchingLevels.Count == 0)
            {
                matchingLevels = noSolutionLevels;
                matchingIndices = noSolutionIndices;
            }

            // Nada disponible
            if (matchingLevels.Count == 0) return null;
                

            // Try to avoid loading the current level if possible
            string currentSceneName = SceneManager.GetActiveScene().name;

            // Si hay varias opciones, intenta no repetir el nivel actual
            if (matchingLevels.Count > 1)
            {
                // Find the current level index in our matching levels (if present)
                int currentLevelIndex = -1;
                for (int i = 0; i < matchingLevels.Count; i++)
                {
                    if (matchingLevels[i].name == currentSceneName)
                    {
                        currentLevelIndex = i;
                        break;
                    }
                }

                // If current level is in our matches, avoid selecting it
                if (currentLevelIndex >= 0)
                {
                    // Get a random index avoiding the current level
                    int randomIndex;
                    do
                    {
                        randomIndex = Random.Range(0, matchingLevels.Count);
                    } while (randomIndex == currentLevelIndex && matchingLevels.Count > 1);

                    // Store the actual level index in GameManager for tracking
                    if (GameManager.Instance != null) GameManager.Instance.SetLevelIndex(matchingIndices[randomIndex]);
                    return matchingLevels[randomIndex];
                }
            }
            // Seleccion por defecto
            int defaultRandomIndex = Random.Range(0, matchingLevels.Count);
            // Store the actual level index in GameManager for tracking
            if (GameManager.Instance != null) GameManager.Instance.SetLevelIndex(matchingIndices[defaultRandomIndex]);
            return matchingLevels[defaultRandomIndex];
        }

        /// <summary>
        /// Revisa si un nivel puede resolverse con los buffs del jugador
        /// </summary>
        private bool DoesLevelMatchPlayerBuffs(PuzzleSO level, List<BuffType> playerBuffs)
        {
            // No solutions defined means any buffs are fine
            if (level.PossibleSolutions == null || level.PossibleSolutions.Count == 0) return true;
            // Check if any solution can be completed with the player's buffs
            foreach (var solution in level.PossibleSolutions)
            {
                if (solution.RequiredBuffs == null || solution.RequiredBuffs.Count == 0) return true; // No requiere buffs
                // Check if player has all required buffs for this solution
                bool hasAllRequiredBuffs = solution.RequiredBuffs.All(requiredBuff => playerBuffs.Contains(requiredBuff));
                if (hasAllRequiredBuffs) return true; // Found a matching solution
            }
            return false; // No matching solution found
        }
    }
}