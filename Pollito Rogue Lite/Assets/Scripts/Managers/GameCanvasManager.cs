using DG.Tweening;
using Enums;
using Managers;
using Scriptables;
using UI;
using UnityEngine;
using TMPro;

public class GameCanvasManager : MonoBehaviour
{
    [SerializeField] private CharacterSelectorUI _characterSelectorUI;
    [SerializeField] private PuzzleMovesCounterUI _puzzleMovesCounter;
    [SerializeField] private StoredFoodUI _storedFoodUI;
    [SerializeField] private DevelopmentBuffsUI _developmentBuffsUI;
    [SerializeField] private CurrencyUI _currencyUI;
    [SerializeField] private CurrentPowerUI _currentPowerUI;
    [SerializeField] private CurrentDebuffUI _currentDebuffUI;
    [SerializeField] private CurrentDebuffUI _currentDebuffUI2;
    [SerializeField] private LevelSummaryUI _levelSummaryUI;
    [SerializeField] GameObject _gameOverPanel;
    [SerializeField] TextMeshProUGUI _gameOverReasonText;

    [SerializeField] private PlayerController _playerController;
    
    private bool _isGameOverActive = false;
    private bool hide = false;
    
    public DevelopmentBuffsUI DevelopmentBuffsUI => _developmentBuffsUI;
    public CharacterSelectorUI CharacterSelectorUI => _characterSelectorUI;
    public PuzzleMovesCounterUI PuzzleMovesCounter => _puzzleMovesCounter;
    public StoredFoodUI StoredFoodUI => _storedFoodUI;
    public CurrencyUI CurrencyUI => _currencyUI;
    public CurrentPowerUI CurrentPowerUI => _currentPowerUI;
    public CurrentDebuffUI CurrentDebuffUI => _currentDebuffUI;
    public CurrentDebuffUI CurrentDebuffUI2 => _currentDebuffUI2;
    public LevelSummaryUI LevelSummaryUI => _levelSummaryUI;

    // Posiciones configurables desde el inspector
    private Vector2 _leftUIPosition = new Vector2(-225f, -16f);
    private Vector2 _rightUIPosition = new Vector2(242f, -16f);

    //Cuando son tres 
    private Vector2 _leftPosition = new Vector2(-328f, -16f);
    private Vector2 _mediumPosition = new Vector2(0f,-16f);
    private Vector2 _rightPosition = new Vector2(338f, -16f);
    public void ResetGame()
    {
        FadeToBlackAndReset();
    }

