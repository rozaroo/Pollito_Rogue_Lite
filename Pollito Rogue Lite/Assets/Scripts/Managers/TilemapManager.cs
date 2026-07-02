using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Enums;
using Managers;
using Scriptables;
using Tilemaps;
using Tiles;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Audio;

public class TilemapManager : MonoBehaviour
{
    [SerializeField] private PuzzleController _puzzleController;
    [SerializeField] private LevelManager _levelManager;
    
    [Header("Tilemap References")]
    [SerializeField] private TilemapReference _floorTilemap; // Tilemap de suelo
    [SerializeField] private TilemapReference _wallOpaqueTilemap; // Paredes sólidas
    [SerializeField] private TilemapReference _wallTransparentTilemap; // Paredes transparentes
    [SerializeField] private TilemapReference _obstacleOpaqueTilemap; // Obstáculos sólidos
    [SerializeField] private TilemapReference _obstacleTransparentTilemap; //// Obstáculos transparentes Agua
    [SerializeField] private TilemapReference _obstacleTransparentWalkableTilemap; // Obstáculos transparentes que se pueden caminar encima
    
    [Header("Specialized Tilemaps")]
    [SerializeField] private TilemapReference _objectivesTilemap; // Tiles de objetivos (spawn, meta)
    [SerializeField] private TilemapReference _pickablesTilemap; // Tiles de items recogibles
    
    [Header("Layer Settings")] // Definición de capas para distintos tipos de tiles
    [SerializeField] private CustomTileLayer _walkableLayers = CustomTileLayer.Floor, _pushableLayers = CustomTileLayer.Obstacle, _dropLayers = CustomTileLayer.Void, _blockedLayers = CustomTileLayer.Wall;

    [Header("Debug Settings")]
    [SerializeField] private bool _enableTileDebug = true;
    [Header("Audio")]
    [SerializeField] private AudioClip _glassCrackSound;
    [SerializeField] private AudioClip _glassBreakingSound;
    [SerializeField] private AudioClip _BoxBrokeSound;
    [SerializeField] private AudioClip _BoxPushSound;
    [SerializeField] private AudioClip _JumpSound;
    [SerializeField] private float _glassCrackVolume = 0.5f;
    [SerializeField] private float _glassBreakingVolume = 0.5f;
    [SerializeField] private float _boxPushVolume = 0.5f;
    [SerializeField] private float _boxBrokeVolume = 0.5f;
    [SerializeField] private float _jumpVolume = 0.5f;
    
    private AudioSource _audioSource;

    private Coroutine _highlightResetCoroutine;
    private bool _isDebugColorsActive;
    private PuzzleSO _currentPuzzle;
    public PuzzleSO CurrentPuzzle => _currentPuzzle;

    // Para controlar los tiles frágiles que se van a romper más tarde
    // HashSet to track tiles marked for delayed breaking
    private readonly HashSet<Vector3Int> _markedFragileTiles = new();
    /// <summary>
    /// Stores the positions of fragile tiles the player has stepped on while the feather buff is active.
    /// </summary>
    private readonly HashSet<Vector3Int> _delayedBreakTiles = new();

    private void Awake()
    {
        _currentPuzzle = _puzzleController.CurrentPuzzle;
        // Puedes usar un AudioSource global o crear uno si no existe
        if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
    }
    
    private void Start()
    {
        // Try to find missing references
        if (_puzzleController == null)
        {
            Debug.LogWarning("[TilemapManager] PuzzleController reference missing, attempting to find one...");
            _puzzleController = FindObjectOfType<PuzzleController>();
        }
    }
    
