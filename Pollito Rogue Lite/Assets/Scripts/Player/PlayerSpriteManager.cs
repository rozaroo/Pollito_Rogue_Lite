using System.Collections.Generic;
using Enums;
using Scriptables;
using Serialization;
using UnityEngine;

namespace Player
{
    public class PlayerSpriteManager : MonoBehaviour
    {
        [Header("Body Part Renderers")]
        [SerializeField] private SpriteRenderer _headRenderer;
        [SerializeField] private SpriteRenderer _bodyRenderer;
        [SerializeField] private SpriteRenderer _backFeetRenderer;
        [SerializeField] private SpriteRenderer _frontFeetRenderer;

        [Header("Default Sprites")]
        [SerializeField] private Sprite _defaultHeadSprite;
        [SerializeField] private Sprite _defaultBodySprite;
        [SerializeField] private Sprite _defaultBackFeetSprite;
        [SerializeField] private Sprite _defaultFrontFeetSprite;
        
        private Dictionary<SpriteRenderer, Color> _originalColors = new();

        private PlayerController _playerController;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
        }

        private void Start()
        {
            ResetToDefaultSprites(); //Asegura que los sprites empiecen con los valores por defecto.
            // Aplica los buffs/debuffs actuales.
            UpdatePlayerSprites();
            // Se suscribe a eventos del nivel (OnPlayerSpawned y OnLevelCompleted) 
            // para actualizar los sprites cuando el jugador aparece o cuando el nivel termina
            GameLevelEvents.OnPlayerSpawned += HandlePlayerSpawned;
            GameLevelEvents.OnLevelCompleted += OnLevelCompleted;
            // Es una corrutina que revisa constantemente si _headRenderer ha sido desactivado accidentalmente.
            StartCoroutine(MonitorHeadRenderer());
        }

        private void OnDestroy()
        {
            GameLevelEvents.OnPlayerSpawned -= HandlePlayerSpawned;
            GameLevelEvents.OnLevelCompleted -= OnLevelCompleted;
        }
        
        private System.Collections.IEnumerator MonitorHeadRenderer()
        {
            bool lastState = _headRenderer ? _headRenderer.enabled : false;
            
            while (true)
            {
                if (_headRenderer && _headRenderer.enabled != lastState)
                {
                    if (!_headRenderer.enabled)
                    {
                        Debug.LogWarning($"[PlayerSpriteManager] Head renderer disabled! Stack trace: {System.Environment.StackTrace}");
                        // Force it back to enabled state
                        _headRenderer.enabled = true;
                    }
                    lastState = _headRenderer.enabled;
                }
                
                yield return new WaitForSeconds(0.1f);
            }
            // Revisa cada 0.1 segundos si el renderer de la cabeza ha sido desactivado.
            //Si lo encuentra desactivado, lo vuelve a activar. Esto evita bugs donde la cabeza del jugador desaparece accidentalmente.
        }
        
        private void OnLevelCompleted()
        {
            Debug.Log("[PlayerSpriteManager] Level completed - ensuring sprites remain visible");
            EnsureRenderersActive();
        }

        private void HandlePlayerSpawned(Transform playerTransform, List<BuffSO> buffs = null, List<DebuffSO> debuffs = null)
        {
            if (playerTransform != transform) return;
            UpdatePlayerSprites(); //Cuando el jugador reaparece, actualiza sus sprites con los buffs/debuffs actuales
        }

