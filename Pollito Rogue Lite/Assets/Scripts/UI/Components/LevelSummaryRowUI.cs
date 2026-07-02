using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

namespace UI.Components
{
    public class LevelSummaryRowUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _summaryStatName;
        [SerializeField] private TMP_Text _summaryStatMultiplier;
        [SerializeField] private TMP_Text _summaryStatTotal;
        [SerializeField] private Image _backgroundImage;

        [Header("Animation Settings")]
        [SerializeField] private float _animationDuration = 0.5f;
        [SerializeField] private float _textAppearDuration = 0.4f;
        [SerializeField] private float _multiplierAppearDuration = 0.2f;
        [SerializeField] private float _countingDuration = 0.5f;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _highlightColor = new Color(0.9f, 0.9f, 0.5f);
        [SerializeField] private Ease _numberCountEase = Ease.OutQuad;
        [SerializeField] private Ease _textAppearEase = Ease.OutQuint;
        [SerializeField] private Vector3 _punchScale = new Vector3(0.2f, 0.2f, 0.2f);

        private int _multiplier;
        private float _baseValue;
        private float _finalValue;
        private string _statName;
        private bool _isAnimating = false;
        private Sequence _currentSequence;

        // Public property to access the final value
        public int FinalValue => Mathf.RoundToInt(_finalValue);

        public bool IsAnimating => _isAnimating;

        private void Awake()
        {
            if (_backgroundImage == null)
                _backgroundImage = GetComponent<Image>();
                
            // Make sure text components have their references
            if (_summaryStatName == null)
                _summaryStatName = transform.Find("StatName")?.GetComponent<TMP_Text>();
                
            if (_summaryStatMultiplier == null)
                _summaryStatMultiplier = transform.Find("Multiplier")?.GetComponent<TMP_Text>();
                
            if (_summaryStatTotal == null)
                _summaryStatTotal = transform.Find("Total")?.GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            // Make sure DOTween is properly initialized for this instance
            DOTween.Init();
        }

        private void OnDisable()
        {
            // Kill any active animations when disabled
            if (_currentSequence != null && _currentSequence.IsActive())
            {
                _currentSequence.Kill();
                _currentSequence = null;
            }
        }

        /// <summary>
        /// Updates the UI with the level summary information
        /// </summary>
        public void SetupRow(string statName, float baseValue, int multiplier)
        {
            // Store values for animation
            _statName = statName;
            _multiplier = multiplier;
            _baseValue = baseValue;
            _finalValue = baseValue * multiplier;
            
            // Only set text if component references exist
            if (_summaryStatName != null)
                _summaryStatName.text = _statName;
                
            if (_summaryStatMultiplier != null)
                _summaryStatMultiplier.text = $"x{_multiplier}";
                
            if (_summaryStatTotal != null)
                _summaryStatTotal.text = "0";
            
            // Reset colors and scales
            if (_backgroundImage != null)
                _backgroundImage.color = _normalColor;
                
            // Reset scales to ensure proper animation
            if (_summaryStatName != null)
                _summaryStatName.transform.localScale = Vector3.one;
                
            if (_summaryStatMultiplier != null)
                _summaryStatMultiplier.transform.localScale = Vector3.one;
                
            if (_summaryStatTotal != null)
                _summaryStatTotal.transform.localScale = Vector3.one;
                
            // Kill any existing animation
            if (_currentSequence != null && _currentSequence.IsActive())
            {
                _currentSequence.Kill();
                _currentSequence = null;
            }
        }
        
        /// <summary>
        /// Sets up the row specifically for total earned value (starting from current currency)
        /// </summary>
        public void SetupTotalRow(string statName, float startValue, float earnedValue)
        {
            // Store values for animation
            _statName = statName;
            _baseValue = startValue;        // Current currency
            _finalValue = startValue + earnedValue;  // Current + earned
            _multiplier = 1;
            
            // Only set text if component references exist
            if (_summaryStatName != null)
                _summaryStatName.text = _statName;
                
            if (_summaryStatMultiplier != null)
                _summaryStatMultiplier.text = "";  // No multiplier for total row
                
            if (_summaryStatTotal != null)
                _summaryStatTotal.text = Mathf.RoundToInt(_baseValue).ToString();  // Start at current value
            
            // Reset colors and scales
            if (_backgroundImage != null)
                _backgroundImage.color = _normalColor;
                
            // Reset scales to ensure proper animation
            if (_summaryStatName != null)
                _summaryStatName.transform.localScale = Vector3.one;
                
            if (_summaryStatMultiplier != null)
                _summaryStatMultiplier.transform.localScale = Vector3.one;
                
            if (_summaryStatTotal != null)
                _summaryStatTotal.transform.localScale = Vector3.one;
                
            // Kill any existing animation
            if (_currentSequence != null && _currentSequence.IsActive())
            {
                _currentSequence.Kill();
                _currentSequence = null;
            }
        }