    /// <summary>
    /// Inicializa los objetivos del puzzle (spawn y meta)
    /// </summary>
    public void InitializePuzzleGoals()
    {
        if (_objectivesTilemap?.Tilemap == null) return;
        if (_puzzleController == null) return;
        // Find the first spawn and goal tiles in the objectives tilemap
        Vector3Int? spawnTilePos = null;
        Vector3Int? goalTilePos = null;
        // Buscar tiles de spawn y meta
        foreach (var position in _objectivesTilemap.Tilemap.cellBounds.allPositionsWithin)
        {
            if (!_objectivesTilemap.Tilemap.HasTile(position)) continue;
            var tile = _objectivesTilemap.Tilemap.GetTile(position) as CustomTile;
            if (tile == null) 
            {
                Debug.Log($"[TilemapManager] Tile at {position} is not a CustomTile");
                continue;
            }
            if (tile.ObjectiveType == ObjectiveType.Spawn && !spawnTilePos.HasValue) spawnTilePos = position;
            else if (tile.ObjectiveType == ObjectiveType.Goal && !goalTilePos.HasValue) goalTilePos = position;
        }
        // Colocar el spawn y meta en el mundo
        if (spawnTilePos.HasValue)
        {
            Vector3 worldPos = _objectivesTilemap.Tilemap.GetCellCenterWorld(spawnTilePos.Value);
            _puzzleController.SpawnPoint.transform.position = worldPos;
        }

        if (!goalTilePos.HasValue) return;
        {
            var worldPos = _objectivesTilemap.Tilemap.GetCellCenterWorld(goalTilePos.Value);
            _puzzleController.Goal.transform.position = worldPos;
        }
    }
    // Determina si una posición es un tile de void mortal
    public bool IsDeadlyVoidTile(Vector3Int position)
    {
        bool hasVoidTile = false;

        // Revisar si el tile de piso es de tipo void
        if (_floorTilemap?.Tilemap != null && _floorTilemap.Tilemap.HasTile(position))
        {
            TileBase floorTile = _floorTilemap.Tilemap.GetTile(position);
            // Log detailed information about the found tile
            if (floorTile != null)
            {
                // Check if the name contains "void" (case-insensitive)
                bool isVoidByName = floorTile.name.ToLower().Contains("void");
                if (isVoidByName) hasVoidTile = true;
                // Additional check for specific tile types if the default name check fails
                if (!hasVoidTile && floorTile is CustomTile customTile)
                {
                    // Check if the tile's layer is Void
                    if (customTile.Layer == CustomTileLayer.Void) hasVoidTile = true;
                }
            }
        }
        
        foreach (var reference in GetAllTilemapReferences())
        {
            if (reference?.Tilemap != null)
            {
                bool hasTile = reference.Tilemap.HasTile(position);
                if (hasTile) { TileBase tile = reference.Tilemap.GetTile(position); }
            }
        }
        // Si no hay void, retornar falso
        if (!hasVoidTile) return false;
        // Rest of the method remains the same - checking for other tiles at this position
        // Check if there are objectives, obstacles, or pickables tiles at this position
        // If any of these exist, the void is covered and player is safe

        // Verificar si hay tiles que cubren el void
        if (_objectivesTilemap?.Tilemap != null && _objectivesTilemap.Tilemap.HasTile(position)) return false;
        // Check obstacles (both opaque and transparent)
        if (_obstacleOpaqueTilemap?.Tilemap != null && _obstacleOpaqueTilemap.Tilemap.HasTile(position)) return false;
        if (_obstacleTransparentTilemap?.Tilemap != null && _obstacleTransparentTilemap.Tilemap.HasTile(position)) return false;
        if (_obstacleTransparentWalkableTilemap?.Tilemap != null && _obstacleTransparentWalkableTilemap.Tilemap.HasTile(position))
        {
            TileBase walkableTile = _obstacleTransparentWalkableTilemap.Tilemap.GetTile(position);
            if (walkableTile != null && !walkableTile.name.Contains("RT_Floor_Void")) return false;
        }
        // Check pickables
        if (_pickablesTilemap?.Tilemap != null && _pickablesTilemap.Tilemap.HasTile(position)) return false;
        // Check walls too for completeness
        if (_wallOpaqueTilemap?.Tilemap != null && _wallOpaqueTilemap.Tilemap.HasTile(position)) return false;
        if (_wallTransparentTilemap?.Tilemap != null && _wallTransparentTilemap.Tilemap.HasTile(position)) return false;
        return true;
    }
    
    /// <summary>
    /// Determines whether the player can jump over an obstacle tile to the landing position.
    /// Verifica si se puede saltar un tile de obstáculo
    /// </summary>
    /// <param name="obstaclePos">The grid position of the obstacle tile.</param>
    /// <param name="landingPos">The grid position where the player would land.</param>
    /// <returns>True if the jump is possible; otherwise, false.</returns>
    public bool CanJumpTile(Vector3Int obstaclePos, Vector3Int landingPos)
    {
        // Check if there's an obstacle at the obstacle position
        var obstacleTilemap = GetTilemap(CustomTileLayer.Void, CustomTileAlpha.Transparent);
        TileBase tileBase = obstacleTilemap?.GetTile(obstaclePos);
    
        // Check if the tile is a void-type CustomTile that can be jumped over
        bool isVoidObstacle = false;
        if (tileBase is CustomTile customTile)
        {
            // Check if the tile is of type "void" (add this property to CustomTile if needed)
            // For now, assuming you might have a property or can check the sprite name
            isVoidObstacle = customTile.Layer == CustomTileLayer.Void;

        }
        if (!isVoidObstacle) return false;
        // Check if landing position is in bounds
        if (!IsPositionInBounds(landingPos)) return false;
        // Check if landing position is free of obstacles and walls
        bool landingClear = IsCellClear(landingPos);
        return landingClear;
    }
    
