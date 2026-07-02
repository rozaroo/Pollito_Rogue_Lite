using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Managers;
using Scriptables;
using UI.Components;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    // Controla la UI de los toggles de Buffs y Debuffs de desarrollo
    // Estos toggles sirven para activar/desactivar efectos en el player
    public class DevelopmentBuffsUI : MonoBehaviour
    {
        [SerializeField] private Transform _buffTogglesContainer; //Donde se instancian los toggles de buffs
        [SerializeField] private Transform _debuffTogglesContainer; //Donde se instancian los toggles de debuffs
        [SerializeField] private GameObject _buffTogglePrefab; //Prefab de cada toggle (el boton ON/OFF)

        private readonly List<DevelopmentBuffToggle> _buffToggles = new();
        private readonly Dictionary<DevelopmentBuffToggle, Toggle> _toggleComponents = new();

        private void Start()
        {
            // Si NO es un build de debug, oculta este panel
            // (Probablemente solo se usa para testear buffs en editor/debug)
            if (!Debug.isDebugBuild) gameObject.SetActive(false);
        }

        // Pide instanciar los toggles de buffs y debuffs del jugador. Se hace con corrutina para esperar inicialización de managers
        public void PopulateBuffToggles(List<BuffSO> playerBuffs, List<DebuffSO> playerDebuffs)
        {
            StartCoroutine(PopulateWithDelay(playerBuffs, playerDebuffs));
        }

        private IEnumerator PopulateWithDelay(List<BuffSO> playerBuffs, List<DebuffSO> playerDebuffs)
        {
            // Espera un pequeño delay para asegurarse que MutationManager esta listo
            yield return new WaitForSeconds(0.25f);

            ClearExistingToggles(); //Limpia toggles viejos antes de crear nuevos

            if (_buffTogglePrefab == null || _buffTogglesContainer == null || _debuffTogglesContainer == null)
            {
                Debug.LogError("[DevelopmentBuffsUI] Missing references!");
                yield break;
            }

            // Obtiene buffs y debuffs disponibles desde MutationManager
            var availableBuffs = GameManager.Instance?.MutationManager?.AvailableBuffs;
            var availableDebuffs = GameManager.Instance?.MutationManager?.AvailableDebuffs;

            if (availableBuffs != null)
            {
                // Crea toggles para TODOS los buffs disponibles
                foreach (var buff in availableBuffs)
                {
                    // Verificia si este buff esta activo en el jugador
                    bool isActive = playerBuffs != null && playerBuffs.Any(b => b.name == buff.name);
                    InstantiateToggle(buff, _buffTogglesContainer, isActive);
                }
            }
            // Crea toggles para TODOS los debuffs disponibles
            if (availableDebuffs != null)
            {
                
                foreach (var debuff in availableDebuffs)
                {
                    // Check if this debuff is currently active on the player
                    bool isActive = playerDebuffs != null && playerDebuffs.Any(d => d.name == debuff.name);
                    InstantiateToggle(debuff, _debuffTogglesContainer, isActive);
                }
            }
        }
        //Borra los toggles viejos de la UI
        private void ClearExistingToggles()
        {
            // Desuscribir eventos
            foreach (var toggle in _buffToggles)
            {
                if (toggle != null) toggle.OnBuffToggled -= HandleBuffToggled;
            }

            _buffToggles.Clear();
            _toggleComponents.Clear();

            // Borra objetos UI de los contenedores
            if (_buffTogglesContainer != null)
            {
                foreach (Transform child in _buffTogglesContainer) 
                    Destroy(child.gameObject);
            }

            if (_debuffTogglesContainer != null)
            {
                foreach (Transform child in _debuffTogglesContainer)
                    Destroy(child.gameObject);
            }
        }
        // Intancia un toggle de UI para un buff/debuff
        private void InstantiateToggle(BaseEffectSO effect, Transform container, bool isActive)
        {
            var toggleGameObject = Instantiate(_buffTogglePrefab, container);
            var toggleComponent = toggleGameObject.GetComponent<DevelopmentBuffToggle>();
            var toggle = toggleGameObject.GetComponent<Toggle>();

            if (toggleComponent != null && toggle != null)
            {
                // Inicializa con el efecto (no activa todavia nada en el Player)
                toggleComponent.Initialize(effect);

                // Marca visualmente si esta activo actualmente
                if (isActive) toggle.isOn = true; // Remove: effect.enabled = true
                
                // Registrar eventos
                if (container == _buffTogglesContainer)
                {
                    // Si es un BUFF → desactiva los demás (solo uno activo a la vez)
                    toggleComponent.OnBuffToggled += (e, isOn) =>
                    {
                        if (isOn) DisableOtherBuffs(toggleComponent);
                        HandleBuffToggled(e, isOn);
                    };
                }
                else toggleComponent.OnBuffToggled += HandleBuffToggled; // Si es DEBUFF → puede haber varios a la vez
                _buffToggles.Add(toggleComponent);
                _toggleComponents[toggleComponent] = toggle;
            }
        }
        // Desactiva todos los demas buffs cuando uno se activa (Solo se puede tener un buff activo a la vez)
        private void DisableOtherBuffs(DevelopmentBuffToggle exceptToggle)
        {
            foreach (var toggle in _buffToggles)
            {
                if (toggle != exceptToggle && toggle.transform.parent == _buffTogglesContainer)
                {
                    if (_toggleComponents.TryGetValue(toggle, out Toggle uiToggle) && uiToggle.isOn) uiToggle.isOn = false;
                }
            }
        }
        //Maneja que pasa cuando se activa/desactiva un buff o debuff
        //Aplica los efectos al Player llamando a UpdateEffects()
        private void HandleBuffToggled(BaseEffectSO effect, bool isEnabled)
        {
            // Verifica que Player exista
            if (GameManager.Instance?.LevelManager?.Player == null) return;
            // Si es un Buff
            if (effect is BuffSO buff)
            {
                if (isEnabled)
                {
                    // Activa solo este buff
                    var buffs = new List<BuffSO> { buff };
                    GameManager.Instance.LevelManager.Player.UpdateEffects(buffs, null);
                }
                else GameManager.Instance.LevelManager.Player.UpdateEffects(new List<BuffSO>(), null); // Clear buffs when a buff is toggled off
            }
            // Si es un Debuff
            else if (effect is DebuffSO debuff) 
            {
                if (isEnabled)
                {
                    // Create a list with just this debuff and apply it
                    var debuffs = new List<DebuffSO> { debuff };
                    GameManager.Instance.LevelManager.Player.UpdateEffects(null, debuffs);
                }
                else GameManager.Instance.LevelManager.Player.UpdateEffects(null, new List<DebuffSO>());
                // Si se apaga limpia los debuffs
            }
        }
        //Quita un turno al jugador (boton de debug probablemente)
        public void RemovePlayerTurn()
        {
            GameManager.Instance.LevelManager.Player.MovementCounter.Decrease();
            GameManager.Instance.LevelManager.UpdateMoveCounter();
        }
        //Agrega un turno al jugador
        public void AddPlayerTurn()
        {
            GameManager.Instance.LevelManager.Player.MovementCounter.Increment();
            GameManager.Instance.LevelManager.UpdateMoveCounter();
        }
        //Cuando se destruye este objeto, desuscribe todos los eventos
        private void OnDestroy()
        {
            foreach (var toggle in _buffToggles)
            {
                if (toggle != null) toggle.OnBuffToggled -= HandleBuffToggled;
            }
            _toggleComponents.Clear();
        }
    }
}