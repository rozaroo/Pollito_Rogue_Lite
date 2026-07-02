using System;
using System.Collections.Generic;
using Custom.Logger;
using DG.Tweening;
using Player;
using UI.Components;
using UnityEngine;
using UnityEngine.Audio;

namespace UI
{
    public class CharacterSelectorUI : MonoBehaviour
    {
        [SerializeField] private CharacterSelectorButton[] _optionButtons;
        [SerializeField] private GameObject _selectionIndicator;
        [SerializeField] private float _navigationTransitionDuration = 0.3f;
        
        [Header("Input Options for Select")]
        [SerializeField] private bool _useEnterKey = false;
        [SerializeField] private bool _useSpaceKey = true;
        [SerializeField] private bool _useWKey = false;

        private Action<int> _onOptionSelected;
        private int _currentSelectedIndex = 1; // Default to middle option
        private bool _isActive;
        [SerializeField] private AudioClip _moveSound;
        [SerializeField] private AudioSource _audioSource;

        private void Awake()
        {
            if (_selectionIndicator == null)
            {
                _selectionIndicator = new GameObject("SelectionIndicator");
                _selectionIndicator.transform.SetParent(transform);
            }
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
        }

        private void OnEnable()
        {
            _isActive = true;

            // Reset DOTween in case it was paused
            DOTween.PlayAll();
        }

        private void OnDisable()
        {
            _isActive = false;

            // Pause all DOTween animations when disabled
            DOTween.PauseAll();
        }

        private void Update()
        {
            if (!_isActive || _optionButtons == null || _optionButtons.Length == 0) return;
            // Handle keyboard navigation
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) MoveSelection(-1);
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) MoveSelection(1);
            // Handle selection confirmation with configurable keys
            if ((_useEnterKey && Input.GetKeyDown(KeyCode.Return)) || (_useSpaceKey && Input.GetKeyDown(KeyCode.Space)) || (_useWKey && Input.GetKeyDown(KeyCode.W))) ConfirmSelection();

        }

        public void SetupOptions(List<ChildOption> options, Action<int> onOptionSelected)
        {
            _onOptionSelected = onOptionSelected;

            // Ensure we have the right number of buttons
            if (_optionButtons == null || _optionButtons.Length != 3)
            {
                CustomLogger.Log("ERROR: CharacterSelectorUI requires exactly 3 button references!");
                return;
            }

            // Set up each button with its option
            for (int i = 0; i < options.Count && i < _optionButtons.Length; i++)
            {
                int index = i; // Local copy for lambda
                _optionButtons[i].Setup(options[i], () => SelectOption(index));
                _optionButtons[i].gameObject.SetActive(true);
            }

            // Default select the middle option (index 1)
            _currentSelectedIndex = 1;
            UpdateSelectionStates();
        }

        private void MoveSelection(int direction)
        {
            int newIndex = _currentSelectedIndex + direction;
            // Wrap around selection
            if (newIndex < 0) newIndex = _optionButtons.Length - 1;
            else if (newIndex >= _optionButtons.Length) newIndex = 0;
            // Only update if it's an active option
            if (_optionButtons[newIndex].gameObject.activeSelf)
            {
                _currentSelectedIndex = newIndex;
                // Update selection states of all buttons
                UpdateSelectionStates();
                PlayMoveSound();
            }
        }

        private void UpdateSelectionStates()
        {
            // Update all buttons' selection state
            for (int i = 0; i < _optionButtons.Length; i++)
            {
                if (_optionButtons[i] != null)
                {
                    _optionButtons[i].SetSelectedAsActive(i == _currentSelectedIndex);
                }
            }
        }

        private void ConfirmSelection()
        {
            if (_optionButtons[_currentSelectedIndex].gameObject.activeSelf)
            {
                // Play selection effect (shake) on the selected button
                _optionButtons[_currentSelectedIndex].PlaySelectionEffect();
                // Add small delay before actually selecting to allow animation to play
                DOVirtual.DelayedCall(0.5f, () => {
                    SelectOption(_currentSelectedIndex);
                });
            }
        }
        private void SelectOption(int index)
        {
            _onOptionSelected?.Invoke(index);
        }
        private void PlayMoveSound()
        {
            if (_moveSound != null && _audioSource != null) _audioSource.PlayOneShot(_moveSound);
        }
    }
}