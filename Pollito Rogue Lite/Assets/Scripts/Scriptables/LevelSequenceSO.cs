using System;
using System.Collections.Generic;
using System.Linq;
using Serialization;
using UnityEditor;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(fileName = "LevelSequence", menuName = "Scriptables/Level Sequence")]
    public class LevelSequenceSO : ScriptableObject
    {
        [SerializeField] private List<PuzzleSO> _levels = new();
        [SerializeField] private List<SceneLevelReference> _sceneReferences = new();
        [SerializeField, Tooltip("If true, the sequence will loop back to the first level after the last level is completed")] 
        private bool _loopSequence = false;
        
        public List<PuzzleSO> Levels => _levels;
        
        /// <summary>
        /// Whether the sequence should loop back to the first level after the last level is completed.
        /// </summary>
        public bool LoopSequence => _loopSequence;
        
        /// <summary>
        /// The total number of levels in the sequence.
        /// </summary>
        public int LevelCount => _levels?.Count ?? 0;

        /// <summary>
        /// The total number of scene references in the sequence.
        /// </summary>
        public int SceneLevelCount => _sceneReferences?.Count ?? 0;

        /// <summary>
        /// Gets the level (puzzle) at the specified index.
        /// </summary>
        /// <param name="index">The index of the level to retrieve.</param>
        /// <returns>The PuzzleSO at the specified index or null if the index is invalid.</returns>
        public PuzzleSO GetLevelAt(int index)
        {
            if (index >= 0 && index < LevelCount)
                return _levels[index];

            return null;
        }

        /// <summary>
        /// Gets the scene reference at the specified index.
        /// </summary>
        /// <param name="index">The index of the scene reference to retrieve.</param>
        /// <returns>The SceneLevelReference at the specified index or null if the index is invalid.</returns>
        public SceneLevelReference GetSceneLevelAt(int index)
        {
            if (index >= 0 && index < SceneLevelCount)
                return _sceneReferences[index];

            return null;
        }

        /// <summary>
        /// Gets the next level in the sequence after the specified index.
        /// </summary>
        /// <param name="currentIndex">The current level index.</param>
        /// <returns>
        /// A tuple containing:
        /// - The index of the next level (or the current index if there is no next level)
        /// - The PuzzleSO of the next level (or null if there is no next level)
        /// - The scene name to load (or null if scene name couldn't be determined)
        /// </returns>
        public (int nextIndex, PuzzleSO nextPuzzle, string sceneName) GetNextLevel(int currentIndex)
        {
            int nextIndex = currentIndex;
            PuzzleSO nextPuzzle = null;
            string sceneName = null;

            // Check if there are more levels
            if (currentIndex + 1 < LevelCount)
            {
                // There is a next level
                nextIndex = currentIndex + 1;
                nextPuzzle = GetLevelAt(nextIndex);
            }
            else if (_loopSequence && LevelCount > 0)
            {
                // Loop back to the first level
                nextIndex = 0;
                nextPuzzle = GetLevelAt(nextIndex);
            }
            else if (LevelCount > 0)
            {
                // No next level, stay on the last level
                nextIndex = currentIndex;
                nextPuzzle = GetLevelAt(nextIndex);
            }

            // Try to find a scene reference for this level
            if (nextPuzzle != null)
            {
                var sceneRef = _sceneReferences.FirstOrDefault(s => s.Level == nextPuzzle);
                if (sceneRef != null && !string.IsNullOrEmpty(sceneRef.SceneName))
                {
                    sceneName = sceneRef.SceneName;
                }
            }

            return (nextIndex, nextPuzzle, sceneName);
        }

        /// <summary>
        /// Gets the previous level in the sequence before the specified index.
        /// </summary>
        /// <param name="currentIndex">The current level index.</param>
        /// <returns>
        /// A tuple containing:
        /// - The index of the previous level (or the current index if there is no previous level)
        /// - The PuzzleSO of the previous level (or null if there is no previous level)
        /// </returns>
        public (int prevIndex, PuzzleSO prevPuzzle, string sceneName) GetPreviousLevel(int currentIndex)
        {
            int prevIndex = currentIndex - 1;
            PuzzleSO prevPuzzle = null;
            string sceneName = null;

            if (prevIndex >= 0 && prevIndex < LevelCount)
            {
                prevPuzzle = _levels[prevIndex];

                // Try to find a scene reference for this level
                if (prevPuzzle != null)
                {
                    var sceneRef = _sceneReferences.FirstOrDefault(s => s.Level == prevPuzzle);
                    if (sceneRef != null && !string.IsNullOrEmpty(sceneRef.SceneName))
                    {
                        sceneName = sceneRef.SceneName;
                    }
                }
            }

            return (prevIndex >= 0 ? prevIndex : currentIndex, prevPuzzle, sceneName);
        }

        #if UNITY_EDITOR
        [ContextMenu("[3] Find and Add Scenes to Build Settings")]
        private void FindAndAddScenesToBuildSettings()
        {
            // Find all scenes in the project
            var sceneGuids = AssetDatabase.FindAssets("t:Scene");
            var scenePaths = sceneGuids.Select(guid => AssetDatabase.GUIDToAssetPath(guid)).ToList();
            var sceneNames = scenePaths.Select(path => System.IO.Path.GetFileNameWithoutExtension(path)).ToList();
            
            var currentBuildScenes = EditorBuildSettings.scenes.ToList();
            var currentBuildScenePaths = currentBuildScenes.Select(s => s.path).ToList();
            
            List<EditorBuildSettingsScene> scenesToAdd = new List<EditorBuildSettingsScene>();
            List<string> addedSceneNames = new List<string>();
            List<string> notFoundSceneNames = new List<string>();
            
            // For each puzzle, try to find a matching scene name
            foreach (var puzzle in _levels)
            {
                if (puzzle == null) continue;
                
                // Get puzzle name from asset
                string puzzlePath = AssetDatabase.GetAssetPath(puzzle);
                string puzzleName = System.IO.Path.GetFileNameWithoutExtension(puzzlePath);
                
                // Find matching scene(s)
                var matchingScenePaths = scenePaths.Where(path => 
                    System.IO.Path.GetFileNameWithoutExtension(path).Equals(puzzleName, 
                    StringComparison.OrdinalIgnoreCase)).ToList();
                
                if (matchingScenePaths.Count > 0)
                {
                    foreach (var scenePath in matchingScenePaths)
                    {
                        // Check if already in build settings
                        if (!currentBuildScenePaths.Contains(scenePath))
                        {
                            scenesToAdd.Add(new EditorBuildSettingsScene(scenePath, true));
                            addedSceneNames.Add(System.IO.Path.GetFileNameWithoutExtension(scenePath));
                        }
                    }
                }
                else
                {
                    notFoundSceneNames.Add(puzzleName);
                }
            }
            
            // Add new scenes to build settings
            if (scenesToAdd.Count > 0)
            {
                var newBuildScenes = currentBuildScenes.Concat(scenesToAdd).ToArray();
                EditorBuildSettings.scenes = newBuildScenes;
                
                Debug.Log($"<color=green>Added {scenesToAdd.Count} scenes to build settings:</color>\n" + 
                          string.Join("\n", addedSceneNames.Select(name => $"• {name}.unity")));
                
                // Open build settings window
                EditorWindow.GetWindow(Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
            }
            
            // Update scene references based on what we found
            UpdateSceneReferences(scenePaths);
            
            // Report scenes that weren't found
            if (notFoundSceneNames.Count > 0)
            {
                string message = $"<color=yellow>Could not find {notFoundSceneNames.Count} scenes:</color>\n" + 
                                 string.Join("\n", notFoundSceneNames.Select(name => $"• {name}.unity"));
                
                Debug.LogWarning(message);
                
                // Ask if the user wants to create these scenes
                if (EditorUtility.DisplayDialog(
                    "Missing Scenes", 
                    $"Could not find {notFoundSceneNames.Count} scenes. Would you like to create them?", 
                    "Create Scenes", "Skip"))
                {
                    CreateMissingScenes(notFoundSceneNames);
                }
            }
        }
        
        /// <summary>
        /// Updates the scene references based on found scene paths
        /// </summary>
        private void UpdateSceneReferences(List<string> allScenePaths)
        {
            SerializedObject serializedObject = new SerializedObject(this);
            SerializedProperty sceneRefsProperty = serializedObject.FindProperty("_sceneReferences");
            bool anyChanges = false;
            
            // Ensure we have enough scene references
            while (_sceneReferences.Count < _levels.Count)
            {
                _sceneReferences.Add(new SceneLevelReference());
            }
            
            // If there are more scene references than levels, trim the list
            if (_sceneReferences.Count > _levels.Count)
            {
                _sceneReferences.RemoveRange(_levels.Count, _sceneReferences.Count - _levels.Count);
            }
            
            // Update scene references with found scenes
            for (int i = 0; i < _levels.Count; i++)
            {
                if (_levels[i] == null) continue;
                
                string puzzlePath = AssetDatabase.GetAssetPath(_levels[i]);
                string puzzleName = System.IO.Path.GetFileNameWithoutExtension(puzzlePath);
                
                var matchingScenePath = allScenePaths.FirstOrDefault(path => 
                    System.IO.Path.GetFileNameWithoutExtension(path).Equals(puzzleName, 
                    StringComparison.OrdinalIgnoreCase));
                
                if (!string.IsNullOrEmpty(matchingScenePath))
                {
                    SerializedProperty currentRef = sceneRefsProperty.GetArrayElementAtIndex(i);
                    SerializedProperty sceneNameProp = currentRef.FindPropertyRelative("_sceneName");
                    
                    string sceneName = System.IO.Path.GetFileNameWithoutExtension(matchingScenePath);
                    
                    if (sceneNameProp.stringValue != sceneName)
                    {
                        sceneNameProp.stringValue = sceneName;
                        anyChanges = true;
                    }
                }
            }
            
            if (anyChanges)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(this);
                Debug.Log($"<color=green>Updated scene references with found scenes</color>");
            }
        }

        [ContextMenu("[4] Validate Level-Scene Connections")]
        private void ValidateLevelSceneConnections()
        {
            bool allValid = true;
            List<string> validationMessages = new List<string>();

            // Check if both lists have the same count
            if (_levels.Count != _sceneReferences.Count)
            {
                validationMessages.Add($"Mismatch in counts: {_levels.Count} puzzles vs {_sceneReferences.Count} scenes");
                allValid = false;
            }

            // Check for null entries in puzzle list
            for (int i = 0; i < _levels.Count; i++)
            {
                if (_levels[i] == null)
                {
                    validationMessages.Add($"Puzzle at index {i} is null");
                    allValid = false;
                }
            }

            // Check for invalid scene entries
            for (int i = 0; i < _sceneReferences.Count; i++)
            {
                if (_sceneReferences[i] == null)
                {
                    validationMessages.Add($"Scene reference at index {i} is null");
                    allValid = false;
                    continue;
                }

                if (string.IsNullOrEmpty(_sceneReferences[i].SceneName))
                {
                    validationMessages.Add($"Scene name at index {i} is empty");
                    allValid = false;
                    continue;
                }

                // Validate if the scene exists in build settings
                if (!EditorBuildSettings.scenes.Any(s => s.path.Contains($"/{_sceneReferences[i].SceneName}.unity")))
                {
                    validationMessages.Add($"Scene '{_sceneReferences[i].SceneName}' at index {i} is not in build settings");
                    allValid = false;
                }
            }

            // Check puzzle-scene associations - validate that scene names match puzzle filenames
            for (int i = 0; i < Mathf.Min(_levels.Count, _sceneReferences.Count); i++)
            {
                if (_levels[i] != null && _sceneReferences[i] != null)
                {
                    // Get the puzzle asset filename without extension
                    string puzzleAssetPath = AssetDatabase.GetAssetPath(_levels[i]);
                    string puzzleFilename = System.IO.Path.GetFileNameWithoutExtension(puzzleAssetPath);

                    // Compare with scene name
                    if (_sceneReferences[i].SceneName != puzzleFilename)
                    {
                        validationMessages.Add($"Index {i}: Puzzle filename '{puzzleFilename}' doesn't match scene name '{_sceneReferences[i].SceneName}'");
                        allValid = false;
                    }
                }
            }

            // Display validation results
            if (allValid)
            {
                Debug.Log($"<color=green>All level-scene connections in {name} are valid!</color>");
            }
            else
            {
                Debug.LogWarning($"<color=yellow>Validation of {name} found issues:</color>");
                foreach (var message in validationMessages)
                {
                    Debug.LogWarning($"• {message}");
                }
            }
        }

        private void CreateMissingScenes(List<string> sceneNames)
        {
            string templateScenePath = EditorUtility.OpenFilePanel("Select Template Scene", "Assets", "unity");
            if (string.IsNullOrEmpty(templateScenePath))
            {
                Debug.LogWarning("Scene creation cancelled - no template selected.");
                return;
            }

            // Convert the full path to a project-relative path
            if (templateScenePath.StartsWith(Application.dataPath))
            {
                templateScenePath = "Assets" + templateScenePath.Substring(Application.dataPath.Length);
            }
            else
            {
                Debug.LogError("Template scene must be within the project's Assets folder.");
                return;
            }

            string scenesFolder = EditorUtility.OpenFolderPanel("Select Destination Folder for New Scenes", "Assets", "");
            if (string.IsNullOrEmpty(scenesFolder))
            {
                Debug.LogWarning("Scene creation cancelled - no destination folder selected.");
                return;
            }

            // Convert the full path to a project-relative path
            string relativeSceneFolder;
            if (scenesFolder.StartsWith(Application.dataPath))
            {
                relativeSceneFolder = "Assets" + scenesFolder.Substring(Application.dataPath.Length);
            }
            else
            {
                Debug.LogError("Destination folder must be within the project's Assets folder.");
                return;
            }

            List<EditorBuildSettingsScene> scenesToAdd = new List<EditorBuildSettingsScene>();

            foreach (var sceneName in sceneNames)
            {
                string newScenePath = System.IO.Path.Combine(relativeSceneFolder, $"{sceneName}.unity");

                // Check if scene already exists to avoid overwriting
                if (System.IO.File.Exists(newScenePath))
                {
                    Debug.LogWarning($"Scene {newScenePath} already exists. Skipping creation.");
                    continue;
                }

                // Copy the template scene to the new location
                bool success = AssetDatabase.CopyAsset(templateScenePath, newScenePath);
                if (success)
                {
                    Debug.Log($"Created scene: {newScenePath}");

                    // Add to our list to update build settings
                    scenesToAdd.Add(new EditorBuildSettingsScene(newScenePath, true));
                }
                else
                {
                    Debug.LogError($"Failed to create scene: {newScenePath}");
                }
            }

            // Update build settings with all the new scenes at once
            if (scenesToAdd.Count > 0)
            {
                List<EditorBuildSettingsScene> buildScenes = EditorBuildSettings.scenes.ToList();
                buildScenes.AddRange(scenesToAdd);
                EditorBuildSettings.scenes = buildScenes.ToArray();
                
                // Open build settings window for verification
                EditorWindow.GetWindow(Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
            }

            AssetDatabase.Refresh();
            Debug.Log($"<color=green>Scene creation complete. Added {scenesToAdd.Count} new scenes to build settings.</color>");
        }
        #endif
    }
}