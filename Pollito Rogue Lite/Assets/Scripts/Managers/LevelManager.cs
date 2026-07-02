using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Enums;
using Player;
using Scriptables;
using Tiles;
using UI;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace Managers
{
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private EntryPoint _entryPoint;
        [SerializeField] private ExitPoint _exitPoint;
        [SerializeField] private GameCanvasManager _gameCanvasManager;
        [SerializeField] private TilemapManager _tilemapManager;
        [SerializeField] private PuzzleController _puzzleController;
        // Add this field to the LevelManager class
        [SerializeField] private bool _isBlockMoving = false;
        public bool IsBlockMoving => _isBlockMoving;
        private int _currentLevelIndex = 0;
        private PlayerController _playerController;
        private GameObject _currentPlayer;
        private readonly List<ChildOption> _generatedOptions = new();
        private bool _levelCompleted = false;

        
        public PlayerController Player => _playerController;
        public TilemapManager TilemapManager => _tilemapManager;
        public GameCanvasManager GameCanvasManager => _gameCanvasManager;
        public int CurrentLevelIndex => _currentLevelIndex;

        [Header("Audio")]
        [SerializeField] private AudioClip _UISound;
        [SerializeField] private float _UIVolume = 2f;
        [SerializeField] private AudioClip _DeathSound;
        [SerializeField] private float _DeathVolume = 1f;
        private AudioSource _audioSource;
        
        private void Update()
        {
            // Check for F1 key press to restart the current level
            if (Input.GetKeyDown(KeyCode.F1)) RestartCurrentLevel();
        }
        
        /// <summary>
        /// Restarts the current level without resetting game progress
        /// </summary>
        public void RestartCurrentLevel()
        {
            Debug.Log("[LevelManager] Restarting current level");

            // Store the current player's buffs and debuffs before restarting
            if (_currentPlayer != null && _playerController != null)
            {
                var buffs = _playerController.GetBuffs();
                var debuffs = _playerController.GetCurrentDebuffs();
        
                // Create a temporary ChildOption to preserve the current character
                var currentChildOption = new ChildOption(buffs, debuffs);
        
                // Store this in GameManager to be used after restart
                if (GameManager.Instance != null)
                {
                    Debug.Log("[LevelManager] Preserving current character buffs and debuffs for level restart");
                    GameManager.Instance.SetSelectedChildOption(currentChildOption);
                }
            }

            // Get the current scene name
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            // Use LoadSceneManager to reload the current scene
            if (LoadSceneManager.Instance != null) LoadSceneManager.Instance.LoadScene(currentSceneName);
            else
            {
                Debug.LogWarning("[LevelManager] LoadSceneManager not found, using SceneManager directly");
                UnityEngine.SceneManagement.SceneManager.LoadScene(currentSceneName);
            }
        }
        
        private void OnEnable()
        {
            GameLevelEvents.OnLevelCompleted += HandleLevelCompleted;
            GameLevelEvents.OnPlayerSpawnRequested += SpawnPlayer;
        }

        private void OnDisable()
        {
            GameLevelEvents.OnLevelCompleted -= HandleLevelCompleted;
            GameLevelEvents.OnPlayerSpawnRequested -= SpawnPlayer;
            UnsubscribeFromPlayerEvents();
        }


        private void Awake()
        {
            if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
            if (_entryPoint == null || _exitPoint == null) Debug.LogError("[LevelManager] Missing entry or exit point reference!");
            // Auto-find missing references for builds
            if (_puzzleController == null)
            {
                Debug.LogWarning("[LevelManager] PuzzleController not found, attempting to find one...");
                _puzzleController = FindObjectOfType<PuzzleController>();
                if (_puzzleController == null) Debug.LogError("[LevelManager] Failed to find PuzzleController in scene!");
            }
        }
        
        public void Start()
        {
            // Reset game state when level starts
            ResetGameState();

            if (GameManager.Instance != null)
            {
                // Set reference to this LevelManager in GameManager
                GameManager.Instance.SetLevelManager(this);

                // Update currency UI when a new level is loaded
                if (GameManager.Instance.CurrencyManager != null)
                {
                    Debug.Log("[LevelManager] Notifying CurrencyManager that a new level has loaded");
                    GameManager.Instance.CurrencyManager.UpdateCurrencyUI();
                }

                // Get the current scene name
                string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

                // If we have a valid scene name, find its index in the level sequencer
                if (!string.IsNullOrEmpty(currentSceneName) && GameManager.Instance.LevelSequencer != null)
                {
                    int levelIndex = GameManager.Instance.LevelSequencer.GetLevelIndexByName(currentSceneName);

                    if (levelIndex >= 0)
                    {
                        // Update current level index in both LevelManager and GameManager
                        _currentLevelIndex = levelIndex;
                        GameManager.Instance.SetLevelIndex(levelIndex);

                        // Log the actual level data we're using
                        var levelData = GameManager.Instance.LevelSequencer.GetLevelAtIndex(levelIndex);
                        _tilemapManager.SetCurrentPuzzle(levelData);
                        _puzzleController.SetCurrentPuzzle(levelData);
                    }
                }
                else
                {
                    // Fallback to the index from GameManager
                    _currentLevelIndex = GameManager.Instance.CurrentLevelIndex;

                    // Log the actual level data we're using
                    var levelData = GameManager.Instance.LevelSequencer.GetLevelAtIndex(_currentLevelIndex);
                    _tilemapManager.SetCurrentPuzzle(levelData);
                    _puzzleController.SetCurrentPuzzle(levelData);
                }
            }

            // Get current puzzle from TilemapManager
            if (_tilemapManager != null)
            {
                if (_tilemapManager.CurrentPuzzle != null)
                {
                    // Initialize the move counter
                    InitializeMoves(_tilemapManager.CurrentPuzzle.MaxMoves);
                    _tilemapManager.InitializePuzzleGoals();
                }
            }
            GameLevelEvents.RequestPlayerSpawn(_entryPoint);
            if (Debug.isDebugBuild) GameManager.Instance.LevelManager.GameCanvasManager.DevelopmentBuffsUI.PopulateBuffToggles(_playerController.GetBuffs(), _playerController.GetCurrentDebuffs());
        }


        /// <summary>
        /// Disables the player's components, making the player inactive in the game.
        /// </summary>
        private void DisablePlayer()
        {
            if (_currentPlayer == null) return;

            // Disable the PlayerController component if it exists.
            var controller = _currentPlayer.GetComponent<PlayerController>();
            if (controller != null) controller.enabled = false;

            // Disable the SpriteRenderer component if it exists.
            var renderer = _currentPlayer.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null) renderer.enabled = false;

            // Disable the Collider2D component if it exists.
            var collider = _currentPlayer.GetComponent<Collider2D>();
            if (collider != null) collider.enabled = false;
        }
        
        /// <summary>
        /// Handles the completion of the level by disabling the player, calculating currency,
        /// and triggering level completion logic.
        /// </summary>
        private void HandleLevelCompleted()
        {
            // Set the flag to prevent any death conditions from triggering
            _levelCompleted = true;

            if (_currentPlayer != null)
            {
                // Disable the PlayerController component
                var playerController = _currentPlayer.GetComponent<PlayerController>();
                if (playerController != null) playerController.enabled = false;
            }

            // If it's not the first playthrough, show level summary and then child selection
            if (GameManager.Instance != null)
            {
                DisablePlayer();
                ShowLevelSummary();
                return;
            }
            // For first time play, directly delegate to GameManager to handle level completion
            if (GameManager.Instance != null) GameManager.Instance.HandleLevelCompleted();
        }

        /// <summary>
        /// Shows the level summary UI with stats before proceeding to child selection.
        /// </summary>
        private void ShowLevelSummary()
        {
            if (_gameCanvasManager?.LevelSummaryUI == null)
            {
                // If no summary UI is available, proceed directly to child selection
                ShowChildSelection();
                return;
            }
            //Reproducir aca efecto de UI
            PlayUISound();
            // Prepare the stat summary data
            List<StatSummaryData> summaryStats = new List<StatSummaryData>();
            
            // Get multipliers from CurrencyManager
            int moveMultiplier = 10; // Default fallback value
            int foodMultiplier = 25; // Default fallback value
            
            if (GameManager.Instance?.CurrencyManager != null)
            {
                moveMultiplier = GameManager.Instance.CurrencyManager.MovesMultiplier;
                foodMultiplier = GameManager.Instance.CurrencyManager.FoodMultiplier;
            }
            
            // Handle moves - whether unlimited or limited
            if (_tilemapManager?.CurrentPuzzle != null)
            {
                if (_tilemapManager.CurrentPuzzle.HasUnlimitedMoves)
                {
                    // For unlimited moves, add a stat with 0 value and multiplier of 1
                    summaryStats.Add(new StatSummaryData("Unlimited Moves", 0, 0));
                }
                else
                {
                    // For limited moves, add the remaining moves with the move multiplier
                    int movesLeft = RemainingMoves();
                    summaryStats.Add(new StatSummaryData("Moves Remaining", movesLeft, moveMultiplier));
                }
            }
            
            // Add food collected stat
            int foodCollected = _playerController != null ? _playerController.BrokenBlocks : 0;
            summaryStats.Add(new StatSummaryData("Food Collected", foodCollected, foodMultiplier));
            
            // Add any other stats you want to display
            // summaryStats.Add(new StatSummaryData("Time Bonus", timeBonus, timeMultiplier));
            
            // Initialize and show the level summary UI
            _gameCanvasManager.LevelSummaryUI.gameObject.SetActive(true);
            _gameCanvasManager.LevelSummaryUI.Initialize(summaryStats, ShowChildSelection); 
        }

        /// <summary>
        /// Shows the child selection UI or selects a default child option if the UI is unavailable.
        /// </summary>
        private void ShowChildSelection()
        {
            // Generate the available child options.
            GenerateChildOptions();

            // If the CharacterSelectorUI is unavailable, select the default option.
            if (_gameCanvasManager?.CharacterSelectorUI == null)
            {
                SelectChildOption(0);
                return;
            }

            // Display the CharacterSelectorUI and set up the options.
            _gameCanvasManager.CharacterSelectorUI.gameObject.SetActive(true);
            _gameCanvasManager.CharacterSelectorUI.SetupOptions(_generatedOptions, SelectChildOption);
        }

        /// <summary>
        /// Generates a list of child options with buffs and debuffs based on build type.
        /// </summary>
        private void GenerateChildOptions()
        {
            _generatedOptions.Clear();
            if (Debug.isDebugBuild) GenerateDebugChildOptions();
            else GenerateDefaultChildOptions();
        }

        /// <summary>
        /// Generates random child options with varied buffs and debuffs for debugging.
        /// </summary>
        /// <summary>
        /// Generates random child options with clear distinction for debugging purposes.
        /// </summary>
        private void GenerateDebugChildOptions()
        {
            var mutationManager = GameManager.Instance?.MutationManager;
            if (mutationManager == null) return;

            // Option 1: Only buffs (no debuffs)
            var randomBuff = mutationManager.GetRandomBuffs();
            _generatedOptions.Add(new ChildOption(randomBuff, null));

            // Option 2: Only debuffs (no buffs)
            _generatedOptions.Add(new ChildOption(null, mutationManager.GetRandomDebuffs()));

            // Option 3: Both buffs and debuffs
            var anotherBuff = mutationManager.GetRandomBuffs();
            _generatedOptions.Add(new ChildOption(anotherBuff, mutationManager.GetRandomDebuffs(Random.Range(1, 3))));

            // Shuffle the generated options
            ShuffleOptions(_generatedOptions);
        }
                
        /// <summary>
        /// Generates varied child options for production with random buffs and debuffs.
        /// Ensures each child has exactly one buff and at least one debuff.
        /// </summary>
        private void GenerateDefaultChildOptions()
        {
            var mutationManager = GameManager.Instance?.MutationManager;
            if (mutationManager == null) return;

            // Get all available buffs and debuffs
            var availableBuffs = mutationManager.AvailableBuffs;
            var availableDebuffs = mutationManager.AvailableDebuffs;
            if (availableBuffs.Count == 0 || availableDebuffs.Count == 0) return;
            // Track used combinations to avoid duplicates
            HashSet<string> usedCombinations = new HashSet<string>();

            // Create three distinct child options
            for (int i = 0; i < 3; i++)
            {
                // Get a random buff and wrap it in a list
                BuffSO selectedBuff = availableBuffs[Random.Range(0, availableBuffs.Count)];
                List<BuffSO> buffsList = new List<BuffSO> { selectedBuff };

                // Decide how many debuffs to assign (1 or 2)
                int debuffCount = Random.Range(1, Math.Min(3, availableDebuffs.Count + 1));

                // Select random debuffs without repeating
                List<DebuffSO> selectedDebuffs = new List<DebuffSO>();
                List<DebuffSO> tempDebuffs = new List<DebuffSO>(availableDebuffs);

                for (int j = 0; j < debuffCount; j++)
                {
                    if (tempDebuffs.Count == 0) break;

                    int index = Random.Range(0, tempDebuffs.Count);
                    selectedDebuffs.Add(tempDebuffs[index]);
                    tempDebuffs.RemoveAt(index);
                }

                // Create a unique key for this combination
                string combinationKey = GetEffectsCombinationKey(selectedBuff, selectedDebuffs);

                // If this combination has already been used, retry
                if (usedCombinations.Contains(combinationKey))
                {
                    // Try again with this index
                    i--;
                    continue;
                }

                // Add this combination to our options and track it
                _generatedOptions.Add(new ChildOption(buffsList, selectedDebuffs));
                usedCombinations.Add(combinationKey);
            }

            // Ensure we have 3 options, filling with random ones if needed
            while (_generatedOptions.Count < 3)
            {
                BuffSO randomBuff = availableBuffs[Random.Range(0, availableBuffs.Count)];
                List<BuffSO> buffsList = new List<BuffSO> { randomBuff };
                
                List<DebuffSO> randomDebuffs = new List<DebuffSO> {
                    availableDebuffs[Random.Range(0, availableDebuffs.Count)]
                };

                _generatedOptions.Add(new ChildOption(buffsList, randomDebuffs));
            }

            // Shuffle the options
            ShuffleOptions(_generatedOptions);
        }
        
        /// <summary>
        /// Creates a unique string key for a buff and debuffs combination.
        /// </summary>
        private string GetEffectsCombinationKey(BuffSO buff, List<DebuffSO> debuffs)
        {
            if (buff == null) return "null-buff";
    
            string buffKey = buff.Type.ToString();
    
            if (debuffs == null || debuffs.Count == 0) return buffKey + "-no-debuffs";
    
            // Sort debuffs by type to ensure consistent key generation
            List<DebuffType> types = debuffs.Select(d => d.Type).OrderBy(t => t.ToString()).ToList();
    
            return buffKey + "-" + string.Join("-", types);
        }

        /// <summary>
        /// Shuffles the list of child options randomly.
        /// </summary>
        /// <param name="options">The list of child options to shuffle.</param>
        private void ShuffleOptions(List<ChildOption> options)
        {
            for (var i = options.Count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (options[i], options[j]) = (options[j], options[i]);
            }
        }

        /// <summary>
        /// Selects a child option based on the provided index and applies the selection.
        /// </summary>
        /// <param name="optionIndex">The index of the selected child option.</param>
        public void SelectChildOption(int optionIndex)
        {
            if (_generatedOptions == null || optionIndex < 0 || optionIndex >= _generatedOptions.Count)
                return;

            // Get the selected option
            var selectedOption = _generatedOptions[optionIndex];
            _gameCanvasManager?.CharacterSelectorUI?.gameObject.SetActive(false);

            // Store the selected child option in GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetSelectedChildOption(selectedOption);
        
                // Let GameManager handle level completion now that a child has been selected
                GameManager.Instance.HandleLevelCompleted();
            }
            else LoadSceneManager.Instance.ReloadCurrentScene();
        }
        

        /// <summary>
        /// Spawns the player at the specified entry point and handles first-time play logic.
        /// </summary>
        /// <param name="entryPoint">The entry point where the player should spawn.</param>
        private void SpawnPlayer(EntryPoint entryPoint)
        {
            if (_playerPrefab == null) return;

            // Instantiate the player
            _currentPlayer = Instantiate(_playerPrefab, entryPoint.SpawnPoint.position, entryPoint.SpawnPoint.rotation);
            _playerController = _currentPlayer.GetComponent<PlayerController>();
            _exitPoint?.SetPlayer(_currentPlayer);

            // Explicitly enable player controls
            if (_playerController != null)
            {
                _playerController.enabled = true;
                // If there's a Rigidbody2D, make sure it's not kinematic
                var rb = _currentPlayer.GetComponent<Rigidbody2D>();
                if (rb != null) rb.isKinematic = false;
                // Ensure collider is enabled
                var collider = _currentPlayer.GetComponent<Collider2D>();
                if (collider != null) collider.enabled = true;
            }

            // Check if there is a pending child option to apply
            if (GameManager.Instance != null)
            {
                // Get and clear the selected option
                var childOption = GameManager.Instance.GetAndClearSelectedChildOption();
                if (childOption != null) GameLevelEvents.PlayerSpawned(_currentPlayer.transform, childOption.Buffs, childOption.Debuffs); // Trigger player spawned event with buffs/debuffs
                else GameLevelEvents.PlayerSpawned(_currentPlayer.transform); // Normal player spawned event without buffs/debuffs
            }
            else GameLevelEvents.PlayerSpawned(_currentPlayer.transform); // Normal player spawned event without buffs/debuffs
            SubscribeToPlayerEvents();
            // Set the player as visible
            var renderer = _currentPlayer.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.enabled = true;
                Color color = renderer.color;
                color.a = 1f;  // Full opacity
                renderer.color = color;
            }

            // Update CurrentPowerUI with player's current buff
            UpdatePowerUI();
        }

        /// <summary>
        /// Subscribes to player-related events, such as ability usage.
        /// </summary>
        private void SubscribeToPlayerEvents()
        {
            var playerController = _currentPlayer?.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.OnUseAbility += HandlePlayerAbility;
                playerController.OnPlayerMoved += HandlePlayerMoved;
                playerController.OnAbilityUsed += HandleAbilityUsed;
            }
        }
        
        /// <summary>
        /// Resets the game state when a new level is loaded
        /// </summary>
        private void ResetGameState()
        {
            // Reset the level completed flag
            _levelCompleted = false;
        
            // Clear any temporary values or states
            _generatedOptions.Clear();

            // If game canvas manager exists, reset its state
            if (_gameCanvasManager != null)
            {
                // Reset UI elements if needed
                if (_gameCanvasManager.CharacterSelectorUI != null) _gameCanvasManager.CharacterSelectorUI.gameObject.SetActive(false);
                // Ensure LevelSummaryUI is disabled at the start
                if (_gameCanvasManager.LevelSummaryUI != null) _gameCanvasManager.LevelSummaryUI.gameObject.SetActive(false);
            }
            // Ensure level completing flag is reset
            if (GameManager.Instance != null) GameManager.Instance.ResetLevelCompletion();
        }

        /// <summary>
        /// Unsubscribes from player-related events, such as ability usage.
        /// </summary>
        private void UnsubscribeFromPlayerEvents()
        {
            if (_currentPlayer != null)
            {
                var playerController = _currentPlayer.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerController.OnUseAbility -= HandlePlayerAbility;
                    playerController.OnPlayerMoved -= HandlePlayerMoved;
                    playerController.OnAbilityUsed -= HandleAbilityUsed;
                }
            }
        }

        private void HandlePlayerAbility(Vector2 position, Vector2 direction)
        {
            if (_tilemapManager == null) return;
            if (_currentPlayer == null) return;
            var playerController = _currentPlayer.GetComponent<PlayerController>();
            if (playerController == null) return;
            
            // Convert the player's world position to a grid position
            var playerGridPos = _tilemapManager.WorldToGridPosition(_currentPlayer.transform.position);

            // Calculate the grid direction and the target tile position
            var gridDirection = new Vector3Int(
                Mathf.Clamp(Mathf.RoundToInt(direction.x), -1, 1),
                Mathf.Clamp(Mathf.RoundToInt(direction.y), -1, 1),
                0
            );
            var targetTilePos = playerGridPos + gridDirection;
            // Check if there's an obstacle tile at the target position
            bool hasObstacleTile = false;
            if (_tilemapManager.GetTilemap(CustomTileLayer.Obstacle, CustomTileAlpha.Opaque)?.HasTile(targetTilePos) == true)
            {
                hasObstacleTile = true;
                bool tileWasBroken = HandleBreakTile(targetTilePos);

                if (tileWasBroken)
                {
                    // If we successfully broke a tile, report success and return
                    playerController.ReportAbilitySuccess(position, direction, true);
                    return;
                }
            }

            // Get all active buffs from the player
            var activeBuffs = playerController.GetBuffs();

            if (activeBuffs == null || activeBuffs.Count == 0)
            {
                // Report failure if no tile was broken and no buffs are active
                playerController.ReportAbilitySuccess(position, direction, false);
                return;
            }

            // Process each buff based on type
            bool abilityProcessed = false;
            foreach (var buff in activeBuffs)
            {
                switch (buff.Type)
                {
                    case BuffType.Strength:
                        HandleBuffStrength(position, direction);
                        abilityProcessed = true;
                        break;

                    case BuffType.Jump:
                        HandleBuffJump(position, direction);
                        abilityProcessed = true;
                        break;
                    
                    case BuffType.Explosive:
                        HandleBuffExplosive();
                        abilityProcessed = true;
                        break;
                    
                    case BuffType.Engineer:
                        HandleBuffEngineer();
                        abilityProcessed = true;
                        break;

                    default:
                        Debug.Log($"[LevelManager] Unknown buff type: {buff.Type}");
                        break;
                }
            }
            if (!abilityProcessed) playerController.ReportAbilitySuccess(position, direction, false);
        }

        /// <summary>
        /// Handles breaking a tile if it's of type Breakable and the player has enough force.
        /// </summary>
        /// <param name="position">The grid position of the tile to break.</param>
        /// <returns>True if the tile was successfully broken, otherwise false.</returns>
        private bool HandleBreakTile(Vector3Int position)
        {
            if (_tilemapManager == null) return false;
            if (_playerController == null) return false;
            // Get the tile at the position
            TileBase tileBase = null;
            // Check obstacle tilemap for the tile
            var obstacleTilemap = _tilemapManager.GetTilemap(CustomTileLayer.Obstacle, CustomTileAlpha.Opaque);
            if (obstacleTilemap?.HasTile(position) == true) tileBase = obstacleTilemap.GetTile(position);
            // Check if the tile is of type CustomTile and is breakable
            if (tileBase is CustomTile customTile)
            {
                if (customTile.IsBreakable)
                {
                    // Check if player has enough force to break the tile
                    if (_playerController.BreakForce >= customTile.BreakForceRequired)
                    {
                        //Sonido de caja rompiendose
                        _tilemapManager.PlayBoxBrokeSound();
                        // Call the TilemapManager's BreakTile method to handle the animation
                        _tilemapManager.BreakTile(position);
                
                        // Update player state after breaking a block
                        _playerController.BlockBroken();
                        _gameCanvasManager.StoredFoodUI.SetStoredFood(_playerController.BrokenBlocks);
                        return true;
                    }
                }
            }

            return false;
        }

        public void KillPlayer(DeathReason reason = DeathReason.Unknown)
        {
            // Don't kill the player if the level is already completed
            if (_levelCompleted || (_currentPlayer != null && _currentPlayer.GetComponent<PlayerController>()?.LevelCompleted == true))
            {
                Debug.Log("[LevelManager] Ignoring KillPlayer call - level is already completed");
                return;
            }
            //Reproducir efecto de muerte
            PlayDeathSound();
            // Disable player input immediately to prevent additional actions
            var playerController = _currentPlayer.GetComponent<PlayerController>();
            if (playerController != null)
            {
                Debug.Log($"[LevelManager] Disabling player controller - Death reason: {reason}");
                playerController.enabled = false;

                // Use the sprite manager to change appearance if available
                var spriteManager = _currentPlayer.GetComponent<PlayerSpriteManager>();
                if (spriteManager != null)
                {
                    // Play death animation
                    spriteManager.SetDeathAppearance();
            
                    // Create a sequence for the death animation
                    Sequence deathSequence = DOTween.Sequence();
            
                    // Add a shake effect
                    deathSequence.Append(_currentPlayer.transform.DOShakePosition(0.3f, 0.2f, 10, 90f));
            
                    // Then scale down while rotating
                    deathSequence.Append(_currentPlayer.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack));
                    deathSequence.Join(_currentPlayer.transform.DORotate(new Vector3(0, 0, 180), 0.5f, RotateMode.FastBeyond360));
            
                    // After the animation completes, show the game over panel
                    deathSequence.OnComplete(() => {
                        Debug.Log("[LevelManager] Death animation completed, showing game over panel");
                        _gameCanvasManager.ShowGameOverPanel(reason);
                    });
                }
                else
                {
                    // If no sprite manager is available, show the game over panel directly
                    _gameCanvasManager.ShowGameOverPanel(reason);
                }
            }
            else
            {
                Debug.LogError("[LevelManager] PlayerController component not found on player");
                _gameCanvasManager.ShowGameOverPanel(reason);
            }
        }

        #region  Moves
        /// <summary>
        /// Initializes the puzzle move counter with the maximum number of moves.
        /// </summary>
        /// <param name="amount">The maximum number of moves allowed.</param>
        public void InitializeMoves(int amount)
        {
            if (_gameCanvasManager != null && _gameCanvasManager.PuzzleMovesCounter != null)
            {
                // Get the current puzzle from the tilemap manager
                PuzzleSO currentPuzzle = _tilemapManager?.CurrentPuzzle;
                // Check if we have a valid puzzle and if it uses unlimited moves
                if (currentPuzzle != null && currentPuzzle.HasUnlimitedMoves) _gameCanvasManager.PuzzleMovesCounter.SetUnlimitedMoves();
                else _gameCanvasManager.PuzzleMovesCounter.Initialize(amount);
            }
        }
        
        private int RemainingMoves()
        {
            if (_tilemapManager.CurrentPuzzle.HasUnlimitedMoves)
                return int.MaxValue;
        
            var maxMoves = _tilemapManager.CurrentPuzzle.MaxMoves;
            var currentMoves = _playerController.MovementCounter.GetMoveCount();
            return maxMoves - currentMoves;
        }
        
        private void HandlePlayerMoved(Vector3 newPosition)
        {
            _playerController.MovementCounter.Increment();
            UpdateMoveCounter();

            Vector3Int currentGridPos = _tilemapManager.WorldToGridPosition(newPosition);

            // Check if there are any marked tiles to break after player movement
            List<Vector3Int> markedTiles = _tilemapManager.GetMarkedForDelayedBreaking();
            if (markedTiles != null && markedTiles.Count > 0)
            {
                foreach (Vector3Int tilePos in markedTiles)
                {
                    _tilemapManager.BreakTile(tilePos);
                }
            }

            // Don't check for death conditions if level is already completed or player is marked as completed
            if (_levelCompleted || (_currentPlayer != null && _currentPlayer.GetComponent<PlayerController>()?.LevelCompleted == true))
            {
                Debug.Log("[LevelManager] Skipping death checks - level is already completed");
                return;
            }

            // Check if the player stepped on a deadly void tile at their NEW position
            bool isOnDeadlyVoidTile = _tilemapManager.IsDeadlyVoidTile(currentGridPos);

            if (isOnDeadlyVoidTile)
            {
                KillPlayer(DeathReason.Void);
                return; // No need to process further
            }
            
            bool isCurrentFragile = _tilemapManager.IsFragileTile(currentGridPos);

            if (isCurrentFragile)
            {
                bool hasFeatherBuff = _playerController.HasBuff(BuffType.Feather);

                if (!hasFeatherBuff)
                {
                    _tilemapManager.BreakTile(currentGridPos);
                    KillPlayer(DeathReason.FragileTile); // Player dies but tile remains intact
                }
                else _tilemapManager.MarkTileForDelayedBreaking(currentGridPos);
            }
        }
            
        /// <summary>
        /// Updates the move counter UI based on the current puzzle settings and checks if moves are depleted.
        /// </summary>
        public void UpdateMoveCounter()
        {
            if (_playerController == null || _gameCanvasManager == null || _gameCanvasManager.PuzzleMovesCounter == null) return;
            if (_tilemapManager == null || _tilemapManager.CurrentPuzzle == null) return;
            var currentMoves = _playerController.MovementCounter.GetMoveCount();
            if (_tilemapManager.CurrentPuzzle.HasUnlimitedMoves)
            {
                // For unlimited moves, just show the current count
                //_gameCanvasManager.PuzzleMovesCounter.SetRemainingMoves(currentMoves);
            }
            else
            {
                // For limited moves, show remaining moves
                var maxMoves = _tilemapManager.CurrentPuzzle.MaxMoves;
                var remaining = maxMoves - currentMoves;
                _gameCanvasManager.PuzzleMovesCounter.SetRemainingMoves(remaining);
                
                // Check if player has run out of moves
                if (remaining > 0 || _levelCompleted || (_currentPlayer != null && _currentPlayer.GetComponent<PlayerController>()?.LevelCompleted == true)) return;
                Debug.Log($"[LevelManager] Player out of moves! Max: {maxMoves}, Current: {currentMoves}");
                KillPlayer(DeathReason.Moves);
            }
        }

        #endregion


        /// <summary>
        /// Handles ability usage success or failure.
        /// </summary>
        /// <param name="position">The position where the ability was used.</param>
        /// <param name="direction">The direction of the ability usage.</param>
        /// <param name="success">Whether the ability was successful.</param>
        private void HandleAbilityUsed(Vector2 position, Vector2 direction, bool success)
        {
            //Debug.Log($"[LevelManager] Ability used at {position} in direction {direction}. Success: {success}");
        }

        /// <summary>
        /// Handles the Strength buff ability to push tiles in the specified direction.
        /// </summary>
        /// <param name="position">The position where the ability is used.</param>
        /// <param name="direction">The direction of the ability usage.</param>
        private void HandleBuffStrength(Vector2 position, Vector2 direction)
        {
            if (_tilemapManager == null || _currentPlayer == null || _isBlockMoving) return;

            // Get player controller for reporting success
            var playerController = _currentPlayer.GetComponent<PlayerController>();
            if (playerController == null) return;

            // Convert the player's world position to a grid position
            var playerGridPos = _tilemapManager.WorldToGridPosition(_currentPlayer.transform.position);

            // Calculate the grid direction and the target tile position
            var gridDirection = new Vector3Int(
                Mathf.Clamp(Mathf.RoundToInt(direction.x), -1, 1),
                Mathf.Clamp(Mathf.RoundToInt(direction.y), -1, 1),
                0
            );
            var tileToPush = playerGridPos + gridDirection;

            // Attempt to push the tile in the specified direction
            bool success = false;
            if (_tilemapManager.CanPushTile(tileToPush, gridDirection)) {
                _isBlockMoving = true;
                success = _tilemapManager.PushTile(tileToPush, gridDirection, OnBlockMovementComplete);
            }

            // Report ability success status back to the player controller
            playerController.ReportAbilitySuccess(position, direction, success);
        }
        
        /// <summary>
        /// Callback invoked when block movement animation completes
        /// </summary>
        private void OnBlockMovementComplete()
        {
            _isBlockMoving = false;
        }

        private void HandleBuffJump(Vector2 position, Vector2 direction)
        {
            if (_tilemapManager == null || _currentPlayer == null) return;

            // Get player controller for reporting success
            var playerController = _currentPlayer.GetComponent<PlayerController>();
            if (playerController == null) return;

            // Convert the player's world position to a grid position
            var playerGridPos = _tilemapManager.WorldToGridPosition(_currentPlayer.transform.position);

            // Calculate the grid direction and the target tile position
            var gridDirection = new Vector3Int(
                Mathf.Clamp(Mathf.RoundToInt(direction.x), -1, 1),
                Mathf.Clamp(Mathf.RoundToInt(direction.y), -1, 1),
                0
            );
            var obstaclePos = playerGridPos + gridDirection;
            var landingPos = playerGridPos + (gridDirection * 2);

            // Attempt to jump over the obstacle
            bool success = false;
            if (_tilemapManager.CanJumpTile(obstaclePos, landingPos))
            {
                success = _tilemapManager.JumpTile(playerGridPos, landingPos, playerController);

                if (success)
                {
                    // Increment the move counter
                    _playerController.MovementCounter.Increment();
                    UpdateMoveCounter(); // Use the shared function
                    _playerController.HandleJump();
                }
            }

            // Report ability success status back to the player controller
            playerController.ReportAbilitySuccess(position, direction, success);
        }

        private void HandleBuffExplosive()
        {
            
        }

        private void HandleBuffEngineer()
        {
            
        }

        /// <summary>
        /// Updates the CurrentPowerUI to display the player's current buff
        /// </summary>
        public void UpdatePowerUI()
        {
            if (_playerController == null || _gameCanvasManager == null || _gameCanvasManager.CurrentPowerUI == null)
            {
                Debug.LogWarning("[LevelManager] Cannot update power UI: missing references");
                return;
            }

            // Get the player's active buffs
            var activeBuffs = _playerController.GetBuffs();
            
            // If there are no buffs, clear the UI
            if (activeBuffs == null || activeBuffs.Count == 0)
            {
                _gameCanvasManager.ClearCurrentPowerUI();
                return;
            }

            // Use the first active buff for the UI (assuming one primary buff)
            var primaryBuff = activeBuffs[0];
            _gameCanvasManager.UpdateCurrentPowerUI(primaryBuff);
            
            Debug.Log($"[LevelManager] Updated power UI with buff: {primaryBuff.effectName}");
        }
        private void PlayUISound()
        {
        if (_UISound == null) return;
        _audioSource.spatialBlend = 0f; // 2D
        _audioSource.PlayOneShot(_UISound, _UIVolume);
        }
        private void PlayDeathSound()
        {
        if (_DeathSound == null) return;
        _audioSource.spatialBlend = 0f; // 2D
        _audioSource.PlayOneShot(_DeathSound, _DeathVolume);
        }
    }
}
