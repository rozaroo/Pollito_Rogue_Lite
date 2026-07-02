using Managers;
using UnityEngine;
using UnityEngine.Events;
using Player;
using DG.Tweening;
using Interfaces.Interfaces;
using Scriptables.Abilities;
using Scriptables.Upgrades;
using TMPro;
using UnityEngine.UI;
using Audio;

namespace Hub
{
    public class HubUpgradeButton : MonoBehaviour
    {
        [SerializeField] private KeyCode _activationKey = KeyCode.E;
        [SerializeField] private float _holdDuration = 1.5f;
        [SerializeField] private UnityEvent _onHoldComplete;

        [Header("References")]
        [SerializeField] private UpgradableReference _upgradableRef;
        [SerializeField] private StoreManager _storeManager;

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private TextMeshProUGUI _effectValueText;
        [SerializeField] private Image _iconImage;

        [Header("Visual References")]
        [SerializeField] private Transform _borderTransform;
        [SerializeField] private SpriteRenderer _borderSprite;

        [Header("Visual Feedback")]
        [SerializeField] private float _pulseScale = 1.05f;
        [SerializeField] private float _pulseDuration = 0.3f;
        [SerializeField] private float _shakeStrength = 0.2f;
        [SerializeField] private int _shakeVibrato = 10;
        [SerializeField] private float _exitShakeStrength = 0.1f;

        [Header("Pulse Settings")]
        [SerializeField] private float _scalePulseSpeed = 0.8f;
        [SerializeField] private float _colorPulseSpeed = 0.8f;

        [Header("Colors")]
        [SerializeField] private Color _defaultColor = Color.white;
        [SerializeField] private Color _successGreenColor = Color.green;
        [SerializeField] private Color _exitRedColor = Color.red;
        [SerializeField] private Color _insufficientFundsColor = Color.yellow;
        [SerializeField] private Color _pulseGrayColor1 = new Color(0.7f, 0.7f, 0.7f);
        [SerializeField] private Color _pulseGrayColor2 = new Color(0.6f, 0.6f, 0.6f);
        [SerializeField] private float _colorTransitionSpeed = 0.3f;

        private PlayerController _playerController;
        private bool _playerInTriggerArea;
        private bool _isHolding;
        private float _currentHoldTime;
        private Sequence _feedbackSequence;
        private Sequence _colorSequence;
        private Vector3 _originalBorderPosition;
        private IUpgradable _upgradable;
        private SpecialAbilitySO _abilityScriptable;
        private bool _canAffordUpgrade;

        public float HoldProgress => _currentHoldTime / _holdDuration;

        public IUpgradable Upgradable => _upgradable;
        
        // Property to check if this ability is at maximum level
        private bool IsAtMaxLevel 
        { 
            get
            {
                if (_upgradable == null || _abilityScriptable == null) return false;
                int currentLevel = _abilityScriptable.UnlockedByDefault ? _abilityScriptable.CurrentTier : 0;
                return currentLevel >= _upgradable.MaxLevel;
            }
        }

        private void Start()
        {
            GetReferences();
            if (_borderTransform != null)
                _originalBorderPosition = _borderTransform.localPosition;

            if (_borderSprite != null)
                _borderSprite.color = _defaultColor;

            // Get the IUpgradable from the reference
            _upgradable = _upgradableRef?.GetUpgradable();
            _abilityScriptable = _upgradable as SpecialAbilitySO;

            // Register this button with the StoreManager
            if (_storeManager == null)
            {
                _storeManager = GameManager.Instance?.GetComponent<StoreManager>();
                if (_storeManager == null)
                    _storeManager = FindObjectOfType<StoreManager>();
            }

            if (_storeManager != null)
            {
                _storeManager.RegisterButton(this);
            }
            else
            {
                Debug.LogWarning("[HubUpgradeButton] No StoreManager found for registration");
            }

            UpdateUI();

            Debug.Log("[HubUpgradeButton] Initialized");
        }

        private void OnDestroy()
        {
            // Unregister from StoreManager
            if (_storeManager != null)
            {
                _storeManager.UnregisterButton(this);
            }

            // Kill all sequences to prevent memory leaks
            _feedbackSequence?.Kill();
            _colorSequence?.Kill();
        }

