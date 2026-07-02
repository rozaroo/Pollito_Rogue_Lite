using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;
using System;
using UI.Components;
using Managers;

namespace UI
{
    //Maneja la pantalla de resumen del nivel (puntos, stats y moneda ganada)
    public class LevelSummaryUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform rowsContainer; //Contenedor de filas (estadisticas)
        [SerializeField] private TextMeshProUGUI totalScoreText; //Texto que muestra el puntaje total
        [SerializeField] private Button continueButton; //Boton para continuar
        [SerializeField] private CanvasGroup mainCanvasGroup; //Grupo para animar fade-in/out de la UI
        [SerializeField] private RectTransform panelRectTransform; //RecTransform del panel principal
        [SerializeField] private CurrencyUI currencyUI; // UI de moneda (oro, puntos, etc)
        [SerializeField] private RectTransform animationParent; // Parent de objetos animados (ej: monedas)

        [Header("Animation Settings")]
        [SerializeField] private float showAnimationDuration = 0.25f;
        [SerializeField] private float delayBetweenRows = 0.25f;
        [SerializeField] private float finalTotalAnimationDuration = 0.75f;
        [SerializeField] private float hideAnimationDuration = 0.5f;
        [SerializeField] private Ease showEase = Ease.OutBack; //Tipo de easing al mostrar
        [SerializeField] private Ease hideEase = Ease.InBack; //Tipo de easing al ocultar

        [Header("Input Settings")]
        [SerializeField] private KeyCode continueKey = KeyCode.Space;
        [SerializeField] private KeyCode skipKey = KeyCode.Escape;

        [SerializeField] private LevelSummaryRowUI totalEarnedRow; //Fila especial de Total ganado
        [SerializeField] private List<LevelSummaryRowUI> rows = new(); //Filas normales de estadisticas
        private int currentRowIndex = -1; //Indice de fila actual en animacion
        private int totalScore = 0; //Puntaje total calculado
        private int displayedScore = 0; //Puntaje mostrado en UI (animado)
        private Action onSummaryComplete; //Callback al terminar el resumen
        private bool animationsComplete = false;
        private bool isTransitioning = false;
        private bool _currencyUpdated = false; //Evita duplicar la animacion de moneda

