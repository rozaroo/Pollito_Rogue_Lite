using System;
using System.Collections.Generic;
using DG.Tweening;
using Enums;
using Player;
using Scriptables;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Components
{
    public class CharacterSelectorButton : MonoBehaviour
    {
        [SerializeField] private ChildOption childData;
        [SerializeField] private Button button;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private GameObject effectPrefab;
        [SerializeField] private Transform effectContainer;
        [SerializeField] private RectTransform effectsSpawnPoint; // Reference to where effects should be positioned
        [SerializeField] private Vector2 effectsOffset = new Vector2(0, 50); // Offset from spawn point
        [SerializeField] private float effectSpacing = 40f; // Space between multiple effects
        [SerializeField] private bool showEffectsOnHover = true; // Option to show effects only on hover
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = new Color(0.8f, 0.9f, 1f);
        [SerializeField] private float pulseIntensity = 0.05f;
        [SerializeField] private float pulseDuration = 1f;

        private readonly List<GameObject> spawnedEffects = new();
        private readonly List<BuffSO> savedBuffs = new();
        private readonly List<DebuffSO> savedDebuffs = new();
        private bool _isSelectedAsActive;
        private Vector3 _originalScale;
        private Sequence _pulseSequence;
        private Tween _colorTween;

        private void Awake()
        {
            _originalScale = transform.localScale;
            
            if (backgroundImage == null && button != null)
            {
                backgroundImage = button.GetComponent<Image>();
            }
        }

        private void OnDisable()
        {
            KillTweens();
        }

        public void Setup(ChildOption option, Action onClicked)
        {
            childData = option;
            savedBuffs.Clear();
            savedDebuffs.Clear();
            ClearEffects();

            CreateEffectPanels(option.Buffs, true);
            CreateEffectPanels(option.Debuffs, false);

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClicked?.Invoke());
            }
            
            // Reset selection state
            SetSelectedAsActive(false);
        }

        public void SetSelectedAsActive(bool isActive)
        {
            if (_isSelectedAsActive == isActive) return;
            
            _isSelectedAsActive = isActive;
            KillTweens();
            
            // Update background color
            if (backgroundImage != null)
            {
                _colorTween = backgroundImage.DOColor(isActive ? selectedColor : normalColor, 0.3f);
            }
            
            // Reset scale
            transform.DOScale(_originalScale, 0.3f).SetEase(Ease.OutBack);
            
            // Start pulsing if selected
            if (isActive)
            {
                StartPulseAnimation();
            }
        }
        
        public void PlaySelectionEffect()
        {
            KillTweens();
            
            // Perform shake animation
            transform.DOShakeScale(0.5f, 0.2f, 10, 90, true)
                .OnComplete(() => {
                    // Resume pulse animation after shake
                    if (_isSelectedAsActive)
                    {
                        StartPulseAnimation();
                    }
                });
        }
        
        private void StartPulseAnimation()
        {
            KillPulseSequence();
            
            _pulseSequence = DOTween.Sequence();
            _pulseSequence.Append(transform.DOScale(_originalScale * (1 + pulseIntensity), pulseDuration / 2).SetEase(Ease.InOutSine));
            _pulseSequence.Append(transform.DOScale(_originalScale, pulseDuration / 2).SetEase(Ease.InOutSine));
            _pulseSequence.SetLoops(-1); // Loop infinitely
        }
        
        private void KillPulseSequence()
        {
            if (_pulseSequence != null && _pulseSequence.IsActive())
            {
                _pulseSequence.Kill();
                _pulseSequence = null;
            }
        }
        
        private void KillTweens()
        {
            KillPulseSequence();
            
            if (_colorTween != null && _colorTween.IsActive())
            {
                _colorTween.Kill();
                _colorTween = null;
            }
            
            // Kill any other tweens targeting this transform
            DOTween.Kill(transform);
        }

        private void CreateEffectPanels<T>(List<T> effects, bool isBuff) where T : BaseEffectSO
        {
            if (effects == null) return;

            foreach (var effect in effects)
            {
                if (effect == null) continue;

                if (isBuff) savedBuffs.Add(effect as BuffSO);
                else savedDebuffs.Add(effect as DebuffSO);

                Sprite icon = effect.icon;
                if (!isBuff)
                {
                    foreach (var buff in savedBuffs)
                    {
                        var specialSprite = buff.GetSpecialSpriteForDebuff((effect as DebuffSO).Type, BodyPart.Head);
                        if (specialSprite != null)
                        {
                            icon = specialSprite;
                            break;
                        }
                    }
                }

                CreateEffectPanel(icon, effect.name, effect.description, isBuff);
            }
        }

        private void CreateEffectPanel(Sprite icon, string title, string description, bool isBuff)
        {
            if (effectPrefab == null || effectContainer == null) return;

            var effectPanel = Instantiate(effectPrefab, effectContainer);
            spawnedEffects.Add(effectPanel);

            var effectUI = effectPanel.GetComponent<CharacterEffectUI>();
            if (effectUI != null)
            {
                effectUI.Setup(icon, title, description, isBuff ? Color.green : Color.red);
            }

            // Positioning logic
            RectTransform effectRect = effectPanel.GetComponent<RectTransform>();
            if (effectRect != null && effectsSpawnPoint != null)
            {
                effectRect.anchoredPosition = effectsSpawnPoint.anchoredPosition + effectsOffset;
                effectsOffset.y -= effectSpacing; // Update offset for next effect
            }
        }

        private void ClearEffects()
        {
            foreach (var effect in spawnedEffects)
            {
                if (effect != null) Destroy(effect);
            }
            spawnedEffects.Clear();
        }

        private void OnDestroy()
        {
            KillTweens();
            button?.onClick.RemoveAllListeners();
            ClearEffects();
            savedBuffs.Clear();
            savedDebuffs.Clear();
        }
    }
}
