using System.Collections.Generic;
using System.Linq;
using Audio;
using Enums;
using Hub;
using Player;
using Scriptables;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Managers
{
    public class GameManager : MonoBehaviour
    {
        [Header("Instanced Objects")]
        [SerializeField] private MutationManager _mutationManager;

        [Header("Components that need to be set")]
        [SerializeField] private AudioManager _audioManager;
        [SerializeField] private LevelManager _levelManager;
        [SerializeField] private LevelSequencer _levelSequencer;
        [SerializeField] private CurrencyManager _currencyManager;
        [SerializeField] private SpecialTilesManager _specialTilesManager;
        [SerializeField] private HubManager _hubManager;
    
        public static GameManager Instance { get; private set; }
        public MutationManager MutationManager => _mutationManager;
        public LevelSequencer LevelSequencer => _levelSequencer;
        public LevelManager LevelManager => _levelManager;
        public CurrencyManager CurrencyManager => _currencyManager;
        public SpecialTilesManager SpecialTilesManager => _specialTilesManager;
        public HubManager HubManager => _hubManager;
        public AudioManager AudioManager => _audioManager;

        [SerializeField] private bool _isFirstTimePlay = true;
        [SerializeField] private int _currentLevelIndex = 0;
        [SerializeField] private bool _isLevelCompleting = false;

        public bool IsFirstTimePlay => _isFirstTimePlay;
        public int CurrentLevelIndex
        {
            get => _currentLevelIndex;
            private set => _currentLevelIndex = value;
        }
        public bool IsLevelCompleting => _isLevelCompleting;

        public void SetLevelIndex(int index)
        {
            _currentLevelIndex = index;
        }

        public void SetHubManager(HubManager hubManager)
        {
            if (hubManager && _hubManager == hubManager) return;
            _hubManager = hubManager;
        }
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            // Initialize components if needed
            if (_mutationManager == null) _mutationManager = GetComponentInChildren<MutationManager>();
            if (_mutationManager == null) Debug.LogError("MutationManager not found. Please add it as a child to GameManager.");
            // Set initial mouse state based on current scene
            UpdateMouseLockState();
        }

        private void CompleteLevel()
        {
            _isFirstTimePlay = false;  // Set to false as soon as first level completes
            _isLevelCompleting = true; // Mark that level is completing to disable input
        }
    
        /// <summary>
        /// Resets the level completion flag
        /// </summary>
        public void ResetLevelCompletion()
        {
            _isLevelCompleting = false;
        }

        private void NextLevel()
        {
            _currentLevelIndex++;
        }

        public void SetLevelManager(LevelManager levelManager)
        {
            _levelManager = levelManager;
        }
    
        // Add these new fields to track selected buffs/debuffs
        private ChildOption _selectedChildOption;
        private bool _pendingChildOptionApplication;
    
        // Add method to store the selected child option
        public void SetSelectedChildOption(ChildOption option)
        {
            _selectedChildOption = option;
            _pendingChildOptionApplication = true;
        }
    
        // Add method to check and clear pending application state
        public bool HasPendingChildOption()
        {
            if (_pendingChildOptionApplication)
            {
                _pendingChildOptionApplication = false;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Loads the hub scene using the cached hub scene name from LevelSequencer
        /// </summary>
        /// <summary>
        /// Loads the hub scene using the hub scene name from LevelSequencer
        /// </summary>
        public void LoadHub()
        {
            Debug.Log("[GameManager] Loading hub scene");

            // Return early if LevelSequencer is not available
            if (_levelSequencer == null) return;

            // Get the hub scene name from LevelSequencer
            string hubSceneName = _levelSequencer.GetHubSceneName();
            Debug.Log($"[GameManager] Loading hub scene: {hubSceneName}");

            // Check if current scene is Loader - if so, don't use fade transition
            bool isLoaderScene = SceneManager.GetActiveScene().name.Equals("Loader", 
                System.StringComparison.OrdinalIgnoreCase);

            if (isLoaderScene && LoadSceneManager.Instance != null)
            {
                // When loading from Loader scene, temporarily disable transitions
                var previousSetting = LoadSceneManager.Instance.GetUseTransitionAnimations();
                LoadSceneManager.Instance.SetUseTransitionAnimations(false);
                LoadSceneManager.Instance.LoadScene(hubSceneName);
                // Restore previous setting for future transitions
                LoadSceneManager.Instance.SetUseTransitionAnimations(previousSetting);
            }
            else LoadSceneManager.Instance?.LoadScene(hubSceneName); // Normal load with transitions
        }
    
        // Get the selected option and clear it
        public ChildOption GetAndClearSelectedChildOption()
        {
            var option = _selectedChildOption;
            _selectedChildOption = null;
            return option;
        }
        
        // In GameManager.cs
        public void LoadLevel(int index)
        {
            if (LevelSequencer == null) return;
            var levelData = LevelSequencer.GetLevelAtIndex(index);
            if (levelData == null) return;
            // Update the current level index
            _currentLevelIndex = index;
            
            // Use LoadSceneManager to load the scene
            LoadSceneManager.Instance?.LoadScene(levelData.name);
        }
        
        /// <summary>
        /// Initializes the game with the first level from the sequence
        /// </summary>
        public void InitializeWithFirstLevel()
        {
            ResetLevelCompletion();
            
            // Reset the current level index to 0
            _currentLevelIndex = 0;
    
            // Make sure we're in first time play mode
            _isFirstTimePlay = true;
    
            // Get the first level scene name from the level sequencer
            if (_levelSequencer == null) return;
            var firstLevelScene = _levelSequencer.GetSceneNameAtIndex(_currentLevelIndex);
            if (!string.IsNullOrEmpty(firstLevelScene)) LoadSceneManager.Instance?.LoadScene(firstLevelScene); // Use LoadSceneManager to load the first level scene
        }
        
        public void HandleLevelCompleted()
        {
            Debug.Log("[GameManager] HandleLevelCompleted called");
            CompleteLevel();

            // If a child option is selected, process it and load the next level
            if (_selectedChildOption != null || _isFirstTimePlay)
            {
                if (_isFirstTimePlay)
                {
                    Debug.Log("[GameManager] First time play completed");
                    _isFirstTimePlay = false;
                }
                if (_selectedChildOption != null) Debug.Log($"[GameManager] Selected child option: {_selectedChildOption.Buffs}");
                NextLevel();
                Debug.Log($"[GameManager] Advanced to level index: {_currentLevelIndex}");

                if (_levelSequencer != null)
                {
                    // Check if we have a child option with buffs
                    if (_selectedChildOption != null && _selectedChildOption.Buffs != null && _selectedChildOption.Buffs.Count > 0)
                    {
                        // Convert BuffSO objects to BuffType enums
                        List<BuffType> optionBuffs = _selectedChildOption.Buffs.Select(buff => buff.Type).ToList();
                        Debug.Log($"[GameManager] Child option has {optionBuffs.Count} buffs: {string.Join(", ", optionBuffs)}");

                        // Get a random level matching the selected buffs
                        PuzzleSO randomLevel = _levelSequencer.GetRandomLevelMatchingBuffs(optionBuffs);

                        if (randomLevel != null)
                        {
                            Debug.Log($"[GameManager] Loading random level matching selected buffs: {randomLevel.name}");
                            LoadSceneManager.Instance?.LoadScene(randomLevel.name);
                        }
                    }
                    
                    else
                    {
                        Debug.Log("[GameManager] No buffs in selected child option or no child option selected - getting a random level");
    
                        // Get a random level without specifying buffs
                        PuzzleSO randomLevel = _levelSequencer.GetRandomLevelMatchingBuffs(new List<BuffType>());
    
                        if (randomLevel != null)
                        {
                            Debug.Log($"[GameManager] Loading random level (no buffs specified): {randomLevel.name}");
                            LoadSceneManager.Instance?.LoadScene(randomLevel.name);
                        }
                    }
                }
            }
        }

        [Header("Mouse Settings")]
        [SerializeField] private bool _lockMouseInGame = true;
        [SerializeField] private bool _showCursorInMenus = true;

        private bool _isInGameplay = false;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Determine if we're in gameplay or a menu scene
            _isInGameplay = !scene.name.Contains("Menu");
            
            // Force cursor lock and hide regardless of scene type
            UpdateMouseLockState();
            
            // Check for hub scene specifically and set reference
            if (scene.name.Contains("Hub"))
            {
                Debug.Log("[GameManager] Detected hub scene load - ensuring cursor is locked and hidden");
                // Force an immediate cursor update without waiting for HubManager
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        /// <summary>
        /// Updates the cursor lock state - mouse will be locked and hidden by default
        /// </summary>
        /// <param name="showMouse">If true, will show the mouse. If false (default), will lock and hide the mouse.</param>
        public void UpdateMouseLockState(bool showMouse = false)
        {
            if (showMouse)
            {
                // Only show cursor if explicitly requested
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Debug.Log("[GameManager] Cursor visible and unlocked (explicitly requested)");
            }
            else
            {
                // Default behavior: always lock and hide cursor
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                Debug.Log("[GameManager] Cursor locked and hidden (default state)");
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                Debug.Log("PlayerPrefs cleared.");
            }
            // Allow the player to toggle the cursor with Escape key
            if (Input.GetKeyDown(KeyCode.Escape)) ToggleCursorState();
        }

        private void ToggleCursorState()
        {
            if (_isInGameplay)
            {
                // Toggle between locked and visible states
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }
    }
}