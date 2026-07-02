using System;
using Scriptables;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Components
{
    public class DevelopmentBuffToggle : MonoBehaviour
    {
        [SerializeField] private Toggle _toggle;
        [SerializeField] private Image _buffIcon;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Color _enabledColor = Color.green;
        [SerializeField] private Color _disabledColor = Color.red;

        private BaseEffectSO _buffEffect;

        public event Action<BaseEffectSO, bool> OnBuffToggled;

        public void Initialize(BaseEffectSO buffEffect)
        {
            _buffEffect = buffEffect;

            // Set buff icon
            if (buffEffect.icon != null)
            {
                _buffIcon.sprite = buffEffect.icon;
                _buffIcon.gameObject.SetActive(true);
            }
            else
            {
                _buffIcon.gameObject.SetActive(false);
            }

            // Set initial toggle state to off
            _toggle.isOn = false;
            // Remove: buffEffect.enabled = false;
            UpdateVisualState(false);

            // Subscribe to toggle changes
            _toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }

        private void OnToggleValueChanged(bool isOn)
        {
            // Remove: _buffEffect.enabled = isOn;
    
            // Update visual state
            UpdateVisualState(isOn);

            // Notify listeners
            OnBuffToggled?.Invoke(_buffEffect, isOn);

            Debug.Log($"[DevelopmentBuffToggle] Buff '{_buffEffect.name}' toggled: {(isOn ? "ENABLED" : "DISABLED")}");
        }
        
        private void UpdateVisualState(bool isEnabled)
        {
            if (_backgroundImage != null)
            {
                _backgroundImage.color = isEnabled ? _enabledColor : _disabledColor;
            }
            
            // Optional: adjust icon appearance based on state
            _buffIcon.color = isEnabled ? Color.white : new Color(1, 1, 1, 0.5f);
        }

        private void OnDestroy()
        {
            // Clean up listeners
            if (_toggle != null)
            {
                _toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
            }
        }
    }
}