        public void UpdatePlayerSprites()
        {
            // Reset to defaults first
            ResetToDefaultSprites();

            if (_playerController == null)
            {
                Debug.LogError("Player controller reference is missing in PlayerSpriteManager");
                return;
            }

            // Get active buffs from player controller
            List<BuffSO> activeBuffs = _playerController.GetBuffs();
            
            // Get active debuff types
            List<DebuffType> activeDebuffTypes = new List<DebuffType>();
            List<DebuffSO> activeDebuffs = _playerController.GetCurrentDebuffs();
            
            if (activeDebuffs != null)
            {
                foreach (var debuff in activeDebuffs)
                {
                    if (debuff != null) activeDebuffTypes.Add(debuff.Type);
                }
            }

            // Process each body part
            ProcessBodyPart(BodyPart.Head, _headRenderer, activeBuffs, activeDebuffTypes);
            ProcessBodyPart(BodyPart.Body, _bodyRenderer, activeBuffs, activeDebuffTypes);
            ProcessBodyPart(BodyPart.BackFeet, _backFeetRenderer, activeBuffs, activeDebuffTypes);
            ProcessBodyPart(BodyPart.FrontFeet, _frontFeetRenderer, activeBuffs, activeDebuffTypes);
            
            // Ensure all renderers are enabled, especially the head
            EnsureRenderersActive();
            //Primero resetea los sprites a los valores por defecto
            //Obtiene los buffs y debuffs actuales del jugador.
            //Llama a ProcessBodyPart para cada parte del cuerpo, aplicando los sprites correspondientes según los buffs.
            //Finalmente, asegura que todos los renderers estén activos.
        }
        
        /// <summary>
        /// Makes sure all sprite renderers are active and visible
        /// </summary>
        private void EnsureRenderersActive()
        {
            if (_headRenderer != null && !_headRenderer.enabled) 
            {
                _headRenderer.enabled = true;
                Debug.Log("[PlayerSpriteManager] Re-enabled head renderer that was found disabled");
            }
            
            if (_bodyRenderer != null && !_bodyRenderer.enabled) _bodyRenderer.enabled = true;
            if (_backFeetRenderer != null && !_backFeetRenderer.enabled) _backFeetRenderer.enabled = true;
            if (_frontFeetRenderer != null && !_frontFeetRenderer.enabled) _frontFeetRenderer.enabled = true;
        }
        
        private void ProcessBodyPart(BodyPart part, SpriteRenderer renderer, List<BuffSO> buffs, List<DebuffType> activeDebuffs)
        {
            if (renderer == null) return;

            // Find the highest priority buff sprite for this body part
            DebuffRequiredSprite bestMatch = null;
            int highestRequirementCount = -1;
            Sprite bestSprite = null;

            foreach (var buff in buffs)
            {
                if (buff == null) continue;
                
                Sprite buffSprite = buff.GetSpriteForBodyPart(part, activeDebuffs);
                if (buffSprite != null)
                {
                    // We found a sprite to use
                    renderer.sprite = buffSprite;
 
                    // Last buff wins - we could implement priority logic here
                    bestSprite = buffSprite;
                }
            }

            // If we found a best match, apply it
            if (bestSprite != null) renderer.sprite = bestSprite;
            
        }

        private void ResetToDefaultSprites()
        {
            if (_headRenderer) _headRenderer.sprite = _defaultHeadSprite;
            if (_bodyRenderer) _bodyRenderer.sprite = _defaultBodySprite;
            if (_backFeetRenderer) _backFeetRenderer.sprite = _defaultBackFeetSprite;
            if (_frontFeetRenderer) _frontFeetRenderer.sprite = _defaultFrontFeetSprite;
        }
        
        /// <summary>
        /// Sets the player sprite to indicate death/failure state with fully black appearance
        /// </summary>
        [ContextMenu("Set Death Appearance")]
        public void SetDeathAppearance()
        {
            // Get all sprite renderers
            var renderers = GetComponentsInChildren<SpriteRenderer>();
            foreach (var renderer in renderers)
            {
                // Store original color if needed for reset
                if (!_originalColors.ContainsKey(renderer)) _originalColors[renderer] = renderer.color;
                // Apply fully black effect with slightly reduced alpha
                var deathColor = Color.black;
                renderer.color = deathColor;
            }
            Debug.Log("[PlayerSpriteManager] Death appearance applied (black)");
            //Cambia todos los sprites a negro para mostrar que el jugador murió.
            //Guarda el color original para poder restaurarlo.
        }

        /// <summary>
        /// Resets the player sprite to its original appearance
        /// </summary>
        public void ResetAppearance()
        {
            // Restore all sprite renderers to original colors
            var renderers = GetComponentsInChildren<SpriteRenderer>();
            foreach (var renderer in renderers)
            {
                renderer.color = _originalColors.TryGetValue(renderer, out var color) ? color :
                    Color.white;
            }
        }
    }
}