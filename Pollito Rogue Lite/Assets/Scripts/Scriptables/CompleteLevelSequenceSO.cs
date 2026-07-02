using System;
using System.Collections.Generic;
using System.Linq;
using Enums;
using Serialization;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Scriptables
{
    [CreateAssetMenu(fileName = "CompleteLevelSequence", menuName = "Scriptables/Complete Level Sequence")]
    public class CompleteLevelSequenceSO : LevelSequenceSO
    {
        #if UNITY_EDITOR
        private const string BASE_LEVELS_PATH = "Assets/Scenes/Levels";
        private const string BASE_PUZZLES_PATH = "Assets/Resources/Puzzles";

        [ContextMenu("[1] Find All Levels")]
        private void FindAllLevels()
        {
            if (!ValidateLevelsDirectory())
                return;

            // Get all scene paths from all subfolders
            List<string> allScenePaths = FindAllScenes();
            if (allScenePaths.Count == 0)
                return;

            // Find or create matching PuzzleSO assets
            Dictionary<string, PuzzleSO> matchingPuzzles = FindMatchingPuzzles();
            List<string> missingPuzzles = FindMissingPuzzles(allScenePaths, matchingPuzzles);

            if (missingPuzzles.Count > 0 && PromptToCreateMissingPuzzles(missingPuzzles))
            {
                CreateMissingPuzzles(missingPuzzles);
                matchingPuzzles = FindMatchingPuzzles(); // Refresh after creation
            }

            // Create ordered lists with tutorials first
            var orderedItems = CreateOrderedLevelSequence(allScenePaths, matchingPuzzles);

            // Update the ScriptableObject with the new data
            UpdateScriptableObject(orderedItems.puzzles, orderedItems.sceneRefs);

            // Report results
            ReportResults(orderedItems.puzzles.Count, orderedItems.tutorialCount, allScenePaths, matchingPuzzles);
        }

        private bool ValidateLevelsDirectory()
        {
            if (!System.IO.Directory.Exists(System.IO.Path.Combine(Application.dataPath, "Scenes/Levels")))
            {
                Debug.LogError($"Directory not found: {BASE_LEVELS_PATH}");
                return false;
            }
            return true;
        }

        private List<string> FindAllScenes()
        {
            List<string> scenePaths = new List<string>();
            
            // Get all scene files in all subfolders of the Levels directory
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { BASE_LEVELS_PATH });
            
            foreach (string guid in sceneGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                scenePaths.Add(path);
            }

            if (scenePaths.Count == 0)
            {
                Debug.LogWarning("No .unity scenes found in any level folders");
            }
            else
            {
                Debug.Log($"Found {scenePaths.Count} level scenes in total");
            }

            return scenePaths;
        }

        private Dictionary<string, PuzzleSO> FindMatchingPuzzles()
        {
            Dictionary<string, PuzzleSO> matchingPuzzles = new Dictionary<string, PuzzleSO>();

            string[] puzzleGuids = AssetDatabase.FindAssets("t:PuzzleSO");
            foreach (string guid in puzzleGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PuzzleSO puzzle = AssetDatabase.LoadAssetAtPath<PuzzleSO>(path);

                if (puzzle != null)
                {
                    string puzzleName = System.IO.Path.GetFileNameWithoutExtension(path);
                    matchingPuzzles[puzzleName] = puzzle;
                }
            }

            Debug.Log($"Found {matchingPuzzles.Count} existing puzzle assets");
            return matchingPuzzles;
        }

        private List<string> FindMissingPuzzles(List<string> scenePaths, Dictionary<string, PuzzleSO> matchingPuzzles)
        {
            List<string> missingPuzzles = new List<string>();

            foreach (string scenePath in scenePaths)
            {
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (!matchingPuzzles.ContainsKey(sceneName))
                {
                    missingPuzzles.Add(sceneName);
                }
            }

            return missingPuzzles;
        }

        private bool PromptToCreateMissingPuzzles(List<string> missingPuzzles)
        {
            string message = $"Found {missingPuzzles.Count} scenes without matching PuzzleSO assets:\n" +
                             string.Join("\n", missingPuzzles.Select(name => $"• {name}"));

            return EditorUtility.DisplayDialog(
                "Missing Puzzles",
                message + "\n\nWould you like to create these puzzles now?",
                "Create Puzzles", "Continue Without Creating");
        }

        private (List<PuzzleSO> puzzles, List<SceneLevelReference> sceneRefs, int tutorialCount) CreateOrderedLevelSequence(
            List<string> scenePaths,
            Dictionary<string, PuzzleSO> matchingPuzzles)
        {
            var tutorialPuzzles = new List<PuzzleSO>();
            var tutorialSceneRefs = new List<SceneLevelReference>();
            var regularPuzzles = new List<PuzzleSO>();
            var regularSceneRefs = new List<SceneLevelReference>();

            // First pass: separate tutorial and regular levels
            foreach (string scenePath in scenePaths)
            {
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                
                // Skip if no matching puzzle exists
                if (!matchingPuzzles.TryGetValue(sceneName, out PuzzleSO puzzle))
                    continue;

                // Check if this is a tutorial level based on the puzzle's difficulty setting
                bool isTutorial = puzzle.Difficulty == PuzzleDifficultyTier.Tutorial;
                
                if (isTutorial)
                {
                    tutorialPuzzles.Add(puzzle);
                    tutorialSceneRefs.Add(CreateSceneLevelReference(puzzle, sceneName));
                }
                else
                {
                    regularPuzzles.Add(puzzle);
                    regularSceneRefs.Add(CreateSceneLevelReference(puzzle, sceneName));
                }
            }

            // Sort regular levels alphabetically by name
            var sortedRegular = regularPuzzles.Select((puzzle, index) => new { Puzzle = puzzle, SceneRef = regularSceneRefs[index], Name = puzzle.name })
                .OrderBy(x => x.Name)
                .ToList();

            // Sort tutorial levels alphabetically by name
            var sortedTutorials = tutorialPuzzles.Select((puzzle, index) => new { Puzzle = puzzle, SceneRef = tutorialSceneRefs[index], Name = puzzle.name })
                .OrderBy(x => x.Name)
                .ToList();

            // Combine lists: tutorials first, then regular levels
            var finalPuzzles = new List<PuzzleSO>();
            var finalSceneRefs = new List<SceneLevelReference>();

            // Add tutorials first
            foreach (var item in sortedTutorials)
            {
                finalPuzzles.Add(item.Puzzle);
                finalSceneRefs.Add(item.SceneRef);
            }

            // Add regular levels
            foreach (var item in sortedRegular)
            {
                finalPuzzles.Add(item.Puzzle);
                finalSceneRefs.Add(item.SceneRef);
            }

            return (finalPuzzles, finalSceneRefs, tutorialPuzzles.Count);
        }

        private SceneLevelReference CreateSceneLevelReference(PuzzleSO puzzle, string sceneName)
        {
            SceneLevelReference sceneRef = new SceneLevelReference();
            var sceneRefType = typeof(SceneLevelReference);

            // Set the level field
            var levelField = sceneRefType.GetField("_level", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (levelField != null)
            {
                levelField.SetValue(sceneRef, puzzle);
            }

            // Set the scene name field
            var sceneNameField = sceneRefType.GetField("_sceneName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (sceneNameField != null)
            {
                sceneNameField.SetValue(sceneRef, sceneName);
            }

            return sceneRef;
        }

        private void UpdateScriptableObject(List<PuzzleSO> orderedPuzzles, List<SceneLevelReference> orderedSceneRefs)
        {
            SerializedObject serializedObject = new SerializedObject(this);

            // Update levels
            SerializedProperty levelsProperty = serializedObject.FindProperty("_levels");
            levelsProperty.ClearArray();

            for (int i = 0; i < orderedPuzzles.Count; i++)
            {
                levelsProperty.arraySize++;
                levelsProperty.GetArrayElementAtIndex(i).objectReferenceValue = orderedPuzzles[i];
            }

            // Update scene references
            SerializedProperty sceneRefsProperty = serializedObject.FindProperty("_sceneReferences");
            sceneRefsProperty.ClearArray();

            for (int i = 0; i < orderedSceneRefs.Count; i++)
            {
                sceneRefsProperty.arraySize++;
                SerializedProperty sceneRef = sceneRefsProperty.GetArrayElementAtIndex(i);
                SerializedProperty levelProp = sceneRef.FindPropertyRelative("_level");
                SerializedProperty sceneNameProp = sceneRef.FindPropertyRelative("_sceneName");

                levelProp.objectReferenceValue = orderedPuzzles[i];
                sceneNameProp.stringValue = System.IO.Path.GetFileNameWithoutExtension(
                    AssetDatabase.GetAssetPath(orderedPuzzles[i]));
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        private void ReportResults(int puzzleCount, int tutorialCount, List<string> allScenePaths, Dictionary<string, PuzzleSO> matchingPuzzles)
        {
            Debug.Log($"<color=green>Successfully populated sequence with {puzzleCount} puzzles:</color>");
            Debug.Log($"<color=green>• {tutorialCount} tutorial levels (placed first)</color>");
            Debug.Log($"<color=green>• {puzzleCount - tutorialCount} regular levels</color>");

            // Report any scenes that still don't have puzzles
            var stillMissingScenes = new List<string>();
            foreach (string scenePath in allScenePaths)
            {
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (!matchingPuzzles.ContainsKey(sceneName))
                {
                    stillMissingScenes.Add(sceneName);
                }
            }

            if (stillMissingScenes.Count > 0)
            {
                Debug.LogWarning($"<color=yellow>{stillMissingScenes.Count} scenes still don't have matching puzzles:</color>\n" +
                                 string.Join("\n", stillMissingScenes.Select(name => $"• {name}")));
            }
        }

        private void CreateMissingPuzzles(List<string> puzzleNames)
        {
            // Create base Resources/Puzzles directory if it doesn't exist
            EnsurePuzzlesDirectoryExists();

            List<PuzzleSO> createdPuzzles = new List<PuzzleSO>();
            List<string> skippedPuzzles = new List<string>();

            // Show progress bar
            EditorUtility.DisplayProgressBar("Creating Puzzles", "Preparing...", 0f);

            // Create each puzzle
            for (int i = 0; i < puzzleNames.Count; i++)
            {
                string puzzleName = puzzleNames[i];
                float progress = (float)i / puzzleNames.Count;
                
                EditorUtility.DisplayProgressBar("Creating Puzzles", 
                    $"Creating {i+1}/{puzzleNames.Count}: {puzzleName}", 
                    progress);
                
                string assetPath = $"{BASE_PUZZLES_PATH}/{puzzleName}.asset";
                
                // Check if asset already exists
                if (AssetDatabase.LoadAssetAtPath<PuzzleSO>(assetPath) != null)
                {
                    PuzzleSO existingPuzzle = AssetDatabase.LoadAssetAtPath<PuzzleSO>(assetPath);
                    createdPuzzles.Add(existingPuzzle);
                    skippedPuzzles.Add(puzzleName);
                    continue;
                }

                // Create new PuzzleSO asset
                PuzzleSO newPuzzle = ScriptableObject.CreateInstance<PuzzleSO>();
                
                // Auto-detect if this might be a tutorial level from the name
                if (puzzleName.ToLower().Contains("tutorial"))
                {
                    newPuzzle.Difficulty = PuzzleDifficultyTier.Tutorial;
                }
                
                AssetDatabase.CreateAsset(newPuzzle, assetPath);
                createdPuzzles.Add(newPuzzle);
            }
            
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Report results
            ReportPuzzleCreationResults(createdPuzzles, skippedPuzzles);
        }

        private void EnsurePuzzlesDirectoryExists()
        {
            if (!AssetDatabase.IsValidFolder(BASE_PUZZLES_PATH))
            {
                // Create Resources folder if it doesn't exist
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                AssetDatabase.CreateFolder("Assets/Resources", "Puzzles");
            }
        }

        private void ReportPuzzleCreationResults(List<PuzzleSO> createdPuzzles, List<string> skippedPuzzles)
        {
            int newPuzzles = createdPuzzles.Count - skippedPuzzles.Count;
            
            string report = $"<color=green>Puzzle creation complete:</color>\n" +
                           $"• Created {newPuzzles} new puzzles\n";

            if (skippedPuzzles.Count > 0)
                report += $"• Skipped {skippedPuzzles.Count} existing puzzles\n";

            Debug.Log(report);

            // Option to repopulate the sequence with the newly created puzzles
            if (newPuzzles > 0 &&
                EditorUtility.DisplayDialog("Update Sequence",
                    $"Created {newPuzzles} puzzles. Would you like to populate the sequence with all puzzles?",
                    "Yes", "No"))
            {
                FindAllLevels();
            }
        }

        [ContextMenu("[2] Clean Null or Empty References")]
        private void CleanNullOrEmptyReferences()
        {
            SerializedObject serializedObject = new SerializedObject(this);

            // Get the scene references and levels properties
            SerializedProperty sceneRefsProperty = serializedObject.FindProperty("_sceneReferences");
            SerializedProperty levelsProperty = serializedObject.FindProperty("_levels");

            if (sceneRefsProperty == null || levelsProperty == null)
            {
                Debug.LogError("Could not find required properties for cleaning references");
                return;
            }

            // Find valid and invalid references
            int removedSceneRefs = 0;
            int removedLevels = 0;
            List<int> validSceneRefsIndices = new List<int>();
            List<int> validLevelsIndices = new List<int>();

            // Check all scene references
            for (int i = 0; i < sceneRefsProperty.arraySize; i++)
            {
                SerializedProperty sceneRef = sceneRefsProperty.GetArrayElementAtIndex(i);
                SerializedProperty levelProp = sceneRef.FindPropertyRelative("_level");
                SerializedProperty sceneNameProp = sceneRef.FindPropertyRelative("_sceneName");

                // Keep if both level and scene name are valid
                if (levelProp.objectReferenceValue != null &&
                    !string.IsNullOrEmpty(sceneNameProp.stringValue))
                {
                    validSceneRefsIndices.Add(i);
                }
                else
                {
                    removedSceneRefs++;
                }
            }

            // Check all levels
            for (int i = 0; i < levelsProperty.arraySize; i++)
            {
                if (levelsProperty.GetArrayElementAtIndex(i).objectReferenceValue != null)
                {
                    validLevelsIndices.Add(i);
                }
                else
                {
                    removedLevels++;
                }
            }

            // Only proceed if we found items to remove
            if (removedSceneRefs > 0 || removedLevels > 0)
            {
                // Remove invalid scene references
                if (removedSceneRefs > 0)
                {
                    RemoveInvalidSceneReferences(sceneRefsProperty, validSceneRefsIndices);
                }

                // Remove invalid levels
                if (removedLevels > 0)
                {
                    RemoveInvalidLevels(levelsProperty, validLevelsIndices);
                }

                // Apply changes
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();

                string message = $"Removed {removedSceneRefs} invalid scene references and {removedLevels} null levels.";
                Debug.Log($"<color=green>{message}</color>");
                EditorUtility.DisplayDialog("Cleanup Complete", message, "OK");
            }
            else
            {
                Debug.Log("<color=green>No invalid references found. Everything is clean!</color>");
                EditorUtility.DisplayDialog("Cleanup Complete", "No invalid references found. Everything is clean!", "OK");
            }
        }

        private void RemoveInvalidSceneReferences(SerializedProperty sceneRefsProperty, List<int> validSceneRefsIndices)
        {
            // Create a new scene references array with only valid items
            SerializedProperty newSceneRefs = new SerializedObject(this).FindProperty("_sceneReferences");

            // Start by clearing the array
            sceneRefsProperty.ClearArray();

            // Add valid items back
            for (int i = 0; i < validSceneRefsIndices.Count; i++)
            {
                sceneRefsProperty.arraySize++;
                SerializedProperty newRef = sceneRefsProperty.GetArrayElementAtIndex(i);
                SerializedProperty oldRef = newSceneRefs.GetArrayElementAtIndex(validSceneRefsIndices[i]);

                // Copy data from old to new
                CopySceneReferenceProperties(oldRef, newRef);
            }
        }

        private void RemoveInvalidLevels(SerializedProperty levelsProperty, List<int> validLevelsIndices)
        {
            SerializedProperty newLevels = new SerializedObject(this).FindProperty("_levels");

            // Start by clearing the array
            levelsProperty.ClearArray();

            // Add valid items back
            for (int i = 0; i < validLevelsIndices.Count; i++)
            {
                levelsProperty.arraySize++;
                SerializedProperty newLevel = levelsProperty.GetArrayElementAtIndex(i);
                SerializedProperty oldLevel = newLevels.GetArrayElementAtIndex(validLevelsIndices[i]);

                newLevel.objectReferenceValue = oldLevel.objectReferenceValue;
            }
        }

        private void CopySceneReferenceProperties(SerializedProperty source, SerializedProperty destination)
        {
            SerializedProperty srcLevel = source.FindPropertyRelative("_level");
            SerializedProperty srcSceneName = source.FindPropertyRelative("_sceneName");
            SerializedProperty srcDisplayName = source.FindPropertyRelative("_displayName");

            SerializedProperty dstLevel = destination.FindPropertyRelative("_level");
            SerializedProperty dstSceneName = destination.FindPropertyRelative("_sceneName");
            SerializedProperty dstDisplayName = destination.FindPropertyRelative("_displayName");

            dstLevel.objectReferenceValue = srcLevel.objectReferenceValue;
            dstSceneName.stringValue = srcSceneName.stringValue;

            // Handle display name if it exists
            if (srcDisplayName != null && dstDisplayName != null)
            {
                dstDisplayName.stringValue = srcDisplayName.stringValue;
            }
        }
        #endif
    }
}