        private void Awake()
        {
            DOTween.Init();
            InitializeRows();
            // Si no se asigno currencyUI en inspector lo busca en escena
            if (currencyUI == null) currencyUI = FindObjectOfType<CurrencyUI>();
        }
        //Recolecta todas las filas de resumen desde el contenedor
        private void InitializeRows()
        {
            // Reinicia la lista
            rows = new List<LevelSummaryRowUI>();
            
            // Get all LevelSummaryRowUI components from the container
            if (rowsContainer != null)
            {
                var foundRows = rowsContainer.GetComponentsInChildren<LevelSummaryRowUI>(true);
                Debug.Log($"[LevelSummaryUI] Found {foundRows.Length} row components in container");
                
                foreach (var row in foundRows) 
                {
                    if (row != null && row != totalEarnedRow) // Separa la fila de total
                    {
                        rows.Add(row);
                        Debug.Log($"[LevelSummaryUI] Added row: {row.name}");
                    }
                }
            }
            else Debug.LogWarning("[LevelSummaryUI] Row container is null!");
            
            
            // Conectar boton continuar
            continueButton?.onClick.AddListener(HideAndProceed);
            //Asegurarse que CanvasGroup y RectTransform esten
            mainCanvasGroup ??= gameObject.AddComponent<CanvasGroup>();
            panelRectTransform ??= GetComponent<RectTransform>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(skipKey) && !isTransitioning) SkipAnimations();
            if (Input.GetKeyDown(continueKey) && animationsComplete && !isTransitioning) HideAndProceed();
        }
        //Inicializa la UI con los datos de estadisticas del nivel
        public void Initialize(List<StatSummaryData> statData, Action onComplete = null)
        {
            Debug.Log($"[LevelSummaryUI] Initialize called with {statData?.Count ?? 0} stat items");
            
            // Reconfigura filas
            InitializeRows();
            ResetRows(); //Limpia contenido
            onSummaryComplete = onComplete;
            currentRowIndex = -1;
            totalScore = 0;
            displayedScore = 0;
            animationsComplete = false;
            isTransitioning = false;
            _currencyUpdated = false;
            if (totalScoreText != null) totalScoreText.text = "0";
            if (statData == null || statData.Count == 0)
            {
                Debug.LogWarning("[LevelSummaryUI] No stat data provided!");
                return;
            }
                
            // Make sure we have the right number of rows
            Debug.Log($"[LevelSummaryUI] Setting up rows. Data count: {statData.Count}, Row count: {rows.Count}");
            
            // Configura cada fila con datos
            for (int i = 0; i < Mathf.Min(statData.Count, rows.Count); i++)
            {
                if (i >= statData.Count)
                {
                    Debug.LogWarning($"[LevelSummaryUI] Not enough stat data for row {i}");
                    continue;
                }
                
                var stat = statData[i];
                
                if (i >= rows.Count)
                {
                    Debug.LogWarning($"[LevelSummaryUI] Not enough rows for stat data {i}");
                    continue;
                }
                
                var row = rows[i];
                
                if (row == null)
                {
                    Debug.LogError($"[LevelSummaryUI] Row at index {i} is null!");
                    continue;
                }
                
                Debug.Log($"[LevelSummaryUI] Setting up row {i}: {row.name} with data: {stat.statName}");
                
                // Setup row data but keep it inactive until animation
                row.gameObject.SetActive(false);
                row.ClearTexts();
                row.SetupRow(stat.statName, stat.baseValue, (int)stat.multiplier);
                totalScore += (int)stat.finalValue; //Suma puntaje total
            }
            
            // Configura fila de Total Earned
            if (totalEarnedRow != null)
            {
                totalEarnedRow.gameObject.SetActive(false);
                totalEarnedRow.ClearTexts();
                // Use the totalScore directly as the earned amount
                totalEarnedRow.SetupRow("Total Earned", totalScore, 1);
            }
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            gameObject.SetActive(true);
            // Muestra panel y arranca animacion de filas
            ShowWithAnimation(() => AnimateNextRow());
        }

        //Animacion inicial de fade + escala al aparecer
        private void ShowWithAnimation(Action onComplete = null)
        {
            mainCanvasGroup.alpha = 0f;
            panelRectTransform.localScale = Vector3.zero;

            DOTween.Sequence()
                .Join(mainCanvasGroup.DOFade(1f, showAnimationDuration))
                .Join(panelRectTransform.DOScale(Vector3.one, showAnimationDuration).SetEase(showEase))
                .OnComplete(() => onComplete?.Invoke());
        }
        // Muestra cada fila una por una con animacion
        private void AnimateNextRow()
        {
            currentRowIndex++;
            
            Debug.Log($"[LevelSummaryUI] Animating row {currentRowIndex} of {rows.Count} rows");
            
            if (currentRowIndex >= rows.Count)
            {
                // Si ya no quedan filas mostrar total
                if (totalEarnedRow != null)
                {
                    Debug.Log("[LevelSummaryUI] Animating total earned row");
                    // Activate the row just before animating it
                    totalEarnedRow.gameObject.SetActive(true);
                    
                    // Animate and fly currency to UI when complete
                    var sequence = totalEarnedRow.AnimateRow(() => {
                        // Only add currency once
                        if (!_currencyUpdated)
                        {
                            _currencyUpdated = true;
                            AnimateCurrencyToUI(animationParent); // Pass the animation parent reference
                        }
                    }); 
                    sequence?.Play();
                }
                else AnimateTotalScore();
                return;
            }

            var row = rows[currentRowIndex];
            if (row == null)
            {
                Debug.LogError($"[LevelSummaryUI] Row at index {currentRowIndex} is null, skipping to next row");
                AnimateNextRow();
                return;
            }

            Debug.Log($"[LevelSummaryUI] Activating and animating row: {row.name}");
            
            try
            {
                // Activate the row just before animating it
                row.gameObject.SetActive(true);
                // Make sure to return the sequence from AnimateRow to avoid null reference
                var sequence = row.AnimateRow(() => DOVirtual.DelayedCall(delayBetweenRows, AnimateNextRow));
                // Force play the sequence to ensure it starts
                sequence?.Play();
            }
            catch (Exception e)
            {
                Debug.LogError($"[LevelSummaryUI] Error animating row {currentRowIndex}: {e.Message}");
                // Continue with next row if there's an error
                DOVirtual.DelayedCall(delayBetweenRows, AnimateNextRow);
            }
        }
        
        /// <summary>
        /// Animacion de monedas volando desde el total ganado hacia el UI de currency
        /// </summary>
        /// <param name="parentTransform">Optional parent transform for animation objects. If null, will try to find the root canvas.</param>
        private void AnimateCurrencyToUI(RectTransform parentTransform = null)
        {
            if (totalEarnedRow == null || !totalEarnedRow.gameObject.activeSelf || currencyUI == null)
            {
                // Skip to complete state instead of going to AnimateTotalScore
                CompleteAnimation();
                return;
            }
            
            // Get the total earned text
            var totalEarnedText = totalEarnedRow.GetTotalText();
            if (totalEarnedText == null)
            {
                // Skip to complete state instead of going to AnimateTotalScore
                CompleteAnimation();
                return;
            }
            
            // Get the currency text UI
            var currencyText = currencyUI.CurrencyText;
            if (currencyText == null)
            {
                // Skip to complete state instead of going to AnimateTotalScore
                CompleteAnimation();
                return;
            }
            
            // Determinar canvas destino
            RectTransform canvasRect = parentTransform;
            
            // If no parent transform was provided, find the root canvas
            if (canvasRect == null)
            {
                Canvas rootCanvas = null;
                
                // Find the root canvas
                Transform parent = transform;
                while (parent != null)
                {
                    rootCanvas = parent.GetComponent<Canvas>();
                    if (rootCanvas != null && rootCanvas.isRootCanvas)
                    {
                        canvasRect = rootCanvas.GetComponent<RectTransform>();
                        break;
                    }
                    parent = parent.parent;
                }
            }
            
            if (canvasRect == null)
            {
                Debug.LogError("[LevelSummaryUI] Could not find root canvas for animation and no parentTransform provided");
                DOVirtual.DelayedCall(delayBetweenRows, AnimateTotalScore);
                return;
            }
            
            // Crear animaciones de monedas
            int numCoins = Mathf.Min(totalScore / 10, 10); // Cap at 10 coins max
            numCoins = Mathf.Max(numCoins, 3); // At least 3 coins
            
            // Get the rect positions in canvas space
            RectTransform totalEarnedRect = totalEarnedText.GetComponent<RectTransform>();
            RectTransform currencyTextRect = currencyText.GetComponent<RectTransform>();
            
            if (totalEarnedRect == null || currencyTextRect == null)
            {
                DOVirtual.DelayedCall(delayBetweenRows, AnimateTotalScore);
                return;
            }
            
            // Calculate positions in canvas space
            Vector2 startPos;
            Vector2 endPos;
            
            // Get positions in canvas space
            Vector2 totalEarnedCanvasPos;
            Vector2 currencyCanvasPos;
            
            Camera cam = null;
            Canvas canvas = canvasRect.GetComponent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) cam = canvas.worldCamera;
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, 
                RectTransformUtility.WorldToScreenPoint(cam, totalEarnedRect.position),
                cam,
                out totalEarnedCanvasPos);
                
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, 
                RectTransformUtility.WorldToScreenPoint(cam, currencyTextRect.position),
                cam,
                out currencyCanvasPos);
                
            startPos = totalEarnedCanvasPos;
            endPos = currencyCanvasPos;
            
            // Create a sequence for the animation
            Sequence coinSequence = DOTween.Sequence();
            
            // Create a few flying coin particles
            for (int i = 0; i < numCoins; i++)
            {
                // Create a temporary coin image
                GameObject coinObj = new GameObject($"FlyingCoin_{i}");
                coinObj.transform.SetParent(canvasRect);
                
                // Add required components
                RectTransform coinRect = coinObj.AddComponent<RectTransform>();
                Image coinImage = coinObj.AddComponent<Image>();
                
                // Position at start point in canvas space
                coinRect.anchorMin = new Vector2(0.5f, 0.5f);
                coinRect.anchorMax = new Vector2(0.5f, 0.5f);
                coinRect.pivot = new Vector2(0.5f, 0.5f);
                coinRect.anchoredPosition = startPos;
                coinRect.sizeDelta = new Vector2(30, 30);
                
                // Set appropriate sprite
                if (currencyUI.CoinSprite != null) coinImage.sprite = currencyUI.CoinSprite;
                else coinImage.color = Color.yellow; // Fallback if no sprite
                // Add random offset to start position
                Vector2 randomOffset = new Vector2(
                    UnityEngine.Random.Range(-40f, 40f),
                    UnityEngine.Random.Range(-20f, 20f));
                coinRect.anchoredPosition += randomOffset;
                
                // Create bezier path point (in canvas space)
                Vector2 bezierPoint = Vector2.Lerp(startPos, endPos, 0.5f);
                bezierPoint += new Vector2(
                    UnityEngine.Random.Range(-100f, 100f),
                    UnityEngine.Random.Range(50f, 150f));
                
                // Stagger the animations slightly
                float delay = i * 0.1f;
                float duration = UnityEngine.Random.Range(0.6f, 1.0f);
                
                // Create sequence for this coin
                Sequence coinPath = DOTween.Sequence();
                
                // Animacion de escala + movimiento curva + desaparicion
                coinRect.localScale = Vector3.zero;
                coinPath.Append(coinRect.DOScale(1.2f, duration * 0.3f).SetEase(Ease.OutQuad));
                
                // Movement animation using anchoredPosition (not DOPath, which uses world position)
                coinPath.Join(
                    DOTween.To(() => coinRect.anchoredPosition, 
                               x => coinRect.anchoredPosition = x, 
                               bezierPoint, 
                               duration * 0.5f)
                    .SetEase(Ease.OutQuad)
                );
                
                coinPath.Append(
                    DOTween.To(() => coinRect.anchoredPosition, 
                               x => coinRect.anchoredPosition = x, 
                               endPos, 
                               duration * 0.5f)
                    .SetEase(Ease.InQuad)
                );
                
                // Scale down at the end
                coinPath.Join(coinRect.DOScale(0.1f, duration * 0.3f).SetDelay(duration * 0.7f).SetEase(Ease.InQuad));
                
                // Clean up
                coinPath.InsertCallback(duration, () => {
                    Destroy(coinObj);
                });
                
                // Add to main sequence with delay
                coinSequence.Insert(delay, coinPath);
            }
            
            // Al terminar monedas sumar moneda al CurrencyManager y mostrar boton
            coinSequence.OnComplete(() => {
                if (GameManager.Instance?.CurrencyManager != null)
                {
                    GameManager.Instance.CurrencyManager.AddCurrency(totalScore);
                    
                    // Update currency UI with pop animation
                    if (currencyText != null)
                    {
                        currencyText.transform.DOPunchScale(new Vector3(0.3f, 0.3f, 0), 0.3f, 10, 1)
                            .OnComplete(() => {
                                // Skip AnimateTotalScore and go directly to complete state
                                CompleteAnimation();
                            });
                    }
                    else
                    {
                        // Skip AnimateTotalScore and go directly to complete state
                        CompleteAnimation();
                    }
                }
                else
                {
                    // Skip AnimateTotalScore and go directly to complete state
                    CompleteAnimation();
                }
            });
            
            // Play the sequence
            coinSequence.Play();
        }
        
        /// <summary>
        /// Marcar fin de animaciones y muestra boton de continuar
        /// </summary>
        private void CompleteAnimation()
        {
            animationsComplete = true;
            
            // Show continue button with animation
            if (continueButton != null && !continueButton.gameObject.activeInHierarchy)
            {
                continueButton.gameObject.SetActive(true);
                continueButton.transform.localScale = Vector3.zero;
                continueButton.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
            }
        }

        private void AnimateTotalScore()
        {
            DOTween.To(() => displayedScore, x => { displayedScore = x; totalScoreText.text = displayedScore.ToString(); },
                (int)totalScore, finalTotalAnimationDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    // Use CompleteAnimation method for consistency
                    CompleteAnimation();
                });
        }
        // Resetea las filas antes de iniciar animaciones
        private void ResetRows()
        {
            Debug.Log($"[LevelSummaryUI] Resetting {rows.Count} rows");
            
            foreach (var row in rows)
            {
                if (row != null)
                {
                    // Completely reset the row
                    row.gameObject.SetActive(false);
                    row.ClearTexts();
                    if (row.IsAnimating) row.InstantComplete();
                    Debug.Log($"[LevelSummaryUI] Reset row: {row.name}");
                }
                else Debug.LogError("[LevelSummaryUI] Found null row in rows list!"); 
            }
            
            if (totalEarnedRow != null)
            {
                totalEarnedRow.gameObject.SetActive(false);
                totalEarnedRow.ClearTexts();
                if (totalEarnedRow.IsAnimating) totalEarnedRow.InstantComplete();
                Debug.Log("[LevelSummaryUI] Reset total earned row");
            }
        }
        // Oculta panel y ejecuta callback de continuar
        private void HideAndProceed()
        {
            if (isTransitioning) return;
            isTransitioning = true;
            DOTween.Sequence()
                .Join(mainCanvasGroup.DOFade(0f, hideAnimationDuration))
                .Join(panelRectTransform.DOScale(Vector3.zero, hideAnimationDuration).SetEase(hideEase))
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    isTransitioning = false;
                    onSummaryComplete?.Invoke();
                });
        }
        //Si el jugador presiona Skip completa todo de inmediato
        private void SkipAnimations()
        {
            // Make all rows visible and complete their animations
            foreach (var row in rows)
            {
                if (row != null)
                {
                    row.gameObject.SetActive(true);
                    row.InstantComplete();
                }
            }
            if (totalEarnedRow != null)
            {
                totalEarnedRow.gameObject.SetActive(true);
                totalEarnedRow.InstantComplete();
            }
            // Add currency to player immediately when skipping
            if (!_currencyUpdated && GameManager.Instance?.CurrencyManager != null)
            {
                _currencyUpdated = true;
                GameManager.Instance.CurrencyManager.AddCurrency(totalScore);
                // Update currency UI
                if (currencyUI != null) currencyUI.UpdateCurrencyText(GameManager.Instance.CurrencyManager.GetCurrentCurrency());
            }
            if (totalScoreText != null) totalScoreText.text = totalScore.ToString();   
            animationsComplete = true;
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
                continueButton.transform.localScale = Vector3.one;
            }
        }
    }
    //Clase auxiliar que define un dato estadistico para mostrar en la UI
    [System.Serializable]
    public class StatSummaryData
    {
        public string statName;
        public float multiplier;
        public float baseValue;
        public float finalValue;

        public StatSummaryData(string name, float baseVal, float mult)
        {
            statName = name;
            baseValue = baseVal;
            multiplier = mult;
            finalValue = baseValue * multiplier;
        }
    }
    //Cada fila LevelSummaryRowUI representa una estadistica (kills,tiempo,etc)
    //Se muestran una por una con animaciones luego aparece Total Earned
    // Se genera una animacion de monedas volando hacia la UI de currency
    // Finalmente aparece el boton de continuar (o el jugador puede skippear todo con ESC)
}
