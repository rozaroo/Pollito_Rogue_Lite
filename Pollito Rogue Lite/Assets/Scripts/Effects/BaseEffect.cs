using Enums;
using Player;
using Scriptables;
using UnityEngine;

namespace Effects
{
    public abstract class BaseEffect : MonoBehaviour
    {
        private BaseEffectSO _effectData;
        public abstract BuffType Type { get; }

        [SerializeField] private PlayerController _playerController;
        [SerializeField] private PlayerEffectManager _playerEffectManager;
        [SerializeField] private EffectEvents _effectEvents;
        
        public BaseEffectSO EffectData => _effectData;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
            _playerEffectManager = GetComponent<PlayerEffectManager>();
            _effectEvents = GetComponent<EffectEvents>();

            OnEffectRegistered();

            _playerEffectManager?.RegisterEffect(this);
        }

        /// <summary>
        /// Called when the component is added to the player and references are setup.
        /// </summary>
        public virtual void OnEffectRegistered()
        {
            _effectEvents?.InvokeRegistered(Type);
        }

        /// <summary>
        /// Activates the buff/debuff logic.
        /// </summary>
        public abstract void ActivateEffect();

        /// <summary>
        /// Deactivates the buff/debuff logic.
        /// </summary>
        public abstract void DeactivateEffect();

        /// <summary>
        /// Cleans up references and events when the buff is fully removed.
        /// </summary>
        public virtual void OnEffectUnregistered()
        {
            _effectEvents?.InvokeUnregistered(Type);
        }

        public void NotifyEffectActivated()
        {
            _effectEvents?.InvokeActivated(Type);
        }

        public void NotifyEffectDeactivated()
        {
            _effectEvents?.InvokeDeactivated(Type);
        }
    }
}