        private void GetReferences()
        {
            if (GameManager.Instance != null && GameManager.Instance.HubManager != null)
            {
                _playerController = GameManager.Instance.HubManager.PlayerController;
                Debug.Log(_playerController != null
                    ? $"[HubUpgradeButton] PlayerController found from HubManager: {_playerController.name}"
                    : "[HubUpgradeButton] PlayerController not found in HubManager");
            }
            else
            {
                Debug.LogWarning("[HubUpgradeButton] GameManager or HubManager not available");
            }

            // Find border transform if not assigned
            if (_borderTransform == null)
            {
                _borderTransform = transform.Find("Border");
                if (_borderTransform == null)
                    _borderTransform = GetComponentInChildren<Transform>();
            }

            // Find sprite renderer if not assigned
            if (_borderSprite == null && _borderTransform != null)
            {
                _borderSprite = _borderTransform.GetComponent<SpriteRenderer>();
                if (_borderSprite == null)
                    _borderSprite = GetComponentInChildren<SpriteRenderer>();
            }

            // Find StoreManager if not assigned
            if (_storeManager == null)
            {
                _storeManager = GameManager.Instance?.GetComponent<StoreManager>();
                if (_storeManager == null)
                    _storeManager = FindObjectOfType<StoreManager>();
            }
        }

        public void UpdateUI()
        {
            if (_upgradable == null || _abilityScriptable == null) return;

            // Get current level directly from the scriptable
            int currentLevel = _abilityScriptable.UnlockedByDefault ? _abilityScriptable.CurrentTier : 0;
            bool isOwned = currentLevel > 0;
            bool isMaxLevel = currentLevel >= _upgradable.MaxLevel;

            // Set icon
            if (_iconImage != null && _upgradable.Icon != null)
                _iconImage.sprite = _upgradable.Icon;

            // Update name text
            if (_nameText != null)
                _nameText.text = _upgradable.DisplayName;

            // Update level text
            if (_levelText != null)
                _levelText.text = isOwned ? $"Level {currentLevel}/{_upgradable.MaxLevel}" : "Not Purchased";

            // Update cost and effect text
            UpdateCostAndEffect(isOwned, isMaxLevel, currentLevel);

            // Update button state based on if player can afford
            UpdateButtonState(isOwned, isMaxLevel);
        }

        private void UpdateCostAndEffect(bool isOwned, bool isMaxLevel, int currentLevel)
        {
            if (_costText != null)
            {
                if (!isOwned)
                {
                    // Initial purchase
                    int cost = _upgradable.InitialPurchaseCost;
                    _costText.text = $"{cost} coins";
                    
                    // Get player coins and check if we can afford it - just use direct comparison
                    int playerCoins = GameManager.Instance.CurrencyManager.GetCurrentCurrency();
                    _canAffordUpgrade = playerCoins >= cost;

                    // Debug actual coin amount vs cost
                    Debug.Log($"[HubUpgradeButton] Player coins: {playerCoins}, Cost: {cost}, CanAfford: {_canAffordUpgrade}");
                }
                else if (!isMaxLevel)
                {
                    // Upgrade
                    int nextLevel = currentLevel + 1;
                    int upgradeCost = _upgradable.GetUpgradeCostForLevel(nextLevel);
                    _costText.text = $"{upgradeCost} coins";
                    
                    // Get player coins and check if we can afford it - just use direct comparison
                    int playerCoins = GameManager.Instance.CurrencyManager.GetCurrentCurrency();
                    _canAffordUpgrade = playerCoins >= upgradeCost;

                    // Debug actual coin amount vs cost
                    Debug.Log($"[HubUpgradeButton] Player coins: {playerCoins}, Cost: {upgradeCost}, CanAfford: {_canAffordUpgrade}");
                }
                else
                {
                    // Max level reached
                    _costText.text = "MAX LEVEL";
                    _canAffordUpgrade = false;
                }
            }

            // Update effect value text
            if (_effectValueText != null)
            {
                float effectValue = _upgradable.GetEffectValueForLevel(Mathf.Max(1, currentLevel));
                _effectValueText.text = $"Effect: {effectValue:F2}";
            }
        }

