using Managers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Player;
using DG.Tweening;
using Audio;

namespace Hub
{
    public class HubExitButton : MonoBehaviour
    {
        [SerializeField] private KeyCode _activationKey = KeyCode.E;
        [SerializeField] private float _holdDuration = 1.0f; // Slightly faster than play button
        [SerializeField] private UnityEvent _onHoldComplete;

        [Header("Visual References")]
        [SerializeField] private Transform _borderTransform; // Transform for position/scale
        [SerializeField] private SpriteRenderer _borderSprite; // SpriteRenderer for color

        [Header("Visual Feedback")]
        [SerializeField] private float _pulseScale = 1.05f;
        [SerializeField] private float _pulseDuration = 0.3f;
        [SerializeField] private float _shakeStrength = 0.2f;
        [SerializeField] private int _shakeVibrato = 10;
        [SerializeField] private float _exitShakeStrength = 0.15f;
        
        [Header("Pulse Settings")]
        [SerializeField] private float _scalePulseSpeed = 0.8f;
        [SerializeField] private float _colorPulseSpeed = 0.8f;

        [Header("Colors")]
        [SerializeField] private Color _defaultColor = Color.white;
        [SerializeField] private Color _exitRedColor = new Color(1f, 0.3f, 0.3f); // Brighter red for exit
        [SerializeField] private Color _cancelBlueColor = new Color(0.3f, 0.5f, 1f);
        [SerializeField] private Color _pulseGrayColor1 = new Color(0.7f, 0.7f, 0.7f);
        [SerializeField] private Color _pulseGrayColor2 = new Color(0.6f, 0.6f, 0.6f);
        [SerializeField] private float _colorTransitionSpeed = 0.2f; // Faster color transition

        [Header("Exit Transition")]
        [SerializeField] private float _exitFadeDuration = 0.4f; // Duration of fade to black
        [SerializeField] private float _exitHoldDuration = 0.15f; // Time to hold at black before exiting

        [Header("Audio")]
        [SerializeField] private AudioClip _buttonPressedSound;
        private AudioSource _audioSource;

        private PlayerController _playerController;
        private bool _playerInTriggerArea;
        private bool _isHolding;
        private float _currentHoldTime;
        private Sequence _feedbackSequence;
        private Sequence _colorSequence;
        private Vector3 _originalBorderPosition;
        private bool _isExiting = false;

        public float HoldProgress => _currentHoldTime / _holdDuration;

        private void Start()
        {
            GetReferences();
            if (_borderTransform != null) _originalBorderPosition = _borderTransform.localPosition;
            if (_borderSprite != null) _borderSprite.color = _defaultColor;
            // Asegurar que el AudioSource exista
            if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
            Debug.Log("[HubExitButton] Initialized");
        }

        private void GetReferences()
        {
            if (GameManager.Instance != null && GameManager.Instance.HubManager != null)
            {
                _playerController = GameManager.Instance.HubManager.PlayerController;
                Debug.Log(_playerController != null
                    ? $"[HubExitButton] PlayerController found from HubManager: {_playerController.name}"
                    : "[HubExitButton] PlayerController not found in HubManager");
            }
            else
            {
                Debug.LogWarning("[HubExitButton] GameManager or HubManager not available");
            }

            // Find border transform if not assigned
            if (_borderTransform == null)
            {
                _borderTransform = transform.Find("Border");
                if (_borderTransform == null)
                    _borderTransform = GetComponentInChildren<Transform>();

                Debug.Log(_borderTransform != null
                    ? $"[HubExitButton] Border transform found: {_borderTransform.name}"
                    : "[HubExitButton] No border transform found in children");
            }

            // Find sprite renderer if not assigned
            if (_borderSprite == null && _borderTransform != null)
            {
                _borderSprite = _borderTransform.GetComponent<SpriteRenderer>();
                if (_borderSprite == null)
                    _borderSprite = GetComponentInChildren<SpriteRenderer>();

                Debug.Log(_borderSprite != null
                    ? "[HubExitButton] Border sprite renderer found"
                    : "[HubExitButton] No sprite renderer found");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_playerController == null)
            {
                _playerController = other.GetComponent<PlayerController>();
                if (_playerController != null) Debug.Log($"[HubExitButton] PlayerController found from trigger: {_playerController.name}");
            }
            if (_playerController == null || other.gameObject != _playerController.gameObject) return;
            _playerInTriggerArea = true;
            Debug.Log("[HubExitButton] Player entered trigger area");
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
            Debug.Log("[HubExitButton] Player exited trigger area");
            FlashBlueColor();
            if (_borderTransform != null) ShakeOnExit();
        }

