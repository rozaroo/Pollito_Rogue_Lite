using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scriptables;
using Serialization;

namespace Editor
{
    public class CuatropeBlackboard : EditorWindow
    {
        [SerializeField] private LevelSequenceSO _currentLevelSequence;
        private Vector2 _scrollPosition;
        [SerializeField] private SceneLevelReference _currentSceneReference;
        private bool _isDataLoaded;
        private string _currentSceneName;
        private bool _showSceneDetails = true;
        private SceneLevelReference _temporaryReference;
        private bool _isTemporaryReference = false;
        private UnityEditor.Editor _puzzleEditor;

        [MenuItem("4P Externals/4P Blackboard")]
        public static void ShowWindow()
        {
            GetWindow<CuatropeBlackboard>("4P Blackboard");
        }

        private void OnEnable()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneClosed += OnSceneClosed;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            _currentSceneName = GetCurrentSceneName();
            
            if (_currentLevelSequence == null)
                FindAndAssignLevelSequence();
                
            LoadData();
        }

        private void OnDisable()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneClosed -= OnSceneClosed;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            
            if (_puzzleEditor != null)
                DestroyImmediate(_puzzleEditor);
        }

        private void FindAndAssignLevelSequence()
        {
            // Try to find CompleteLevelSequenceSO first
            string[] guids = AssetDatabase.FindAssets("t:CompleteLevelSequenceSO");
            if (guids.Length > 0)
            {
                _currentLevelSequence = AssetDatabase.LoadAssetAtPath<LevelSequenceSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
                if (_currentLevelSequence != null)
                    Debug.Log($"Automatically loaded level sequence: {_currentLevelSequence.name}");
            }

            // Fallback to any LevelSequenceSO
            if (_currentLevelSequence == null)
            {
                guids = AssetDatabase.FindAssets("t:LevelSequenceSO");
                if (guids.Length > 0)
                {
                    _currentLevelSequence = AssetDatabase.LoadAssetAtPath<LevelSequenceSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
                    if (_currentLevelSequence != null)
                        Debug.Log($"Automatically loaded fallback level sequence: {_currentLevelSequence.name}");
                }
            }
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode || state == PlayModeStateChange.EnteredPlayMode)
            {
                string newSceneName = GetCurrentSceneName();
                if (newSceneName != _currentSceneName)
                {
                    _currentSceneName = newSceneName;
                    UpdateCurrentSceneReference();
                    Repaint();
                }
            }
        }

        private void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            _currentSceneName = scene.name;
            UpdateCurrentSceneReference();
            Repaint();
        }

        private void OnSceneClosed(Scene scene)
        {
            _currentSceneName = GetCurrentSceneName();
            UpdateCurrentSceneReference();
            Repaint();
        }

        private string GetCurrentSceneName() => SceneManager.GetActiveScene().name;

        private void UpdateCurrentSceneReference()
        {
            if (_currentLevelSequence == null || string.IsNullOrEmpty(_currentSceneName))
                return;

            _currentSceneReference = null;
            for (int i = 0; i < _currentLevelSequence.SceneLevelCount; i++)
            {
                SceneLevelReference reference = _currentLevelSequence.GetSceneLevelAt(i);
                if (reference != null && reference.SceneName == _currentSceneName)
                {
                    _currentSceneReference = reference;
                    UpdatePuzzleEditor();
                    Debug.Log($"Blackboard updated to scene: {_currentSceneName}");
                    return;
                }
            }

            Debug.LogWarning($"Scene '{_currentSceneName}' is not found in the level sequence.");
        }

        private void UpdatePuzzleEditor()
        {
            if (_puzzleEditor != null)
            {
                DestroyImmediate(_puzzleEditor);
                _puzzleEditor = null;
            }

            PuzzleSO puzzleToEdit = _isTemporaryReference ? _temporaryReference?.Level : _currentSceneReference?.Level;
            
            if (puzzleToEdit != null)
                _puzzleEditor = UnityEditor.Editor.CreateEditor(puzzleToEdit);
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            GUILayout.Label("4P Blackboard", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Level sequence field
            EditorGUI.BeginChangeCheck();
            _currentLevelSequence = (LevelSequenceSO)EditorGUILayout.ObjectField(
                "Level Sequence", _currentLevelSequence, typeof(LevelSequenceSO), false);
                
            if (EditorGUI.EndChangeCheck())
            {
                LoadData();
                UpdateCurrentSceneReference();
                if (_temporaryReference != null)
                    ClearTemporaryReference();
            }

            EditorGUILayout.LabelField("Current Scene", _currentSceneName ?? "None");

            // Control buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload"))
            {
                LoadData();
                UpdateCurrentSceneReference();
                if (_temporaryReference != null)
                    ClearTemporaryReference();
            }

            GUI.enabled = _isTemporaryReference && _temporaryReference != null;
            if (GUILayout.Button("Save Temporary Reference"))
                SaveTemporaryReference();
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Display data state
            if (_isDataLoaded && _currentLevelSequence != null)
            {
                EditorGUILayout.HelpBox(
                    $"Loaded {_currentLevelSequence.name} with {_currentLevelSequence.LevelCount} levels and {_currentLevelSequence.SceneLevelCount} scene references.",
                    MessageType.Info);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Current Scene Information", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Scene Name", _currentSceneName ?? "None");

                if (_currentSceneReference == null && !string.IsNullOrEmpty(_currentSceneName))
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox($"No level reference found for current scene: {_currentSceneName}", MessageType.Error);

                    if (!_isTemporaryReference)
                    {
                        if (GUILayout.Button("Create Temporary Reference"))
                            CreateTemporaryReference();
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Working with temporary reference. Click 'Save Temporary Reference' to make it permanent.", MessageType.Warning);
                        if (GUILayout.Button("Discard Temporary Reference"))
                            ClearTemporaryReference();
                    }
                }

                // Display reference details - either real or temporary
                SceneLevelReference referenceToDisplay = _isTemporaryReference ? _temporaryReference : _currentSceneReference;
                if (referenceToDisplay != null)
                {
                    EditorGUILayout.Space();
                    _showSceneDetails = EditorGUILayout.Foldout(_showSceneDetails, "Scene Level Reference Details", true);
                    if (_showSceneDetails)
                    {
                        EditorGUI.indentLevel++;
                        DisplaySceneLevelReferenceDetails(referenceToDisplay);
                        EditorGUI.indentLevel--;
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No data loaded. Please select a Level Sequence asset.", MessageType.Warning);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DisplaySceneLevelReferenceDetails(SceneLevelReference reference)
        {
            // Display scene name (read-only)
            GUI.enabled = false;
            EditorGUILayout.LabelField("Scene Name", reference.SceneName);
            GUI.enabled = true;

            PuzzleSO puzzleData = reference.Level;
            if (puzzleData == null)
            {
                EditorGUILayout.HelpBox("No puzzle data assigned to this reference.", MessageType.Warning);
                if (GUILayout.Button("Create New Puzzle Data"))
                    CreateNewPuzzleData(reference);
                return;
            }

            EditorGUILayout.ObjectField("Level Asset", puzzleData, typeof(PuzzleSO), false);
            EditorGUILayout.Space();

            // Draw puzzle editor if available
            if (_puzzleEditor != null && _puzzleEditor.target == puzzleData)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField("Puzzle Properties", EditorStyles.boldLabel);
                _puzzleEditor.OnInspectorGUI();
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(puzzleData);
                    AssetDatabase.SaveAssets();
                }
            }
            else
            {
                // Fallback editor if the Editor.CreateEditor approach fails
                EditorGUILayout.LabelField("Puzzle Properties", EditorStyles.boldLabel);

                SerializedObject serializedPuzzle = new SerializedObject(puzzleData);
                serializedPuzzle.Update();

                EditorGUI.BeginChangeCheck();

                SerializedProperty iterator = serializedPuzzle.GetIterator();
                bool enterChildren = true;
                iterator.NextVisible(true); // Skip script reference

                while (iterator.NextVisible(enterChildren))
                {
                    EditorGUILayout.PropertyField(iterator, true);
                    enterChildren = false;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    serializedPuzzle.ApplyModifiedProperties();
                    EditorUtility.SetDirty(puzzleData);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        private void CreateTemporaryReference()
        {
            if (string.IsNullOrEmpty(_currentSceneName) || _currentLevelSequence == null)
                return;

            _temporaryReference = new SceneLevelReference();

            var sceneNameField = typeof(SceneLevelReference).GetField("_sceneName",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            sceneNameField?.SetValue(_temporaryReference, _currentSceneName);

            PuzzleSO tempPuzzle = CreateTempPuzzle();

            var levelField = typeof(SceneLevelReference).GetField("_level",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            levelField?.SetValue(_temporaryReference, tempPuzzle);

            _isTemporaryReference = true;
            UpdatePuzzleEditor();
            Debug.Log($"Created temporary reference for scene: {_currentSceneName}");
        }

        private PuzzleSO CreateTempPuzzle()
        {
            string basePath = "Assets/Resources/Puzzles";
            string tempFolderPath = $"{basePath}/_temp";

            EnsureFolderExists("Assets/Resources");
            EnsureFolderExists(basePath);
            EnsureFolderExists(tempFolderPath);

            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string assetPath = $"{tempFolderPath}/TempPuzzle_{_currentSceneName}_{timestamp}.asset";

            PuzzleSO tempPuzzle = ScriptableObject.CreateInstance<PuzzleSO>();
            AssetDatabase.CreateAsset(tempPuzzle, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Created temporary puzzle at {assetPath}");
            return tempPuzzle;
        }

        private void ClearTemporaryReference()
        {
            if (_isTemporaryReference && _temporaryReference != null)
            {
                PuzzleSO tempPuzzle = _temporaryReference.Level;
                if (tempPuzzle != null && AssetDatabase.Contains(tempPuzzle))
                {
                    string assetPath = AssetDatabase.GetAssetPath(tempPuzzle);
                    AssetDatabase.DeleteAsset(assetPath);
                    AssetDatabase.Refresh();
                    Debug.Log($"Deleted temporary puzzle at {assetPath}");
                }

                _temporaryReference = null;
                _isTemporaryReference = false;
                UpdatePuzzleEditor();
            }
        }

        private void CreateNewPuzzleData(SceneLevelReference reference)
        {
            // Implementation left out
        }
        
        private void SaveTemporaryReference()
        {
            if (!_isTemporaryReference || _temporaryReference == null || _currentLevelSequence == null)
                return;
            
            try
            {
                // Find scene path to determine difficulty folder
                string[] sceneGuids = AssetDatabase.FindAssets($"t:Scene {_currentSceneName}");
                if (sceneGuids.Length == 0)
                {
                    EditorUtility.DisplayDialog("Error", $"Scene {_currentSceneName} not found", "OK");
                    return;
                }
                
                string scenePath = null;
                foreach (string guid in sceneGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.Contains("/Scenes/Levels/") && path.EndsWith($"{_currentSceneName}.unity"))
                    {
                        scenePath = path;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(scenePath))
                {
                    EditorUtility.DisplayDialog("Error", 
                        $"Scene {_currentSceneName} not found in Scenes/Levels/", "OK");
                    return;
                }

                // Extract difficulty name from path
                int levelsIndex = scenePath.IndexOf("/Levels/") + 8;
                int lastSlashIndex = scenePath.LastIndexOf('/');
                string difficultyName = scenePath.Substring(levelsIndex, lastSlashIndex - levelsIndex);

                // Create puzzle folder structure
                string basePath = "Assets/Resources/Puzzles";
                string difficultyPath = $"{basePath}/{difficultyName}";
                EnsureFolderExists("Assets/Resources");
                EnsureFolderExists(basePath);
                EnsureFolderExists(difficultyPath);

                // Set asset path and handle existing assets
                string assetPath = $"{difficultyPath}/{_currentSceneName}.asset";
                if (AssetDatabase.LoadAssetAtPath<PuzzleSO>(assetPath) != null)
                {
                    bool overwrite = EditorUtility.DisplayDialog("Asset Already Exists",
                        $"Puzzle '{_currentSceneName}' already exists. Overwrite?", "Yes", "No");
                    if (!overwrite) return;
                    AssetDatabase.DeleteAsset(assetPath);
                }

                // Get the temporary puzzle path
                string tempPuzzlePath = AssetDatabase.GetAssetPath(_temporaryReference.Level);
                if (string.IsNullOrEmpty(tempPuzzlePath))
                {
                    EditorUtility.DisplayDialog("Error", "Temporary puzzle asset path not found", "OK");
                    return;
                }

                // Copy temporary puzzle to permanent location
                bool copySuccess = AssetDatabase.CopyAsset(tempPuzzlePath, assetPath);
                if (!copySuccess)
                {
                    EditorUtility.DisplayDialog("Error", $"Failed to copy asset to {assetPath}", "OK");
                    return;
                }
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                PuzzleSO newPuzzle = AssetDatabase.LoadAssetAtPath<PuzzleSO>(assetPath);
                if (newPuzzle == null)
                {
                    EditorUtility.DisplayDialog("Error", $"Failed to load newly created puzzle at {assetPath}", "OK");
                    return;
                }

                // Add to level sequence using serialized properties
                SerializedObject serializedObject = new SerializedObject(_currentLevelSequence);
                serializedObject.Update();

                // Add to levels list
                SerializedProperty levelsProperty = serializedObject.FindProperty("_levels");
                if (levelsProperty != null && levelsProperty.isArray)
                {
                    int levelIndex = levelsProperty.arraySize;
                    levelsProperty.arraySize++;
                    SerializedProperty newLevel = levelsProperty.GetArrayElementAtIndex(levelIndex);
                    if (newLevel != null)
                    {
                        newLevel.objectReferenceValue = newPuzzle;
                    }
                }
                else
                {
                    Debug.LogError("Could not find '_levels' property in level sequence");
                }

                // Add to scene references list
                SerializedProperty sceneRefsProperty = serializedObject.FindProperty("_sceneReferences");
                if (sceneRefsProperty != null && sceneRefsProperty.isArray)
                {
                    int sceneRefIndex = sceneRefsProperty.arraySize;
                    sceneRefsProperty.arraySize++;
                    SerializedProperty sceneRef = sceneRefsProperty.GetArrayElementAtIndex(sceneRefIndex);
                    
                    if (sceneRef != null)
                    {
                        SerializedProperty levelProp = sceneRef.FindPropertyRelative("_level");
                        SerializedProperty sceneNameProp = sceneRef.FindPropertyRelative("_sceneName");
                        SerializedProperty displayNameProp = sceneRef.FindPropertyRelative("_displayName");
                        
                        if (levelProp != null) levelProp.objectReferenceValue = newPuzzle;
                        if (sceneNameProp != null) sceneNameProp.stringValue = _currentSceneName;
                        if (displayNameProp != null) displayNameProp.stringValue = _currentSceneName;
                    }
                }
                else
                {
                    Debug.LogError("Could not find '_sceneReferences' property in level sequence");
                }

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_currentLevelSequence);
                AssetDatabase.SaveAssets();

                // Clean up and update
                ClearTemporaryReference();
                UpdateCurrentSceneReference();

                Debug.Log($"<color=green>Saved puzzle at {assetPath} and added to level sequence</color>");
                EditorUtility.DisplayDialog("Success", $"Puzzle saved at {assetPath}", "OK");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error saving reference: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("Error", $"Failed to save: {ex.Message}", "OK");
            }
        }

        private void AddReferenceToLevelSequence(PuzzleSO puzzle, string sceneName, string displayName)
        {
            // Implementation left out
        }

        private void EnsureFolderExists(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string parentPath = System.IO.Path.GetDirectoryName(folderPath).Replace('\\', '/');
                string folderName = System.IO.Path.GetFileName(folderPath);
                AssetDatabase.CreateFolder(parentPath, folderName);
            }
        }

        private void LoadData()
        {
            _isDataLoaded = _currentLevelSequence != null;
        }

        [MenuItem("4P Externals/Clean Temporary Puzzles")]
        public static void CleanTemporaryPuzzles()
        {
            string tempFolderPath = "Assets/Resources/Puzzles/_temp";
            if (AssetDatabase.IsValidFolder(tempFolderPath))
            {
                string[] tempFiles = AssetDatabase.FindAssets("TempPuzzle_", new[] { tempFolderPath });
                int removedCount = 0;

                foreach (string guid in tempFiles)
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
                    removedCount++;
                }

                AssetDatabase.Refresh();
                string message = $"Removed {removedCount} temporary puzzles from {tempFolderPath}.";
                Debug.Log($"<color=green>{message}</color>");
                EditorUtility.DisplayDialog("Cleanup Complete", message, "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Cleanup Complete", "No temporary puzzles folder found.", "OK");
            }
        }
    }
}