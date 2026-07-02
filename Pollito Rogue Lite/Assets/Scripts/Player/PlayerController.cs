using Audio; // Add Audio namespace
using DG.Tweening;
using Enums;
using Managers;
using Player;
using Scriptables;
using Scriptables.Abilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tiles;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    //MOVIMIENTO EN GRILLA
    [Header("Grid Movement Parameters")]
    [SerializeField] private float gridSize = 1f; //Tamaño de cada celda de la grilla
    [SerializeField] private float moveSpeed = 5f; //Velocidad de movimiento normal
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private LayerMask collisionLayer; //Qué capas bloquean el movimiento
    [SerializeField] private LayerMask obstaclesInteractionLayer; //Qué capas son obstaculos interactuables
    [SerializeField] private float dashSpeedMultiplier = 1.5f; //Multiplicador de velocidad al hacer dash
    //ESTADO DEL JUGADOR
    [Header("Player State")]
    [SerializeField] private List<BuffSO> _currentBuffs; //Buffs activos
    [SerializeField] private List<DebuffSO> _currentDebuffs; //Debuffs activos
    [SerializeField] private int _brokenBlocks = 0; //Recursos para dashes
    [SerializeField] private bool isDashing = false; //Si esta haciendo dash
    [SerializeField] public bool _isControlsInverted = false; //Si los controles estan invertidos
    [SerializeField] private bool isMoving = false; //Si se esta moviendo actualmente
    [SerializeField] private Vector2 moveDirection; //Direccion de movimiento
    [SerializeField] private Vector3 _previousPosition; //Posicion anterior (para revertir si hace falta)
    [SerializeField] private bool _inputEnabled = true; //Si el jugador puede mover al PJ
    public bool debuff;
    public bool explosive;
    public bool jumpbuff;
    private int bombsAvailable = 5; // Cantidad de bombas disponibles
    //COMPONENTES DEL PLAYER
    [Header("Player Components")]
    [SerializeField] private PlayerEffectManager _playerEffectManager; //Maneja efectos graficos
    [SerializeField] private MovementCounter _movementCounter; //Contador de movimientos
    [SerializeField] private GameObject _globalLights; //Luces globales (cuando no hay ceguera)
    [SerializeField] private GameObject _blindnessLights; //Luces especiales para debuff de ceguera
    [SerializeField] private GameObject _bomb; //Prefab de la bomba
    [SerializeField] private PlayerAudioController _playerAudioController; // Maneja sonidos del player
    //HABILIDADES
    [Header("Player Abilities")]
    [SerializeField] public int BreakForce = 1; //Fuerza para romper bloques
    [SerializeField] private SpecialAbilitySO _dashAbility; //Configuracion del dash
    //EFECTOS DE FEEDBACK
    [Header("Feedback Effects")] 
    [SerializeField] private GameObject _spriteRenderer; //El sprite del PJ
    [SerializeField] private float _cantDashShakeDuration = 0.3f; //Tiempo del shake
    [SerializeField] private float _cantDashShakeStrength = 0.2f; //Fuerza del shake
    [SerializeField] private int _cantDashShakeVibrato = 10; //Vibraciones por segundo
    [SerializeField] private float _cantDashShakeRandomness = 90f; //Aleatoriedad del shake
    [SerializeField] private bool _cantDashShakeFadeOut = true; //Si el shake se suaviza al terminar
    
    private Sequence _feedbackSequence; //Secuencia DOTween para feedbacks
    private bool _isFeedbackPlaying = false; //Evita reproducir feedback duplicado

    // VARIABLES DE MOVIMIENTO
    private int CurrentGridX; //Coordenada X en la grilla
    private int CurrentGridY; //Coordenada Y en la grilla
    private Vector3 targetPosition; //Posicion hacia donde moverse
    
    // REFERENCIAS EXTRA
    private PlayerSpriteManager _spriteManager; //Maneja los sprites
    private PlayerAnimator _playerAnimator; //Maneja animaciones

    // Direccion que mira el jugador (por defecto a la derecha)
    public Vector2 lastDirection = Vector2.right;
    public bool UsedAbility = false; //Marca si uso una habilidad

    #region Properties
    public int BrokenBlocks => _brokenBlocks;
    public bool IsMoving => isMoving;
    public float GridSize => gridSize;
    public LayerMask CollisionLayer => collisionLayer;
    public List<BuffSO> GetBuffs() => _currentBuffs;
    public List<DebuffSO> GetCurrentDebuffs() => _currentDebuffs;
    public LayerMask ObstaclesInteractionLayer => obstaclesInteractionLayer;
    public bool LevelCompleted { get; private set; }
    public Vector3 PreviousPosition => _previousPosition;
    public MovementCounter MovementCounter => _movementCounter;
    public GameObject GlobalLights => _globalLights;
    public GameObject BlindnessLights => _blindnessLights;
    public bool blinded;
    #endregion
    private bool _isShiftPressedForBomb = false;
    #region Events
    // Eventos
    public delegate void UseAbilityHandler(Vector2 position, Vector2 direction);
    public event UseAbilityHandler OnUseAbility; //Evento cuando usa habilidad 

    public delegate void PlayerMovedHandler(Vector3 newPosition); //Evento cuando se mueve
    public event PlayerMovedHandler OnPlayerMoved;

    public delegate void AbilityUsedHandler(Vector2 position, Vector2 direction, bool success);
    public event AbilityUsedHandler OnAbilityUsed; //Evento cuando habilidad fue usada

    public event Action OnBlindnessApplied;
    public event Action OnBlindnessRemoved;

    // Eventos para coordinar animaciones
    public event Action OnInputDetected;
    public event Action OnMovementStarted;
    public event Action OnMovementStopped;
    public event Action OnAbilityTriggered;
    #endregion

    #region Unity Lifecycle Methods
    private void Awake()
    {
        _movementCounter = GetComponent<MovementCounter>();
        _spriteManager = GetComponent<PlayerSpriteManager>();
        _playerAnimator = GetComponent<PlayerAnimator>();
        // Get or create PlayerAudioController
        if (_playerAudioController == null) _playerAudioController = GetComponent<PlayerAudioController>();
        if (_playerAnimator == null) Debug.LogWarning("PlayerAnimator component not found on player. Animations will be handled by legacy code.");
        // Suscripcion a eventos de movimiento
        OnMovementStarted += HandleMovementStarted;
    }

    private void OnEnable() => GameLevelEvents.OnPlayerSpawned += OnPlayerSpawned;
    private void OnDisable() 
    {
        GameLevelEvents.OnPlayerSpawned -= OnPlayerSpawned;
        OnMovementStarted -= HandleMovementStarted;
    }
    private void OnDestroy() 
    {
        GameLevelEvents.OnPlayerSpawned -= OnPlayerSpawned;
        OnMovementStarted -= HandleMovementStarted;
    }

    private void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        //Ajusta el player a la grilla (se snapea)
        CurrentGridX = Mathf.RoundToInt(transform.position.x / gridSize);
        CurrentGridY = Mathf.RoundToInt(transform.position.y / gridSize);
        transform.position = new Vector3(CurrentGridX * gridSize, CurrentGridY * gridSize, transform.position.z);
    }

    private void Update()
    {
        //No procesa input si el nivel esta terminando
        if (GameManager.Instance != null && GameManager.Instance.IsLevelCompleting) return;
        ProcessInput(); //Lee input del jugador
        MoveTowardsTarget(); //Mueve al jugador
        CheckExplosiveBuff();
        CheckJumpBuff();
        CheckDebuffs();
        checkBlinded();
    }
    #endregion

    #region Input Processing
    //Variables relacionadas con el dash
    private bool _isShiftHeldDown = false; //True mientras el jugador mantenga presionado Shift
    private bool _hasDashFeedbackTriggered = false; //Evita que el feedback de "no puedes hacer dash" se repita varias veces
    
    private void ProcessInput()
    {
        // NO hacer nada si los inputs estan bloqueados (por ejemplo, durante una cinematica o pausa)
        if (!_inputEnabled) return; //No procesar input si el jugador ya esta en movimiento
        if (isMoving) return; //Capturamos el input del teclado

        var horizontalInput = Input.GetAxisRaw("Horizontal");
        var verticalInput = Input.GetAxisRaw("Vertical");
        bool isShiftPressed = Input.GetKey(KeyCode.LeftShift); //Dash
        bool spacePressed = (horizontalInput != 0 || verticalInput != 0); //Detectar si el jugador presiono alguna direccion
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift); //Shift presionado
        _isShiftPressedForBomb = shiftHeld;
        // Si soltamos Shift, reseteamos el feedback del dash
        if (!isShiftPressed && _isShiftHeldDown)
        {
            _isShiftHeldDown = false;
            _hasDashFeedbackTriggered = false;
        }
        // Si hay cualquier tipo de input (movimiento o habilidad), avisamos al sistema
        if (Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f || spacePressed) OnInputDetected?.Invoke(); // Evento para notificar que el jugador presiono algo
        // Comprobamos si estamos en el HUB (en esa zona no se permite dash)
        bool isInHubScene = IsInHubScene();
        // Si intentamos hacer dash en el hub o sin bloques rotos, damos feedback
        if (isShiftPressed && (isInHubScene || _brokenBlocks <= 0))
        {
            // Marcamos que Shift esta presionado
            _isShiftHeldDown = true;
            // En el hub simplemente no se puede hacer dash
            if (isInHubScene) Debug.Log("[PlayerController] Attempted to dash in hub scene - not allowed");
            else if (_brokenBlocks <= 0 && !_hasDashFeedbackTriggered)
            {
                // Si no tenemos bloques rotos para gastar mostramos un feedback visual de error
                PlayCantDashFeedback();
                _hasDashFeedbackTriggered = true;
                Debug.Log("[PlayerController] Attempted to dash without broken blocks");
            }
            // Si estamos en el HUB cancelamos el intento de dash
            if (isInHubScene) isShiftPressed = false;
        }
        if (Input.GetKeyDown(KeyCode.Space) && explosive && bombsAvailable > 0) HandleBombPlacement();
        //Si tenemos el debuff de controles invertidos, damos vuelta los inputs
        if (_isControlsInverted)
        {
            horizontalInput = -horizontalInput;
            verticalInput = -verticalInput;
        }
        
        // USO DE HABILIDAD (espacio)
        if (spacePressed)
        {
            // Cambiar direccion del sprite segun hacia donde miraba el jugador
            if (lastDirection.x != 0) transform.localScale = new Vector3(lastDirection.x * Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            // Avisamos que se uso la habilidad (para animaciones y efectos)
            OnAbilityTriggered?.Invoke();
            UsedAbility = true;
            // Ejecutamos el efecto de la habilidad en la direccion guardada
            InvokeUseAbility(lastDirection);
        }
        if (Input.GetKeyDown(KeyCode.Escape) && !isInHubScene) GameManager.Instance.LoadHub();
        // Guardamos la ultima direccion de movimiento (horizontal)
        if (Mathf.Abs(horizontalInput) > 0.5f && (GameManager.Instance?.LevelManager == null || !GameManager.Instance.LevelManager.IsBlockMoving))
        {
            lastDirection = new Vector2(Mathf.Sign(horizontalInput), 0);
            TryMoveInDirection(horizontalInput, true, isShiftPressed); //Intentamos movernos en esa direccion
        }
        else if (Mathf.Abs(verticalInput) > 0.5f && (GameManager.Instance?.LevelManager == null || !GameManager.Instance.LevelManager.IsBlockMoving))
        {
            lastDirection = new Vector2(0, Mathf.Sign(verticalInput)); //Guardamos la ultima direccion de movimiento (vertical)
            TryMoveInDirection(verticalInput, false, isShiftPressed); //Intentamos movernos en esa direccion
        }
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// Checks if the player is currently in the hub scene.
    /// </summary>
    /// <returns>True if in the hub scene, false otherwise.</returns>
    private bool IsInHubScene()
    {
        // Si no existe el GameManager no podemos determinar nada devolvemos false
        if (GameManager.Instance == null) return false;
        // Si HubManager esta activo significa que estamos en la escena del Hub
        return GameManager.Instance.HubManager != null && GameManager.Instance.HubManager.isActiveAndEnabled;
    }
    #endregion

    #region Movement
    private void TryMoveInDirection(float input, bool isHorizontal, bool useExtendedMove)
    {
        // Si los bloques del nivel estan en movimiento el jugador no puede moverse
        if (GameManager.Instance?.LevelManager != null && GameManager.Instance.LevelManager.IsBlockMoving)
        {
            Debug.Log("[PlayerController] Movement blocked - blocks are currently moving");
            return;
        }
        //Determinar direccion: 1 = positivo (derecha/arriba), -1 = negativo (izquierda/abajo)
        var direction = input > 0 ? 1 : -1;
        int moveDistance = 1; //Por defecto se mueve 1 casilla
        isDashing = false; //Por defecto NO esta haciendo dash

        // ¿Esta intentando hacer un movimiento extendido (dash)?
        bool attemptingExtendedMove = useExtendedMove && _brokenBlocks > 0;

        // Calcular la primera casilla hacia la que se moveria
        var firstCheckPosition = isHorizontal ?
            new Vector3((CurrentGridX + direction) * gridSize, CurrentGridY * gridSize, transform.position.z) :
            new Vector3(CurrentGridX * gridSize, (CurrentGridY + direction) * gridSize, transform.position.z);

        // Hacer un raycast para comprobar si hay obstaculo en esa primera casilla
        RaycastHit2D firstHit = Physics2D.Raycast(transform.position, firstCheckPosition - transform.position, gridSize, collisionLayer);
        if (firstHit.collider != null)
        {
            // Si hay algo bloqueando se cancela el movimiento
            OnMovementStopped?.Invoke();
            return;
        }

        // Si intenta un dash hay que comprobar que TODO el camino esta libre
        if (attemptingExtendedMove)
        {
            // Se calcula cuanto puede llegar a avanzar segun la fuerza del dash
            int potentialDashDistance = Mathf.RoundToInt(_dashAbility.CurrentStrengthLevel);
            bool pathClear = true;
            
            // Revisar cada casilla desde la 2 hasta la distancia maxima del dash
            for (int i = 2; i <= potentialDashDistance; i++)
            {
                // Calculate position to check
                var checkPosition = isHorizontal ?
                    new Vector3((CurrentGridX + direction * i) * gridSize, CurrentGridY * gridSize, transform.position.z) :
                    new Vector3(CurrentGridX * gridSize, (CurrentGridY + direction * i) * gridSize, transform.position.z);

                // Raycast to check for any obstacles
                RaycastHit2D hit = Physics2D.Raycast(transform.position, checkPosition - transform.position, gridSize * i, collisionLayer);
                
                if (hit.collider != null)
                {
                    Debug.Log($"[PlayerController] Found obstacle at distance {i} in dash path: {hit.collider.name}. Limiting dash.");
                    pathClear = false;
                    break;
                }
            }

            // Si el camino esta libre: ejecutar dash
            if (pathClear)
            {
                Debug.Log("[PlayerController] Full dash path is clear. Performing extended move.");
                moveDistance = potentialDashDistance;
                isDashing = true;
                Debug.Log($"[PlayerController] Using dash strength of {_dashAbility.CurrentStrengthLevel}, rounded to {moveDistance}");
            }
            else
            {
                // Si no esta libre se mueve solo 1 casilla
                Debug.Log("[PlayerController] Obstacles detected in dash path. Limiting to regular move.");
                moveDistance = 1;
            }
        }

        // Ajustar la direccion visual del jugador (mirando a izquierda/derecha)
        if (isHorizontal) transform.localScale = new Vector3(direction * Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        
        // Actualizar posicion en el grid
        if (isHorizontal) CurrentGridX += direction * moveDistance;
        else CurrentGridY += direction * moveDistance;
        //Establecer la posicion objetivo en el mundo
        targetPosition = new Vector3(CurrentGridX * gridSize, CurrentGridY * gridSize, transform.position.z);
        // Activar animacion de movimiento
        OnMovementStarted?.Invoke();
        isMoving = true;

        // Si fue un dash (mas de 1 casilla) gastar un bloque roto
        if (moveDistance > 1)
        {
            _brokenBlocks--;
            if (GameManager.Instance?.LevelManager?.GameCanvasManager?.StoredFoodUI != null) GameManager.Instance.LevelManager.GameCanvasManager.StoredFoodUI.SetStoredFood(BrokenBlocks);
            Debug.Log($"[PlayerController] Used extended move! Remaining broken blocks: {_brokenBlocks}");
        }
    }

    private void MoveTowardsTarget()
    {
        if (!isMoving) return;
        // Guardar posicion anterior
        _previousPosition = transform.position;

        // Si hace dash usa velocidad aumentada
        float currentSpeed = isDashing ? moveSpeed * dashSpeedMultiplier : moveSpeed;
        // Mover al jugador hacia la posicion objetivo
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, currentSpeed * Time.deltaTime);
        //SI llego al objetivo
        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            transform.position = targetPosition;
            isMoving = false;
            isDashing = false; // Si no esta presionando teclas, notificar que el movimiento termino

            // Only notify movement stopped if no movement keys are being held
            bool isHoldingMovementKey = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f || 
                                        Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f;
        
            // Notify movement stopped for animation
            if (!isHoldingMovementKey) OnMovementStopped?.Invoke();
            //Notificar a otros sistemas que el jugador se movio
            OnPlayerMoved?.Invoke(transform.position);
        }
    }
    //FEEDBACK VISUAL CUANDO NO PUEDE HACER DASH
    private void PlayCantDashFeedback()
    {
        // Si ya esta mostrando feedback, no repetir
        if (_isFeedbackPlaying) return;
        // Make sure we have a sprite renderer to shake
        if (_spriteRenderer == null)
        {
            Debug.LogWarning("[PlayerController] Cannot play dash feedback - _spriteRenderer is null");
            return;
        }
        // Cancelar animaciones anteriores
        _feedbackSequence?.Kill();

        // Crear animacion de sacudida (shake)
        _feedbackSequence = DOTween.Sequence();
        
        // Set the flag to prevent multiple plays
        _isFeedbackPlaying = true;

        // Add shake effect to the sprite renderer with reduced intensity
        _feedbackSequence.Append(_spriteRenderer.transform.DOShakePosition(
            _cantDashShakeDuration * 0.7f,         // Shorter duration for quicker feedback
            _cantDashShakeStrength * 0.6f,        // Lower strength for less aggressive shake
            _cantDashShakeVibrato - 2,            // Reduce vibrato for smoother shake
            _cantDashShakeRandomness * 0.5f,      // Reduce randomness for more controlled shake
            false,
            _cantDashShakeFadeOut));

        // Al terminar el feedback volver a posicion normal
        _feedbackSequence.OnComplete(() => {
            _spriteRenderer.transform.localPosition = Vector3.zero;
            _isFeedbackPlaying = false;
            Debug.Log("[PlayerController] Dash feedback completed");
        });

        Debug.Log("[PlayerController] Can't dash - no broken blocks available");
    }

    /// <summary>
    /// JUMP (SALTO SOBRE UN VACIO)
    /// Called by TilemapManager when performing a jump over a void tile.
    /// </summary>
    public void HandleJump()
    {
        // Signal that movement is complete
        isMoving = false;

        // Ensure player is at target position
        targetPosition = transform.position;
        CurrentGridX = Mathf.RoundToInt(transform.position.x / gridSize);
        CurrentGridY = Mathf.RoundToInt(transform.position.y / gridSize);
    }
    //BOMBA LOGICA
    private void HandleBombPlacement() 
    {
        if (_bomb == null) return;
        TilemapManager tilemapManager = FindObjectOfType<TilemapManager>();
        if (tilemapManager == null)
        {
            Debug.LogError("TilemapManager nulo!");
            return;
        }
        // Calcular la posición del tile en el que está el pollo
        Vector3Int chickenTilePos = tilemapManager.WorldToCell(transform.position);
        //Revisar si el tile debajo del pollo es valido para poner bomba
        CustomTile currentTile = tilemapManager.GetTileAtPosition(chickenTilePos);

        //Obtener la posicion en el mundo del centro de la celda
        Vector3 spawnPos = tilemapManager.CellToWorld(chickenTilePos) + new Vector3(0.5f, 0.5f, 0f);
        bombsAvailable--;
        //Instanciar la bomba
        Instantiate(_bomb, spawnPos, Quaternion.identity);
    }

    /// <summary>
    /// Updates the player's position after a jump action.
    /// </summary>
    /// <param name="worldPosition">The new world position after the jump.</param>
    public void UpdatePositionAfterJump(Vector3 worldPosition)
    {
        // Set the transform position directly
        transform.position = worldPosition;

        // Update grid coordinates directly from the passed worldPosition
        CurrentGridX = Mathf.RoundToInt(worldPosition.x / gridSize);
        CurrentGridY = Mathf.RoundToInt(worldPosition.y / gridSize);

        // Update target position to the exact world position
        targetPosition = worldPosition;

        // Log the position update to verify correctness
        Debug.Log($"[PlayerController] Posicion tras salto: Grid({CurrentGridX},{CurrentGridY}), World({worldPosition})");

        // Trigger the moved event
        OnPlayerMoved?.Invoke(worldPosition);
    }

    public void SetMovementBlocked(bool blocked)
    {
        isMoving = blocked;
    }
    #endregion

    #region Ability Handling
    //HABILIDADES
    private void InvokeUseAbility(Vector2 direction)
    {
        OnUseAbility?.Invoke(transform.position, direction);
    }

    public void ReportAbilitySuccess(Vector2 position, Vector2 direction, bool success)
    {
        OnAbilityUsed?.Invoke(position, direction, success);
    }

    public void BlockBroken()
    {
        _brokenBlocks++; //Aumenta la cantidad de bloques rotos que puede usar para dash
    }
    #endregion

    #region Audio Handling
    /// <summary>
    /// Reproduce el sonido de pasos cuando empiezas a moverte
    /// </summary>
    private void HandleMovementStarted()
    {
        if (_playerAudioController != null) _playerAudioController.PlayFootstep();
    }
    #endregion

    #region Player State Management
    //ESTADO DEL JUGADOR (Buffs/Debuffs)
    private void OnPlayerSpawned(Transform playerTransform, List<BuffSO> buffs = null, List<DebuffSO> debuffs = null)
    {
        if (playerTransform != transform) return;
        transform.position = playerTransform.position;

        // Inicializa las listas si son nulas
        _currentBuffs ??= new List<BuffSO>();
        _currentDebuffs ??= new List<DebuffSO>();

        // Vacia los Buffs/Debuffs existentes
        _currentBuffs.Clear();
        _currentDebuffs.Clear();

        // Add the new buffs/debuffs if provided
        if (buffs != null) _currentBuffs.AddRange(buffs);
        if (debuffs != null) _currentDebuffs.AddRange(debuffs);

        // Revisa si tiene controles invertidos
        _isControlsInverted = _currentDebuffs.Any(d => d.Type == DebuffType.Inverted);

        // Revisar si tiene ceguera
        bool hasBlindness = _currentDebuffs.Any(d => d.Type == DebuffType.Blindness);
        ApplyBlindnessDebuff(hasBlindness);

        // Update sprites based on new buffs/debuffs
        if (_spriteManager != null) _spriteManager.UpdatePlayerSprites();
    }

    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;

        if (!enabled)
        {
            moveDirection = Vector2.zero;
            isMoving = false;
        }
    }

    public bool HasBlindnessDebuff()
    {
        if (_currentDebuffs == null || _currentDebuffs.Count == 0) return false;
        return _currentDebuffs.Any(debuff => debuff.Type == DebuffType.Blindness);
    }

    public void ApplyBlindnessDebuff(bool isBlind)
    {
        if (isBlind)
        {
            if (_globalLights != null) _globalLights.SetActive(false);
            if (_blindnessLights != null) _blindnessLights.SetActive(true);
            TriggerBlindnessApplied();
        }
        else
        {
            if (_globalLights != null) _globalLights.SetActive(true);
            if (_blindnessLights != null) _blindnessLights.SetActive(false);
            TriggerBlindnessRemoved();
        }
    }

    public void TriggerBlindnessApplied()
    {
        OnBlindnessApplied?.Invoke();
    }

    public void TriggerBlindnessRemoved()
    {
        OnBlindnessRemoved?.Invoke();
    }

    public bool HasBuff(BuffType buffType)
    {
        if (_currentBuffs == null || _currentBuffs.Count == 0) return false;
        return _currentBuffs.Any(buff => buff.Type == buffType);
    }

    public void SetControlsInverted(bool inverted)
    {
        _isControlsInverted = inverted;
        if (_isControlsInverted) Debug.Log("Controls are now inverted");
        else Debug.Log("Controls are back to normal");
    }

    public void UpdateEffects(List<BuffSO> buffs = null, List<DebuffSO> debuffs = null)
    {
        bool changed = false;

        if (buffs != null)
        {
            _currentBuffs.Clear();
            _currentBuffs.AddRange(buffs);
            changed = true;
        }

        if (debuffs != null)
        {
            _currentDebuffs.Clear();
            _currentDebuffs.AddRange(debuffs);
            changed = true;

            // Check for inverted controls debuff
            _isControlsInverted = _currentDebuffs.Any(d => d.Type == DebuffType.Inverted);

            // Check for blindness debuff
            bool hasBlindness = _currentDebuffs.Any(d => d.Type == DebuffType.Blindness);
            ApplyBlindnessDebuff(hasBlindness);
        }
        if (changed && _spriteManager != null) _spriteManager.UpdatePlayerSprites();
    }

    public void AddBuffs(List<BuffSO> buffs)
    {
        ClearBuffs();
        _currentBuffs.AddRange(buffs);
    }

    public void ClearBuffs()
    {
        _currentBuffs.Clear();
    }

    public void SetLevelCompleted(bool completed)
    {
        LevelCompleted = completed;
        if (completed) _inputEnabled = false; //Si se completo el nivel se bloquea el input
    }
    #endregion
    private void CheckExplosiveBuff()
    {
        explosive = HasBuff(BuffType.Explosive);
    }
    private void CheckJumpBuff()
    {
        jumpbuff = HasBuff(BuffType.Jump);
    }
    // Chequea si el jugador tiene al menos 2 debuffs activos
    private void CheckDebuffs()
    {
        // Si la lista no está inicializada, la tratamos como vacía
        if (_currentDebuffs.Count == 2) debuff = true;
        else debuff = false;
    }
    private void checkBlinded()
    {
        if (_currentDebuffs.Count == 1 && _isControlsInverted == false) blinded = true;
    }
    public void Die()
    {
        LevelManager levelManager = FindObjectOfType<LevelManager>();
        GameCanvasManager canvasManager = FindObjectOfType<GameCanvasManager>();
        levelManager.KillPlayer(DeathReason.Void);
        canvasManager.ShowGameOverPanel(DeathReason.Void);
    }
}
