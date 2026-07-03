using Managers;
using UnityEngine;
using UnityEngine.Events;
using Player;
using DG.Tweening;
using Audio;
namespace Hub
{
    public class HubPlayButton : MonoBehaviour
    {
        [SerializeField] private KeyCode _activationKey = KeyCode.E;
        [SerializeField] private float _holdDuration = 1.5f;
        [SerializeField] private UnityEvent _onHoldComplete;

        [Header("Visual References")]
        [SerializeField] private Transform _borderTransform; // Transform for position/scale
        [SerializeField] private SpriteRenderer _borderSprite; // SpriteRenderer for color

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

        public float HoldProgress => _currentHoldTime / _holdDuration;

        private void Start()
        {
            GetReferences();
            if (_borderTransform != null) _originalBorderPosition = _borderTransform.localPosition;
            if (_borderSprite != null) _borderSprite.color = _defaultColor;
            // Asegurar que el AudioSource exista
            if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
            Debug.Log("[HubPlayButton] Initialized");
        }

        private void GetReferences()
        {
            if (GameManager.Instance != null && GameManager.Instance.HubManager != null)
            {
                _playerController = GameManager.Instance.HubManager.PlayerController;
                Debug.Log(_playerController != null
                    ? $"[HubPlayButton] PlayerController found from HubManager: {_playerController.name}"
                    : "[HubPlayButton] PlayerController not found in HubManager");
            }
            else Debug.LogWarning("[HubPlayButton] GameManager or HubManager not available");
            // Find border transform if not assigned
            if (_borderTransform == null)
            {
                _borderTransform = transform.Find("Border");
                if (_borderTransform == null)
                    _borderTransform = GetComponentInChildren<Transform>();

                Debug.Log(_borderTransform != null
                    ? $"[HubPlayButton] Border transform found: {_borderTransform.name}"
                    : "[HubPlayButton] No border transform found in children");
            }

            // Find sprite renderer if not assigned
            if (_borderSprite == null && _borderTransform != null)
            {
                _borderSprite = _borderTransform.GetComponent<SpriteRenderer>();
                if (_borderSprite == null)
                    _borderSprite = GetComponentInChildren<SpriteRenderer>();

                Debug.Log(_borderSprite != null
                    ? "[HubPlayButton] Border sprite renderer found"
                    : "[HubPlayButton] No sprite renderer found");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_playerController == null)
            {
                _playerController = other.GetComponent<PlayerController>();
                if (_playerController != null) Debug.Log($"[HubPlayButton] PlayerController found from trigger: {_playerController.name}");
            }
            if (_playerController == null || other.gameObject != _playerController.gameObject) return;
            _playerInTriggerArea = true;
            Debug.Log("[HubPlayButton] Player entered trigger area");
            // Play button hover sound
            if (GameManager.Instance?.AudioManager != null) GameManager.Instance.AudioManager.PlayButtonHover();
            PulseGrayColor();
            if (_borderTransform != null) PulseBorder();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (_playerController == null || other.gameObject != _playerController.gameObject) return;
            _playerInTriggerArea = false;
            ResetHoldProgress();
            Debug.Log("[HubPlayButton] Player exited trigger area");
            FlashRedColor();
            if (_borderTransform != null) ShakeOnExit();
        }

        private void Update()
        {
            if (_playerController == null) GetReferences();
            if (!_playerInTriggerArea || _playerController == null) return;
            if (Input.GetKey(_activationKey))
            {
                if (!_isHolding)
                {
                    _isHolding = true;
                    Debug.Log("[HubPlayButton] Player started holding action button");
                    //Añadir acá para que reproduzca el efecto de sonido
                    AkSoundEngine.PostEvent("PlayButton", gameObject);
                    TweenToGreenColor();
                }
                _currentHoldTime += Time.deltaTime;
                // Update border visual based on hold progress
                UpdateBorderVisual();
                if (_currentHoldTime >= _holdDuration) InvokeStartPlay();
            }
            else if (_isHolding && _currentHoldTime < _holdDuration)
            {
                // Only reset if not completed
                Debug.Log("[HubPlayButton] Player released button before completion");
                ResetHoldProgress();
                // Reset border visual and color
                ResetBorderVisual();
                PulseGrayColor();
            }
        }

        private void ResetHoldProgress()
        {
            _isHolding = false;
            _currentHoldTime = 0f;
        }

        private void InvokeStartPlay()
        {
            Debug.Log("[HubPlayButton] Hold completed - Invoking play event");

            // Play success sound when action completes
            if (GameManager.Instance?.AudioManager != null)
                GameManager.Instance.AudioManager.PlaySuccessSound();

            // Play success feedback animation
            if (_borderTransform != null)
                ShakeBorder();

            _onHoldComplete?.Invoke();

            // Don't reset hold progress or visual state - stay in completed state
            _isHolding = false; // Just reset the holding flag but keep visuals
            _currentHoldTime = _holdDuration; // Keep at max

            // Kill any ongoing color tweens to freeze at current color
            _colorSequence?.Kill();
    
            // Call GameManager to load the first level
            if (GameManager.Instance != null)
            {
                Debug.Log("[HubPlayButton] Requesting GameManager to initialize with the first level");
                GameManager.Instance.InitializeWithFirstLevel();
            }
            else
            {
                Debug.LogError("[HubPlayButton] GameManager instance not found, cannot load level");
            }
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

            // Very short, subtle shake
            float minimalShakeStrength = 0.05f;
            int minimalVibrato = 2; 
            float veryShortDuration = 0.15f;

            _feedbackSequence = DOTween.Sequence();
            _feedbackSequence.Append(_borderTransform.DOShakePosition(veryShortDuration, minimalShakeStrength, minimalVibrato))
                .AppendCallback(() => {
                    _borderTransform.localScale = Vector3.one;
                    _borderTransform.localPosition = _originalBorderPosition; // Ensure it returns to original position
                });
        }

        // Color tweening methods

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
    
            // Start directly with the first pulse gray color instead of default white
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

        private void OnDestroy()
        {
            // Kill all sequences to prevent memory leaks
            _feedbackSequence?.Kill();
            _colorSequence?.Kill();
        }
    }
}