        /// <summary>
        /// Animates the row showing the calculations and final value in sequence
        /// </summary>
        public Sequence AnimateRow(Action onComplete = null)
        {
            Debug.Log($"[LevelSummaryRowUI] Starting animation for: {_statName}");
            _isAnimating = true;
            
            // Kill any existing animation
            if (_currentSequence != null && _currentSequence.IsActive())
            {
                _currentSequence.Kill();
            }
            
            // Create sequence for all animations
            _currentSequence = DOTween.Sequence();
            
            // First highlight the row
            if (_backgroundImage != null)
                _currentSequence.Append(_backgroundImage.DOColor(_highlightColor, _animationDuration * 0.25f));
            
            // Simple animation for the stat name (with punchscale since typing effect isn't working)
            if (_summaryStatName != null)
            {
                _currentSequence.Append(_summaryStatName.transform.DOPunchScale(_punchScale, 0.4f, 3, 0.5f));
                Debug.Log($"[LevelSummaryRowUI] Animating name: {_statName}");
            }
            
            // Only animate multiplier if it has text
            if (_summaryStatMultiplier != null && !string.IsNullOrEmpty(_summaryStatMultiplier.text))
            {
                // Hide and then scale up
                _multiplierAppearDuration = 0.25f;
                _summaryStatMultiplier.transform.localScale = Vector3.zero;
                _currentSequence.Append(_summaryStatMultiplier.transform.DOScale(1, _multiplierAppearDuration).SetEase(Ease.OutBack));
                Debug.Log("[LevelSummaryRowUI] Animating multiplier");
            }
            
            // Animate the counting directly (more reliable than tweening callbacks)
            if (_summaryStatTotal != null)
            {
                float startValue = float.TryParse(_summaryStatTotal.text, out float parsedValue) ? parsedValue : _baseValue;
                
                // Animate from current value to the final value
                _currentSequence.Append(
                    DOTween.To(
                        () => startValue,
                        x => {
                            startValue = x;
                            _summaryStatTotal.text = Mathf.RoundToInt(x).ToString();
                        },
                        _finalValue,
                        _countingDuration
                    ).SetEase(_numberCountEase)
                );
                
                Debug.Log($"[LevelSummaryRowUI] Animating total from {startValue} to {_finalValue}");
            }
            
            // Return to normal color
            if (_backgroundImage != null)
                _currentSequence.Append(_backgroundImage.DOColor(_normalColor, _animationDuration * 0.25f));
            
            // Set completion callback
            _currentSequence.OnComplete(() => 
            {
                _isAnimating = false;
                Debug.Log($"[LevelSummaryRowUI] Animation sequence complete for: {_statName}");
                onComplete?.Invoke();
            });
            
            return _currentSequence;
        }

        /// <summary>
        /// Skip the animation and set the final values immediately
        /// </summary>
        public void InstantComplete()
        {
            // Kill any existing animation
            if (_currentSequence != null && _currentSequence.IsActive())
            {
                _currentSequence.Kill();
                _currentSequence = null;
            }
            
            if (_backgroundImage != null)
                _backgroundImage.color = _normalColor;
            
            if (_summaryStatName != null)
                _summaryStatName.text = _statName;
                
            if (_summaryStatMultiplier != null) {
                _summaryStatMultiplier.text = $"x{_multiplier}";
                _summaryStatMultiplier.transform.localScale = Vector3.one;
            }
            
            if (_summaryStatTotal != null)
                _summaryStatTotal.text = Mathf.RoundToInt(_finalValue).ToString();
            
            _isAnimating = false;
        }
        
        /// <summary>
        /// Highlights this row to indicate it's currently focused
        /// </summary>
        public void Highlight(bool highlight = true)
        {
            if (_backgroundImage != null)
            {
                _backgroundImage.color = highlight ? _highlightColor : _normalColor;
            }
        }

        /// <summary>
        /// Clears all text fields to empty strings
        /// </summary>
        public void ClearTexts()
        {
            // Only clear text if component references exist
            if (_summaryStatName != null)
                _summaryStatName.text = "";
                
            if (_summaryStatMultiplier != null)
                _summaryStatMultiplier.text = "";
                
            if (_summaryStatTotal != null)
                _summaryStatTotal.text = "";
        }

        /// <summary>
        /// Returns the total text component for animation purposes
        /// </summary>
        public TMP_Text GetTotalText() => _summaryStatTotal;
    }
}