    private void Update()
    {
        if (_playerController == null) _playerController = FindObjectOfType<PlayerController>();
        UpdateCurrentDebuffUI();
        //else ClearCurrentDebuffUI();
        // Permite volver al hub presionando la barra espaciadora cuando el panel de Game Over está activo
        if (_isGameOverActive && _gameOverPanel != null && _gameOverPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("[GameCanvasManager] Space key pressed on game over screen - returning to hub");
                HideGameOverPanel();
            }
        }
    }
    
    /// <summary>
    /// Muestra el panel de Game Over con animación y mensaje según la causa de la muerte
    /// </summary>
    /// <param name="reason">The reason for the player's death</param>
    public void ShowGameOverPanel(DeathReason reason = DeathReason.Unknown)
    {
        if (_gameOverPanel != null)
        {
            Debug.Log($"[GameCanvasManager] Showing game over panel with animation. Death reason: {reason}");

            // Activa el panel y lo escala desde 0 (efecto de aparicion)
            _gameOverPanel.SetActive(true);
            _gameOverPanel.transform.localScale = Vector3.zero;
            ClearCurrentDebuffUI();
            _puzzleMovesCounter.gameObject.SetActive(false);
            _storedFoodUI.gameObject.SetActive(false);
            _characterSelectorUI.gameObject.SetActive(false);
            _developmentBuffsUI.gameObject.SetActive(false);
            // Oculta otros elementos de la UI
            if (_currentPowerUI != null) _currentPowerUI.gameObject.SetActive(false);
            if (_currencyUI != null) _currencyUI.gameObject.SetActive(false);
            // Muestra la razon de la derrota
            if (_gameOverReasonText != null)
            {
                switch (reason)
                {
                    case DeathReason.Moves:
                        _gameOverReasonText.text = "You ran out of moves!";
                        break;
                    case DeathReason.Void:
                        _gameOverReasonText.text = "You fell into the void!";
                        break;
                    case DeathReason.FragileTile:
                        _gameOverReasonText.text = "The fragile floor broke beneath you!";
                        break;
                    case DeathReason.Unknown:
                    default:
                        _gameOverReasonText.text = "Game Over";
                        break;
                }
            }

            // Activa el flag de estado
            _isGameOverActive = true;

            // Animacion de aparicion (escala de 0 a 1)
            _gameOverPanel.transform
                .DOScale(Vector3.one, 0.4f)
                .SetEase(Ease.OutBack)
                .OnComplete(() => {
                    Debug.Log("[GameCanvasManager] Game over panel animation completed");
                });
        }
        else Debug.LogError("[GameCanvasManager] Game over panel is null");
    }
    // Oculta el panel de GameOver con animacion y luego reinicia
    public void HideGameOverPanel()
    {
        if (_gameOverPanel != null)
        {
            Debug.Log("[GameCanvasManager] Hiding game over panel with animation");
            
            // Disable input handling
            _isGameOverActive = false;
            hide = true;
            // Animacion de desaparicion (escala a 0)
            _gameOverPanel.transform
                .DOScale(Vector3.zero, 0.3f)
                .SetEase(Ease.InBack)
                .OnComplete(() => {
                    Debug.Log("[GameCanvasManager] Game over panel hide animation completed");
                    // Don't set panel inactive here, do it during fade transition
                    FadeToBlackAndReset();
                });
        }
        else Debug.LogError("[GameCanvasManager] Game over panel is null");
    }
    //Oculta el panel y carga el hub
    private void FadeToBlackAndReset()
    {
        // Hide the game over panel if it's visible
        if (_gameOverPanel != null && _gameOverPanel.activeSelf)
        {
            _gameOverPanel.SetActive(false);
            Debug.Log("[GameCanvasManager] Game over panel hidden");
        }

        // Use the LoadSceneManager for the transition
        Debug.Log("[GameCanvasManager] Loading hub scene");
        GameManager.Instance.LoadHub();
    }

    // Aca añadir para que se muestren los otros cuando hay más activos
    /// <summary>
    /// Actualiza el UI del poder actual con el buff activo
    /// </summary>
    /// <param name="buff">El buff actual activo</param>
    public void UpdateCurrentPowerUI(BuffSO buff)
    {
        if (_currentPowerUI != null) _currentPowerUI.UpdateBuffDisplay(buff);
        else Debug.LogError("[GameCanvasManager] Current Power UI is null");
    }

    /// <summary>
    /// Limpia el UI del poder actual cuando no hay buff activo
    /// </summary>
    public void ClearCurrentPowerUI()
    {
        if (_currentPowerUI != null) _currentPowerUI.ClearBuffDisplay();
    }
    /// Actualiza el UI del debuff actual con el debuff activo
    /// </summary>
    /// <param name="debuff">El debuff actual activo</param>
    public void UpdateCurrentDebuffUI()
    {
        if (_isGameOverActive == false && hide == false) 
        { 
            if (_playerController._isControlsInverted == false && _playerController.blinded == true) 
            {
            _currentPowerUI.GetComponent<RectTransform>().anchoredPosition = _leftUIPosition;
            _currentDebuffUI.GetComponent<RectTransform>().anchoredPosition = _rightUIPosition;
            _currentDebuffUI.UpdateDebuffDisplay();
            }
            if (_playerController._isControlsInverted == true && _playerController.blinded == false) 
            {
            _currentPowerUI.GetComponent<RectTransform>().anchoredPosition = _rightUIPosition;
            _currentDebuffUI2.GetComponent<RectTransform>().anchoredPosition = _leftUIPosition;
            _currentDebuffUI2.UpdateDebuffDisplay();
            }
            if (_playerController.debuff == true)
            {
            RestorePositions();
            _currentDebuffUI.UpdateDebuffDisplay();
            _currentDebuffUI2.UpdateDebuffDisplay();
            }
        }
    }

    /// <summary>
    /// Limpia el UI del debuff actual cuando no hay debuff activo
    /// </summary>
    public void ClearCurrentDebuffUI()
    {
        _currentDebuffUI.ClearDebuffDisplay();
        _currentDebuffUI2.ClearDebuffDisplay();
    }
    private void RestorePositions() 
    {
        _currentPowerUI.GetComponent<RectTransform>().anchoredPosition = _mediumPosition;
        _currentDebuffUI.GetComponent<RectTransform>().anchoredPosition = _rightPosition;
        _currentDebuffUI2.GetComponent<RectTransform>().anchoredPosition = _leftPosition;
    }
        
    
}
