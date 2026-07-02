using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Managers;
using UnityEngine.UI;

namespace UI
{
    // Controla la UI que muestra la moneda/currency del jugador (ej: monedas recolectadas)
    public class CurrencyUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _currencyText;
        [SerializeField] private Sprite _coinSprite;
        
        public TextMeshProUGUI CurrencyText => _currencyText;
        public Sprite CoinSprite => _coinSprite;

        private void Awake()
        {
            Initialize();
        }
    
        private void OnEnable()
        {
            // Cada vez que se activa la UI, se actualiza con la cantidad actual de monedas
            RefreshCurrencyDisplay();
        }

        /// <summary>
        /// Actualiza el texto que muestra la cantidad de monedas
        /// </summary>
        public void UpdateCurrencyText(int amount)
        {
            if (_currencyText != null) _currencyText.text = amount.ToString();
        }
        
        /// <summary>
        /// Establece directamente la cantidad de monedas
        /// </summary>
        public void SetCurrencyAmount(int amount)
        {
            // Hace lo mismo que UpdateCurrencyText (es como un alias por compatibilidad)
            UpdateCurrencyText(amount);
        }
        
        /// <summary>
        /// Devuelve el componente de texto (util para animaciones de DOTween u otros)
        /// </summary>
        public TextMeshProUGUI GetCurrencyText() => _currencyText;

        /// <summary>
        /// Refresca ka cantidad de monedas desde el CurrencyManager del GameManager
        /// </summary>
        public void RefreshCurrencyDisplay()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrencyManager != null)
            {
                int currentCurrency = GameManager.Instance.CurrencyManager.GetCurrentCurrency();
                SetCurrencyAmount(currentCurrency);
                Debug.Log($"[CurrencyUI] Refreshed currency display: {currentCurrency}");
            }
        }
        //Inicializa el valor de monedas al empezar
        private void Initialize(int amount = 0)
        {
            SetCurrencyAmount(amount);
            // Luego intenta cargar el valor real desde el CurrencyManager
            RefreshCurrencyDisplay();
        }
    }
}