        private void Update()
        {
            if (_isExiting) return; // Don't process input if already exiting
            if (_playerController == null) GetReferences();
            if (!_playerInTriggerArea || _playerController == null) return;
            if (Input.GetKey(_activationKey))
            {
                if (!_isHolding)
                {
                    _isHolding = true;
                    Debug.Log("[HubExitButton] Player started holding action button");
                    //Añadir acá para que reproduzca el efecto de sonido
                    _audioSource.clip = _buttonPressedSound;
                    _audioSource.Play();
                    // Play button press sound when starting to hold
                    if (GameManager.Instance?.AudioManager != null) GameManager.Instance.AudioManager.PlayButtonPress();
                    TweenToRedColor();
                }
                _currentHoldTime += Time.deltaTime;
                // Update border visual based on hold progress
                UpdateBorderVisual();
                if (_currentHoldTime >= _holdDuration) InvokeExitGame();
            }
            else if (_isHolding && _currentHoldTime < _holdDuration)
            {
                // Only reset if not completed
                Debug.Log("[HubExitButton] Player released button before completion");
                ResetHoldProgress();
                _audioSource.Stop();
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

        private void InvokeExitGame()
        {
            if (_isExiting) return; // Prevent multiple invocations
            _isExiting = true;
            
            Debug.Log("[HubExitButton] Hold completed - Invoking exit game event");

            // Play menu close sound when exiting (more appropriate than error sound)
            if (GameManager.Instance?.AudioManager != null)
                GameManager.Instance.AudioManager.PlayMenuClose();

            // Play exit feedback animation
            if (_borderTransform != null)
                ShakeBorder();

            _onHoldComplete?.Invoke();

            // Don't reset hold progress or visual state - stay in completed state
            _isHolding = false; // Just reset the holding flag but keep visuals
            _currentHoldTime = _holdDuration; // Keep at max

            // Kill any ongoing color tweens to freeze at current color
            _colorSequence?.Kill();
            
            // Start the fade-to-black exit sequence using DOTween
            FadeToBlackAndExit();
        }

        /// <summary>
        /// Creates a DOTween sequence to fade to black and then exit the application
        /// </summary>
        private void FadeToBlackAndExit()
        {
            // Get the LoadSceneManager instance
            LoadSceneManager fadeManager = LoadSceneManager.Instance;
            
            if (fadeManager == null)
            {
                // No fade manager available, exit immediately
                Debug.LogWarning("[HubExitButton] LoadSceneManager not found, exiting without transition");
                ExitApplication();
                return;
            }

            // Use the LoadSceneManager's built-in transition system
            Canvas transitionCanvas = fadeManager.TransitionCanvas;
            Image transitionImage = fadeManager.TransitionImage;
            
            if (transitionCanvas == null || transitionImage == null)
            {
                // Transition elements not available, exit immediately
                Debug.LogWarning("[HubExitButton] Transition canvas or image not found, exiting without transition");
                ExitApplication();
                return;
            }
            
            // Enable the transition animation
            fadeManager.SetUseTransitionAnimations(true);
            
            // Enable the canvas and make sure image is visible
            transitionCanvas.gameObject.SetActive(true);
            transitionImage.gameObject.SetActive(true);
            
            // Start with transparent black
            transitionImage.color = new Color(0, 0, 0, 0);
            
            Debug.Log("[HubExitButton] Starting fade to black animation");

            // Kill any existing tweens on this image
            DOTween.Kill(transitionImage);
            
            // Create a sequence for the exit fade
            Sequence exitSequence = DOTween.Sequence();
            exitSequence.SetUpdate(UpdateType.Normal, true) // Makes the animation run in realtime regardless of timescale
                       .SetId("ExitSequence");
                       
            // Add the fade to black animation
            exitSequence.Append(transitionImage.DOFade(1f, _exitFadeDuration).SetEase(Ease.InOutQuad));
            
            // Add a brief pause at black
            exitSequence.AppendInterval(_exitHoldDuration);
            
            // Add the exit application callback after the fade is complete
            exitSequence.AppendCallback(() => {
                Debug.Log("[HubExitButton] Exit animation completed, quitting application");
                ExitApplication();
            });
            
            // If somehow the sequence fails, have a safety timeout
            DOVirtual.DelayedCall(2f, () => {
                if (_isExiting) {
                    Debug.LogWarning("[HubExitButton] Exit animation timed out, forcing quit");
                    ExitApplication();
                }
            }).SetUpdate(UpdateType.Normal, true);
        }

        /// <summary>
        /// Exits the application immediately
        /// </summary>
        private void ExitApplication()
        {
            Debug.Log("[HubExitButton] Exiting application");
            
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        #region Visual Feedback Methods
        private void UpdateBorderVisual()
        {
            if (_borderTransform == null) return;

            // Cancel any running animations
            _feedbackSequence?.Kill();

            // Smoothly scale the border based on hold progress
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
        private void TweenToRedColor()
        {
            if (_borderSprite == null) return;

            _colorSequence?.Kill();
            _colorSequence = DOTween.Sequence();
            _colorSequence.Append(_borderSprite.DOColor(_exitRedColor, _holdDuration));
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

        private void FlashBlueColor()
        {
            if (_borderSprite == null) return;

            _colorSequence?.Kill();

            _colorSequence = DOTween.Sequence();
            _colorSequence.Append(_borderSprite.DOColor(_cancelBlueColor, _colorTransitionSpeed / 2))
                .Append(_borderSprite.DOColor(_defaultColor, _colorTransitionSpeed / 2));
        }
        #endregion

        private void OnDestroy()
        {
            // Kill all sequences to prevent memory leaks
            _feedbackSequence?.Kill();
            _colorSequence?.Kill();
        }
    }
}