        private void UpdateButtonState(bool isOwned, bool isMaxLevel)
        {
            if (_borderSprite == null) return;

            if (isMaxLevel)
            {
                // Max level - use green color
                _borderSprite.color = _successGreenColor;
                _colorSequence?.Kill();
            }
            else if (_playerInTriggerArea)
            {
                // Player is in area, pulse normal colors
                if (_isHolding)
                    TweenToGreenColor();
                else
                    PulseGrayColor();
            }
            else if (!_canAffordUpgrade && !isMaxLevel)
            {
                // Can't afford - use yellow
                _borderSprite.color = _insufficientFundsColor;
                _colorSequence?.Kill();
            }
            else
            {
                // Default state
                _borderSprite.color = _defaultColor;
                _colorSequence?.Kill();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log($"[HubUpgradeButton] Trigger entered by: {other.gameObject.name}");

            // Skip all interactions if upgrade is at max level
            if (IsAtMaxLevel)
            {
                Debug.Log("[HubUpgradeButton] Player entered trigger area but upgrade is at max level - ignoring interaction");
                return;
            }

            if (_playerController == null)
            {
                _playerController = other.GetComponent<PlayerController>();
                if (_playerController != null)
                    Debug.Log($"[HubUpgradeButton] PlayerController found from trigger: {_playerController.name}");
            }

            if (_playerController == null || other.gameObject != _playerController.gameObject)
                return;

            _playerInTriggerArea = true;
            Debug.Log("[HubUpgradeButton] Player entered trigger area");

            // Play button hover sound
            if (GameManager.Instance?.AudioManager != null)
                GameManager.Instance.AudioManager.PlayButtonHover();

            PulseGrayColor();
            if (_borderTransform != null)
                PulseBorder();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (_playerController == null || other.gameObject != _playerController.gameObject)
                return;

            // Skip all exit feedback if upgrade is at max level
            if (IsAtMaxLevel)
            {
                _playerInTriggerArea = false;
                Debug.Log("[HubUpgradeButton] Player exited trigger area but upgrade is at max level - ignoring exit feedback");
                return;
            }

            _playerInTriggerArea = false;
            ResetHoldProgress();
            Debug.Log("[HubUpgradeButton] Player exited trigger area");

            FlashRedColor();
            if (_borderTransform != null)
                ShakeOnExit();

            // Update UI to show correct state when player leaves
            UpdateUI();
        }

        private void Update()
        {
            // Skip all interactions if at max level
            if (IsAtMaxLevel)
            {
                // Ensure we don't have any ongoing animations for max level buttons
                if (_feedbackSequence != null || _colorSequence != null)
                {
                    _feedbackSequence?.Kill();
                    _colorSequence?.Kill();
                    if (_borderSprite != null)
                        _borderSprite.color = _successGreenColor;
                }
                return;
            }

            if (_playerController == null)
            {
                GetReferences();
                Debug.LogWarning("[HubUpgradeButton] Player controller was null, attempted to get references");
            }

            if (!_playerInTriggerArea)
            {
                // Debug.Log("[HubUpgradeButton] Player not in trigger area");
                return;
            }

            if (_playerController == null)
            {
                Debug.LogError("[HubUpgradeButton] PlayerController is still null after GetReferences");
                return;
            }

            if (!_canAffordUpgrade)
            {
                // Only show feedback when the player attempts to purchase
                if (Input.GetKeyDown(_activationKey))
                {
                    Debug.LogWarning("[HubUpgradeButton] Player can't afford upgrade but tried to press the button");
                    int playerCoins = GameManager.Instance.CurrencyManager.GetCurrentCurrency();
                    int cost = 0;
                    
                    // Get the current cost based on if it's initial purchase or upgrade
                    if (_abilityScriptable != null)
                    {
                        int currentLevel = _abilityScriptable.UnlockedByDefault ? _abilityScriptable.CurrentTier : 0;
                        if (currentLevel == 0)
                            cost = _upgradable.InitialPurchaseCost;
                        else
                            cost = _upgradable.GetUpgradeCostForLevel(currentLevel + 1);
                        
                        Debug.LogWarning($"[HubUpgradeButton] Cannot afford: Coins: {playerCoins}, Cost: {cost}");
                    }
                    
                    // Play the insufficient funds feedback only when player tries to purchase
                    ShowInsufficientFundsFeedback();
                }
                return;
            }

            // Debug the key press state
            if (Input.GetKeyDown(_activationKey))
            {
                Debug.Log($"[HubUpgradeButton] Key {_activationKey} pressed down");
            }

            if (Input.GetKey(_activationKey))
            {
                // Add timestamp to see exact time of log to track input detection
                string timestamp = Time.realtimeSinceStartup.ToString("F3");

                if (!_isHolding)
                {
                    _isHolding = true;
                    Debug.Log($"[{timestamp}][HubUpgradeButton] Player started holding action button");
                    
                    // Play button press sound when starting to hold
                    if (GameManager.Instance?.AudioManager != null)
                        GameManager.Instance.AudioManager.PlayButtonPress();
                    
                    TweenToGreenColor();
                }

                _currentHoldTime += Time.deltaTime;
                
                // Log less frequently to avoid spam but still track progress
                if (_currentHoldTime % 0.25f < Time.deltaTime)
                    Debug.Log($"[{timestamp}][HubUpgradeButton] Hold time: {_currentHoldTime:F2}/{_holdDuration}");

                UpdateBorderVisual();

                if (_currentHoldTime >= _holdDuration)
                {
                    Debug.Log($"[{timestamp}][HubUpgradeButton] Hold duration reached ({_currentHoldTime:F2} >= {_holdDuration}), calling PurchaseUpgrade()");
                    PurchaseUpgrade();
                }
            }
            else if (_isHolding && _currentHoldTime < _holdDuration)
            {
                Debug.Log("[HubUpgradeButton] Player released button before completion");
                ResetHoldProgress();
                ResetBorderVisual();
                PulseGrayColor();
            }
        }

        private void PurchaseUpgrade()
        {
            if (_upgradable == null)
            {
                Debug.LogError("[HubUpgradeButton] Missing upgradable reference");
                return;
            }
            
            if (_abilityScriptable == null)
            {
                Debug.LogError("[HubUpgradeButton] Missing ability scriptable reference");
                return;
            }
            
            if (_storeManager == null)
            {
                Debug.LogError("[HubUpgradeButton] Missing store manager reference");
                return;
            }

            Debug.Log($"[HubUpgradeButton] Hold completed - Processing upgrade purchase for {_upgradable.DisplayName}");

            // Verify that we can actually afford this
            int playerCoins = GameManager.Instance.CurrencyManager.GetCurrentCurrency();
            int cost = 0;
            int currentLevel = _abilityScriptable.UnlockedByDefault ? _abilityScriptable.CurrentTier : 0;
            
            if (currentLevel == 0)
                cost = _upgradable.InitialPurchaseCost;
            else
                cost = _upgradable.GetUpgradeCostForLevel(currentLevel + 1);
                
            Debug.Log($"[HubUpgradeButton] Attempting purchase: Level {currentLevel} -> {currentLevel+1}, Cost: {cost}, Coins: {playerCoins}");
                
            // Use the StoreManager to handle the upgrade
            bool success = _storeManager.HandleUpgrade(_upgradable);

            if (success)
            {
                Debug.Log($"[HubUpgradeButton] Successfully upgraded {_upgradable.DisplayName}");
                
                // Play success sound when upgrade is purchased successfully
                if (GameManager.Instance?.AudioManager != null)
                    GameManager.Instance.AudioManager.PlaySuccessSound();
                
                _onHoldComplete?.Invoke();
                
                // Update the UI immediately to reflect the change
                UpdateUI();
            }
            else
            {
                Debug.LogError($"[HubUpgradeButton] Failed to upgrade {_upgradable.DisplayName}. Check StoreManager.HandleUpgrade implementation.");
                
                // Play error sound when upgrade fails
                if (GameManager.Instance?.AudioManager != null)
                    GameManager.Instance.AudioManager.PlayErrorSound();
            }

            // Reset holding state
            ResetHoldProgress();
        }

        private void ResetHoldProgress()
        {
            _isHolding = false;
            _currentHoldTime = 0f;
        }

        private void UpdateBorderVisual()
        {
            if (_borderTransform == null) return;

            // Cancel any running animations
            _feedbackSequence?.Kill();

            // Smoothly scale the border based on hold progress, ensuring it scales from center
            float scaleValue = 1f + (HoldProgress * 0.2f); // Scale from 1 to 1.2 based on progress
            _borderTransform.localScale = Vector3.one * scaleValue;

            // Ensure position is maintained at the original center
            _borderTransform.localPosition = _originalBorderPosition;
        }

        private void ResetBorderVisual()
        {
            if (_borderTransform == null) return;

            _feedbackSequence?.Kill();
            _borderTransform.localScale = Vector3.one;
            _borderTransform.localPosition = _originalBorderPosition;
        }

        private void PulseBorder()
        {
            if (_borderTransform == null) return;

            _feedbackSequence?.Kill();
            _borderTransform.localScale = Vector3.one;
            _borderTransform.localPosition = _originalBorderPosition;

            _feedbackSequence = DOTween.Sequence();
            _feedbackSequence.Append(_borderTransform.DOScale(Vector3.one * _pulseScale, _scalePulseSpeed))
                .Append(_borderTransform.DOScale(Vector3.one, _scalePulseSpeed))
                .SetLoops(-1); // Continuous subtle pulsing
        }

        private void ShakeBorder()
        {
            if (_borderTransform == null) return;

            _feedbackSequence?.Kill();
            _borderTransform.localScale = Vector3.one;
            _borderTransform.localPosition = _originalBorderPosition;

            _feedbackSequence = DOTween.Sequence();
            _feedbackSequence.Append(_borderTransform.DOShakeScale(_pulseDuration, _shakeStrength, _shakeVibrato))
                .AppendCallback(() => {
                    _borderTransform.localScale = Vector3.one;
                    _borderTransform.localPosition = _originalBorderPosition;
                });
        }

        private void ShakeOnExit()
        {
            if (_borderTransform == null) return;

            _feedbackSequence?.Kill();
            _borderTransform.localScale = Vector3.one;
            _borderTransform.localPosition = _originalBorderPosition;

            float minimalShakeStrength = 0.05f;
            int minimalVibrato = 2;
            float veryShortDuration = 0.15f;

            _feedbackSequence = DOTween.Sequence();
            _feedbackSequence.Append(_borderTransform.DOShakePosition(veryShortDuration, minimalShakeStrength, minimalVibrato))
                .AppendCallback(() => {
                    _borderTransform.localScale = Vector3.one;
                    _borderTransform.localPosition = _originalBorderPosition;
                });
        }

        private void TweenToGreenColor()
        {
            if (_borderSprite == null) return;

            _colorSequence?.Kill();
            _colorSequence = DOTween.Sequence();
            _colorSequence.Append(_borderSprite.DOColor(_successGreenColor, _holdDuration));
        }

        private void PulseGrayColor()
        {
            if (_borderSprite == null) return;

            _colorSequence?.Kill();
            _borderSprite.color = _pulseGrayColor1;

            _colorSequence = DOTween.Sequence();
            _colorSequence.Append(_borderSprite.DOColor(_pulseGrayColor2, _colorPulseSpeed))
                .Append(_borderSprite.DOColor(_pulseGrayColor1, _colorPulseSpeed))
                .SetLoops(-1, LoopType.Restart);
        }

        private void FlashRedColor()
        {
            if (_borderSprite == null) return;

            _colorSequence?.Kill();

            _colorSequence = DOTween.Sequence();
            _colorSequence.Append(_borderSprite.DOColor(_exitRedColor, _colorTransitionSpeed / 2))
                .Append(_borderSprite.DOColor(_defaultColor, _colorTransitionSpeed / 2));
        }

        private void ShowInsufficientFundsFeedback()
        {
            // Play error sound when player tries to purchase but can't afford it
            if (GameManager.Instance?.AudioManager != null)
                GameManager.Instance.AudioManager.PlayErrorSound();
            
            // Play a more intense shake effect
            if (_borderTransform != null)
            {
                _feedbackSequence?.Kill();
                _borderTransform.localScale = Vector3.one;
                _borderTransform.localPosition = _originalBorderPosition;

                // Use stronger shake parameters to make it obvious
                float strongShake = _shakeStrength * 1.5f;
                int highVibrato = _shakeVibrato + 5;
                
                _feedbackSequence = DOTween.Sequence();
                _feedbackSequence.Append(_borderTransform.DOShakePosition(_pulseDuration, strongShake, highVibrato))
                    .AppendCallback(() => {
                        _borderTransform.localScale = Vector3.one;
                        _borderTransform.localPosition = _originalBorderPosition;
                    });
            }
            
            // Flash between insufficient funds color and default color
            if (_borderSprite != null)
            {
                _colorSequence?.Kill();
                
                _colorSequence = DOTween.Sequence();
                _colorSequence.Append(_borderSprite.DOColor(_insufficientFundsColor, _colorTransitionSpeed / 3))
                               .Append(_borderSprite.DOColor(Color.red, _colorTransitionSpeed / 3))
                               .Append(_borderSprite.DOColor(_insufficientFundsColor, _colorTransitionSpeed / 3))
                               .Append(_borderSprite.DOColor(_defaultColor, _colorTransitionSpeed));
            }
            
            // Could add UI text flash or notification here if needed
            Debug.Log("[HubUpgradeButton] Showing insufficient funds feedback to player");
        }
    }
}