    /// <summary>
    /// Realiza el salto de un jugador de startPos a landingPos
    /// </summary>
    /// <param name="startPos">The starting grid position of the player.</param>
    /// <param name="landingPos">The grid position where the player will land.</param>
    /// <param name="playerController">The player controller to update.</param>
    /// <returns>True if the jump was successful; otherwise, false.</returns>
    public bool JumpTile(Vector3Int startPos, Vector3Int landingPos, PlayerController playerController)
    {
        // Convert grid positions to world positions
        Vector3 startWorldPos = GridToWorldPosition(startPos);
        Vector3 targetWorldPos = GridToWorldPosition(landingPos);
        
        // Create a midpoint with a less pronounced arc
        Vector3 midPointWorldPos = (startWorldPos + targetWorldPos) * 0.5f + Vector3.up * 0.5f;
        
        // Disable player input during the jump animation
        playerController.SetInputEnabled(false);
        
        // Create a path for the jump with a subtle arc
        Vector3[] path = { startWorldPos, midPointWorldPos, targetWorldPos };
        //Añadir aca efecto de saltar
        PlayJumpSound();
        // Animate the player along the path
        playerController.transform.DOPath(path, 0.4f, PathType.CatmullRom)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                // Use UNCORRECTED position for debugging
                playerController.UpdatePositionAfterJump(targetWorldPos);
                
                // Re-enable input when animation completes
                playerController.SetInputEnabled(true);
            });

        return true;
    }
    
    // Funciones auxiliares
    /// <summary>
    /// Checks if a grid position is within the bounds of the tilemap.
    /// </summary>
    /// <param name="position">The grid position to check.</param>
    /// <returns>True if the position is within bounds; otherwise, false.</returns>
    private bool IsPositionInBounds(Vector3Int position)
    {
        if (_floorTilemap?.Tilemap == null) return false;
        var bounds = _floorTilemap.Tilemap.cellBounds;
        return bounds.Contains(position);
    }

    /// <summary>
    /// Converts a grid position to a world position.
    /// </summary>
    /// <param name="gridPosition">The grid position to convert.</param>
    /// <returns>The corresponding world position.</returns>
    public Vector3 GridToWorldPosition(Vector3Int gridPosition)
    {
        if (_floorTilemap?.Tilemap == null) return new Vector3(gridPosition.x, gridPosition.y, 0);
        return _floorTilemap.Tilemap.GetCellCenterWorld(gridPosition);
    }

    /// <summary>
    /// Retrieves the GetTilemapManager associated with the specified custom tile layer and alpha transparency.
    /// </summary>
    /// <param name="layer">The custom tile layer to determine the tilemap reference.</param>
    /// <param name="alpha">The alpha transparency level of the tile layer.</param>
    /// <returns>
    /// The GetTilemapManager corresponding to the given layer and alpha, or null if no match is found.
    /// </returns>
    private TilemapReference GetTilemapReferenceFromCustomLayers(CustomTileLayer layer, CustomTileAlpha alpha)
    {
        var reference = layer switch
        {
            CustomTileLayer.Floor => _floorTilemap,
            CustomTileLayer.Wall when alpha == CustomTileAlpha.Opaque => _wallOpaqueTilemap,
            CustomTileLayer.Wall => _wallTransparentTilemap,
            CustomTileLayer.Obstacle when alpha == CustomTileAlpha.Opaque => _obstacleOpaqueTilemap,
            CustomTileLayer.Obstacle => _obstacleTransparentTilemap,
            CustomTileLayer.Objective => _objectivesTilemap,
            CustomTileLayer.Pickable => _pickablesTilemap,
            CustomTileLayer.Void => _obstacleTransparentTilemap,
            _ => null
        };
    
        if (reference == null || reference.Tilemap == null) Debug.LogError($"[TilemapManager] No tilemap reference found for layer={layer}, alpha={alpha}");
        return reference;
    }

    /// <summary>
    /// Retrieves the Tilemap associated with the specified custom tile layer and alpha transparency.
    /// </summary>
    /// <param name="layer">The custom tile layer to determine the tilemap.</param>
    /// <param name="alpha">The alpha transparency level of the tile layer.</param>
    /// <returns>
    /// The Tilemap corresponding to the given layer and alpha, or null if no match is found.
    /// </returns>
    public Tilemap GetTilemap(CustomTileLayer layer, CustomTileAlpha alpha) =>
        GetTilemapReferenceFromCustomLayers(layer, alpha)?.Tilemap;

    /// <summary>
    /// Retrieves all TilemapReferences used in the TilemapManager.
    /// </summary>
    /// <returns>
    /// An enumerable collection of TilemapReferences, including all tilemaps (floor, wall, obstacle, objectives, extras)
    /// </returns>
    public IEnumerable<TilemapReference> GetAllTilemapReferences() =>
        new[] { 
            _floorTilemap, 
            _wallOpaqueTilemap, 
            _wallTransparentTilemap, 
            _obstacleOpaqueTilemap, 
            _obstacleTransparentTilemap,
            _obstacleTransparentWalkableTilemap,
            _objectivesTilemap,
            _pickablesTilemap
        };
    
    /// <summary>
    /// Creates a new instance of a CustomTile and initializes it with the provided tile data.
    /// </summary>
    /// <param name="data">The data used to configure the properties of the CustomTile.</param>
    /// <returns>
    /// A new instance of CustomTile configured with the specified data.
    /// </returns>
    private CustomTile CreateTile(CustomTileData data)
    {
        CustomTile tile = ScriptableObject.CreateInstance<CustomTile>();

        #if UNITY_EDITOR
        if (!string.IsNullOrEmpty(data.spriteReference))
        {
            var spriteAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(data.spriteReference);
            if (spriteAsset != null)
            {
                var sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(data.spriteReference).OfType<Sprite>().ToArray();
                tile.sprite = sprites.FirstOrDefault(s => s.name == data.spriteName) ?? sprites.FirstOrDefault();
            }
        }
        #endif

        tile.color = data.color;
        tile.flags = data.flags;
        tile.colliderType = data.colliderType;
        tile.transform = data.transformMatrix;
        tile.Layer = data.layer;
        tile.Alpha = data.alpha;
        tile.IsFragile = data.isFragile;
        tile.IsMovable = data.isMovable;
        tile.ObjectiveType = data.objectiveType;
        tile.IsBreakable = data.isBreakable;
        tile.BreakForceRequired = data.breakForceRequired;
        return tile;
    }

    public void SetCurrentPuzzle(PuzzleSO puzzle)
    {
        _currentPuzzle = puzzle;
    }
    
    /// <summary>
    /// Empujar caja
    /// </summary>
    /// <param name="position">The grid position of the tile to push.</param>
    /// <param name="direction">The direction in which to push the tile.</param>
    /// <returns>
    /// True if the tile was successfully pushed to the target position; otherwise, false.
    /// </returns>
    public bool PushTile(Vector3Int position, Vector3Int direction, Action onComplete = null)
    {
        // Check if the tile can be pushed in the specified direction.
        if (!CanPushTile(position, direction)) return false;

        TileBase tileToPush = null;
        Tilemap sourceTilemap = null;

        // Determine the tile and its source tilemap based on the position.
        if (_obstacleOpaqueTilemap?.Tilemap?.HasTile(position) == true)
        {
            tileToPush = _obstacleOpaqueTilemap.Tilemap.GetTile(position);
            sourceTilemap = _obstacleOpaqueTilemap.Tilemap;
        }
        else if (_obstacleTransparentTilemap?.Tilemap?.HasTile(position) == true)
        {
            tileToPush = _obstacleTransparentTilemap.Tilemap.GetTile(position);
            sourceTilemap = _obstacleTransparentTilemap.Tilemap;
        }

        // If no tile or source tilemap is found, return false.
        if (tileToPush == null || sourceTilemap == null) return false;
        // Calculate the target position
        Vector3Int targetPosition = position + direction;
        // Create a temporary GameObject with a SpriteRenderer for animation
        GameObject tileObj = CreatePushableTileSprite(sourceTilemap, position);
        if (tileObj == null) return false;
        // Disable player input during animation
        if (_levelManager != null && _levelManager.Player != null) _levelManager.Player.SetInputEnabled(false);
        // Remove the tile from its original position immediately
        sourceTilemap.SetTile(position, null);

        // Calculate world positions for animation
        Vector3 targetWorldPos = GridToWorldPosition(targetPosition);
        //Añadir acá efecto de sonido de empujar
        PlayBoxPushingSound();
        // Animate the tile movement
        tileObj.transform.DOMove(targetWorldPos, 0.2f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                // Set the tile at the target position
                sourceTilemap.SetTile(targetPosition, tileToPush);
                // Destroy the temporary visual object
                Destroy(tileObj);
                // Re-enable player input after animation completes
                if (_levelManager != null && _levelManager.Player != null) _levelManager.Player.SetInputEnabled(true);
                // Call the callback if provided
                onComplete?.Invoke();
            });

        return true;
    }
    
    /// <summary>
    /// Creates a sprite object representing a pushable tile for animation.
    /// </summary>
    private GameObject CreatePushableTileSprite(Tilemap tilemap, Vector3Int position)
    {
        // Create a temporary sprite for the animation
        GameObject tileObj = new GameObject("PushableTile");
        SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();
        // Get the sprite from the tile
        Sprite tileSprite = tilemap.GetSprite(position);
        if (tileSprite == null)
        {
            // Try to get a sprite from the tile directly
            if (tilemap.GetTile(position) is CustomTile customTile) tileSprite = customTile.sprite;
        }

        sr.sprite = tileSprite;
    
        // Position the sprite at the tile's world position
        Vector3 tileWorldPos = tilemap.GetCellCenterWorld(position);
        tileObj.transform.position = tileWorldPos;

        // Set sorting layer to match the tilemap
        TilemapRenderer renderer = tilemap.GetComponent<TilemapRenderer>();
        if (renderer != null)
        {
            sr.sortingLayerID = renderer.sortingLayerID;
            sr.sortingOrder = renderer.sortingOrder + 1;
        }

        return tileObj;
    }

    private bool IsCellClear(Vector3Int position) =>
        !_wallOpaqueTilemap?.Tilemap?.HasTile(position) == true &&
        !_wallTransparentTilemap?.Tilemap?.HasTile(position) == true &&
        !_obstacleOpaqueTilemap?.Tilemap?.HasTile(position) == true &&
        !_obstacleTransparentTilemap?.Tilemap?.HasTile(position) == true &&
        _floorTilemap?.Tilemap?.HasTile(position) == true;

    /// <summary>
    /// Determines whether a tile at the specified position can be pushed in the given direction.
    /// </summary>
    /// <param name="position">The grid position of the tile to check.</param>
    /// <param name="direction">The direction in which the tile is intended to be pushed.</param>
    /// <returns>
    /// True if the tile can be pushed to the target position; otherwise, false.
    /// </returns>
    public bool CanPushTile(Vector3Int position, Vector3Int direction)
    {
        // Retrieve the tile at the specified position from the obstacle tilemaps.
        TileBase tileBase = _obstacleOpaqueTilemap?.Tilemap?.GetTile(position) ?? _obstacleTransparentTilemap?.Tilemap?.GetTile(position);

        // Check if the tile is a movable CustomTile; if not, return false.
        if (tileBase is not CustomTile customTile || !customTile.IsMovable) return false;

        // Calculate the target position and check if the cell is clear.
        Vector3Int targetPosition = position + direction;
        return IsCellClear(targetPosition);
    }
    
    /// <summary>
    /// Returns all tile positions that are marked for delayed breaking.
    /// </summary>
    /// <returns>A list of grid positions that are marked for delayed breaking.</returns>
    public List<Vector3Int> GetMarkedForDelayedBreaking()
    {
        return new List<Vector3Int>(_markedFragileTiles);
    }
    
    /// <summary>
    /// Manejo de Tiles Fragiles
    /// </summary>
    public bool IsFragileTile(Vector3Int position)
    {
        // First, let's log all tilemaps and their tile contents
        foreach (var reference in GetAllTilemapReferences())
        {
            if (reference?.Tilemap == null) continue;
            // Get bounds of the tilemap
            BoundsInt bounds = reference.Tilemap.cellBounds;
            // Count tiles in this tilemap
            int tileCount = 0;
            List<Vector3Int> fragileTilePositions = new List<Vector3Int>();
            
            // Check all positions within bounds
            foreach (Vector3Int pos in bounds.allPositionsWithin)
            {
                if (reference.Tilemap.HasTile(pos))
                {
                    tileCount++;
                    TileBase tile = reference.Tilemap.GetTile(pos);
                    if (tile is CustomTile customTile && customTile.IsFragile) fragileTilePositions.Add(pos);
                }
            }
            
            // Check specifically for the requested position
            if (reference.Tilemap.HasTile(position))
            {
                TileBase tile = reference.Tilemap.GetTile(position);
                if (tile is CustomTile customTile)
                {
                    if (customTile.IsFragile) return true;   
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Marks a fragile tile for delayed breaking when player moves off it.
    /// Used with Feather buff.
    /// </summary>
    public void MarkTileForDelayedBreaking(Vector3Int position)
    {
        if (IsFragileTile(position))
        {
            // Activar animación de agitar
            ActivateFragileTile(position);
            // Marcar para romper después
            _markedFragileTiles.Add(position);
        }
    }
    
    /// <summary>
    /// Activates a visual effect on a fragile tile (small shake) to indicate it's about to break.
    /// </summary>
    /// <param name="position">The grid position of the fragile tile to activate.</param>
    public void ActivateFragileTile(Vector3Int position)
    {
        Debug.Log($"[TilemapManager] Activating fragile tile at position {position}");

        // Check if the position has a fragile tile
        if (!IsFragileTile(position))
        {
            Debug.LogWarning($"[TilemapManager] No fragile tile found at position {position}");
            return;
        }

        foreach (var reference in GetAllTilemapReferences())
        {
            if (reference?.Tilemap == null) continue;

            if (!reference.Tilemap.HasTile(position)) continue;

            TileBase tile = reference.Tilemap.GetTile(position);
            if (tile is CustomTile customTile && customTile.IsFragile)
            {
                // Reemplazar tile por activo y reproducir sonido/animación
                // Get the active fragile tile from the GameManager
                CustomTile activeTile = GameManager.Instance?.SpecialTilesManager?.GetFragileActiveTile();

                if (activeTile == null)
                {
                    Debug.LogError("[TilemapManager] Failed to get fragile active tile from GameManager");

                    // Fall back to animation only if asset not found
                    GameObject animationTileObj = CreateShakingTileSprite(reference.Tilemap, position);
                    PlayFragileTileShakeAnimation(animationTileObj, position);
                    return;
                }

                // Replace the tile with the active version
                reference.Tilemap.SetTile(position, activeTile);
                PlayGlassCrackSound(); //Aca reproducir sonido de vidrio roto
                // Also play a subtle shake animation to draw attention
                GameObject shakeTileObj = CreateShakingTileSprite(reference.Tilemap, position);
                PlayFragileTileShakeAnimation(shakeTileObj, position);
                Debug.Log($"[TilemapManager] Fragile tile at {position} replaced with active version");
                return;
            }
        }
    }

    /// <summary>
    /// Creates a sprite object at a tile's position for shake animation while keeping the original tile.
    /// </summary>
    private GameObject CreateShakingTileSprite(Tilemap tilemap, Vector3Int position)
    {
        // Create a temporary sprite as a visual overlay
        GameObject tileObj = new GameObject("ShakingTile");
        SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();

        // Get the sprite from the tile
        Sprite tileSprite = tilemap.GetSprite(position);

        if (tileSprite == null)
        {
            // Try to get a sprite from the tile directly
            if (tilemap.GetTile(position) is CustomTile customTile) tileSprite = customTile.sprite;
        }

        sr.sprite = tileSprite;

        // Position the sprite at the tile's world position
        Vector3 tileWorldPos = tilemap.GetCellCenterWorld(position);
        tileObj.transform.position = tileWorldPos;

        // Set sorting layer to match the tilemap
        TilemapRenderer renderer = tilemap.GetComponent<TilemapRenderer>();
        if (renderer != null)
        {
            sr.sortingLayerID = renderer.sortingLayerID;
            // Place it slightly in front of the original tile
            sr.sortingOrder = renderer.sortingOrder + 1;
        }

        return tileObj;
    }

    /// <summary>
    /// Plays a subtle shake animation for fragile tiles that are being activated.
    /// </summary>
    private void PlayFragileTileShakeAnimation(GameObject tileObj, Vector3Int position)
    {
        if (tileObj == null) return;
        Debug.Log($"[TilemapManager] Playing fragile tile shake animation at {position}");

        // Create a sequence for a subtle shake effect
        Sequence shakeSequence = DOTween.Sequence();

        // First do a subtle quick shake
        shakeSequence.Append(tileObj.transform.DOShakePosition(0.2f, 0.1f, 10, 90f));

        // Add a small pulse effect
        shakeSequence.Append(tileObj.transform.DOScale(1.1f, 0.1f).SetEase(Ease.OutQuad));
        shakeSequence.Append(tileObj.transform.DOScale(1f, 0.1f).SetEase(Ease.InQuad));

        // Destroy the visual object after animation completes
        shakeSequence.OnComplete(() => {
            Debug.Log($"[TilemapManager] Fragile tile shake animation complete at {position}");
            Destroy(tileObj);
        });
    }

    /// <summary>
    /// Checks if a tile position is marked for delayed breaking.
    /// </summary>
    public bool IsMarkedForDelayedBreaking(Vector3Int position)
    {
        bool isMarked = _markedFragileTiles.Contains(position);
        return isMarked;
    }
    
    /// <summary>
    /// Breaks a tile at the specified grid position with a scaling animation before removing it.
    /// </summary>
    /// <param name="position">The grid position of the tile to break.</param>
    public void BreakTile(Vector3Int position)
    {
        // Remove from marked tiles if present
        if (_markedFragileTiles.Contains(position)) _markedFragileTiles.Remove(position);
        bool tileFound = false;
        PlayGlassBrokeSound();
        foreach (var reference in GetAllTilemapReferences())
        {
            if (reference?.Tilemap == null) continue;
            if (!reference.Tilemap.HasTile(position)) continue;
            TileBase tile = reference.Tilemap.GetTile(position);
            if (tile is CustomTile customTile)
            {
                // Check if the tile is breakable (either fragile or normal breakable)
                if (customTile.IsFragile || customTile.IsBreakable)
                {
                    tileFound = true;
                    
                    // Create animated sprite for the breaking effect before removing the tile
                    GameObject tileObj = CreateBreakingTileSprite(reference.Tilemap, position);
                    
                    // Remove the tile from the tilemap
                    reference.Tilemap.SetTile(position, null);
                    // Apply the appropriate breaking animation
                    if (customTile.IsFragile) PlayFragileTileBreakAnimation(tileObj);
                    else if (customTile.IsBreakable) PlayBreakableTileBreakAnimation(tileObj);
                    break;
                }
            }
        }
    }
    
    /// <summary>
    /// Creates a sprite object at a tile's position for break animations.
    /// </summary>
    private GameObject CreateBreakingTileSprite(Tilemap tilemap, Vector3Int position)
    {
        // Create a temporary sprite at the tile's position for the animation
        GameObject tileObj = new GameObject("BreakingTile");
        SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();

        // Get the sprite from the tile
        Sprite tileSprite = tilemap.GetSprite(position);
        
        if (tileSprite == null)
        {
            // Try to get a sprite from the tile directly
            if (tilemap.GetTile(position) is CustomTile customTile) tileSprite = customTile.sprite;
        }
        
        sr.sprite = tileSprite;
        
        // Position the sprite at the tile's world position
        Vector3 tileWorldPos = tilemap.GetCellCenterWorld(position);
        tileObj.transform.position = tileWorldPos;

        // Set sorting layer to match the tilemap
        TilemapRenderer renderer = tilemap.GetComponent<TilemapRenderer>();
        if (renderer != null)
        {
            sr.sortingLayerID = renderer.sortingLayerID;
            sr.sortingOrder = renderer.sortingOrder + 1;
        }

        return tileObj;
    }

    /// <summary>
    /// Plays the breaking animation for fragile tiles.
    /// </summary>
    private void PlayFragileTileBreakAnimation(GameObject tileObj)
    {
        if (tileObj == null) return;
        // Make sure the scale is normalized before animating
        tileObj.transform.localScale = Vector3.one;
        // Create a sequence for a more abrupt fragile tile break
        Sequence fragileBreakSequence = DOTween.Sequence();
        // First a quick flash/pulse (grow slightly and return to normal in 0.1s)
        fragileBreakSequence.Append(tileObj.transform.DOScale(1.2f, 0.05f));
        fragileBreakSequence.Append(tileObj.transform.DOScale(1f, 0.05f));
        // Then immediately shrink to zero (very fast - 0.15s)
        fragileBreakSequence.Append(tileObj.transform.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack));
        // Destroy the object when animation completes
        fragileBreakSequence.OnComplete(() => {
            Destroy(tileObj);
        });
    }

    /// <summary>
    /// Plays the breaking animation for standard breakable tiles (faster version).
    /// </summary>
    private void PlayBreakableTileBreakAnimation(GameObject tileObj)
    {
        if (tileObj == null) return;
        // Make sure the scale is normalized before animating
        tileObj.transform.localScale = Vector3.one;

        // Create a sequence for breakable tiles with faster animation
        Sequence breakSequence = DOTween.Sequence();

        // First shake the tile (faster - 0.15s instead of 0.3s)
        breakSequence.Append(tileObj.transform.DOShakePosition(0.15f, 0.2f, 10, 90f));

        // Then scale it down while rotating (faster - 0.15s instead of 0.3s)
        breakSequence.Append(tileObj.transform.DOScale(Vector3.zero, 0.15f).SetEase(Ease.OutQuad));
        breakSequence.Join(tileObj.transform.DORotate(new Vector3(0, 0, 180), 0.15f, RotateMode.FastBeyond360));

        // Destroy the object when animation completes
        breakSequence.OnComplete(() => {
            Destroy(tileObj);
        });
    }
    
    public void ExplodeBomb(Vector3Int center) 
    {
        int radius = 1; // puedes hacerlo configurable

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector3Int pos = new Vector3Int(center.x + x, center.y + y, center.z);

                // Busca solo en wallTransparent
                if (_wallTransparentTilemap?.Tilemap?.HasTile(pos) == true)
                {
                    _wallTransparentTilemap.Tilemap.SetTile(pos, null);
                    Debug.Log($"[TilemapManager] WallTransparent destruido en {pos}");
                }
            }
        }
    }

    private void PlayGlassCrackSound()
    {
        if (_glassCrackSound == null)
        {
            Debug.LogWarning("[TilemapManager] No glass crack sound assigned.");
            return;
        }
        _audioSource.spatialBlend = 0f; // 2D
        _audioSource.PlayOneShot(_glassCrackSound, _glassCrackVolume);
    }
    private void PlayGlassBrokeSound()
    {
        if (_glassBreakingSound == null)
        {
            Debug.LogWarning("[TilemapManager] No glass break sound assigned.");
            return;
        }
        _audioSource.spatialBlend = 0f; // 2D
        _audioSource.PlayOneShot(_glassBreakingSound, _glassBreakingVolume);
    }
    private void PlayBoxPushingSound()
    {
        if (_BoxPushSound == null)
        {
            Debug.LogWarning("[TilemapManager] No box push sound assigned.");
            return;
        }
        _audioSource.spatialBlend = 0f; // 2D
        _audioSource.PlayOneShot(_BoxPushSound, _boxPushVolume);
    }
    public void PlayBoxBrokeSound()
    {
        if (_BoxBrokeSound == null)
        {
            Debug.LogWarning("[TilemapManager] No box broke sound assigned.");
            return;
        }
        _audioSource.spatialBlend = 0f; // 2D
        _audioSource.PlayOneShot(_BoxBrokeSound, _boxBrokeVolume);
    }
    private void PlayJumpSound()
    {
        if (_JumpSound == null)
        {
            Debug.LogWarning("[TilemapManager] No jump sound assigned.");
            return;
        }
        _audioSource.spatialBlend = 0f; // 2D
        _audioSource.PlayOneShot(_JumpSound, _jumpVolume);
    }
    public Vector3 CellToWorld(Vector3Int cellPos)
    {
        return _floorTilemap.Tilemap.CellToWorld(cellPos);
    }
    public Vector3Int WorldToCell(Vector3 worldPosition)
    {
        return _floorTilemap.Tilemap.WorldToCell(worldPosition);
    }
    public Vector3 GetCellCenterWorld(Vector3Int cellPosition)
    {
        return _floorTilemap.Tilemap.GetCellCenterWorld(cellPosition);
    }
    public CustomTile GetTileAtPosition(Vector3Int cellPos)
    {
        return _floorTilemap.Tilemap.GetTile(cellPos) as CustomTile;
    }
    /// <summary>
    /// Converts a world position to a grid position within the tilemap.
    /// </summary>
    /// <param name="worldPosition">The world position to convert.</param>
    /// <returns>
    /// The corresponding grid position as a Vector3Int. If the floor tilemap is not available,
    /// the world position is rounded to the nearest integer values.
    /// </returns>
    public Vector3Int WorldToGridPosition(Vector3 worldPosition) =>
        _floorTilemap?.Tilemap?.WorldToCell(worldPosition) ?? new Vector3Int(Mathf.RoundToInt(worldPosition.x), Mathf.RoundToInt(worldPosition.y), 0);
}