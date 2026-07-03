using UnityEngine;
using UI;

namespace Managers
{
    public class CurrencyManager : MonoBehaviour
    {
        [Header("Currency Multipliers")]
        [Tooltip("Multiplicador aplicado a los movimientos restantes para calcular monedas")]
        [SerializeField] private int _movesMultiplier = 5;

        [Tooltip("Multiplicador aplicado a la comida almacenada para calcular monedas")]
        [SerializeField] private int _foodMultiplier = 10;

        [SerializeField] private int _totalCurrency = 0; //Moneda total acumulada
        
        // Clave usada para guardar/cargar moneda desde PlayerPrefs
        private const string CURRENCY_PREFS_KEY = "PlayerCurrency";
        
        // Referencia a la UI de moneda activa
        private CurrencyUI _cachedCurrencyUI;

        // Propiedades publicas para acceder a los multiplicadores
        /// <summary>
        /// Gets the multiplier applied to remaining moves when calculating currency.
        /// </summary>
        public int MovesMultiplier => _movesMultiplier;

        /// <summary>
        /// Gets the multiplier applied to stored food when calculating currency.
        /// </summary>
        public int FoodMultiplier => _foodMultiplier;
        
        private void Awake()
        {
            if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
            // Carga el valor de moneda guardado al iniciar
            LoadCurrency();
        }
        
        /// <summary>
        /// Carga la moneda guardada en PlayerPrefs
        /// </summary>
        private void LoadCurrency()
        {
            if (PlayerPrefs.HasKey(CURRENCY_PREFS_KEY))
            {
                _totalCurrency = PlayerPrefs.GetInt(CURRENCY_PREFS_KEY, 0);
                Debug.Log($"[CurrencyManager] Loaded currency from PlayerPrefs: {_totalCurrency}");
            }
            else Debug.Log("[CurrencyManager] No saved currency found in PlayerPrefs");
            // Actualiza la UI con la moneda cargada
            UpdateCurrencyUI();
        }
        
        /// <summary>
        /// Guarda la moneda actual en PlayerPrefs
        /// </summary>
        private void SaveCurrency()
        {
            PlayerPrefs.SetInt(CURRENCY_PREFS_KEY, _totalCurrency);
            PlayerPrefs.Save();
            Debug.Log($"[CurrencyManager] Saved currency to PlayerPrefs: {_totalCurrency}");
        }

        /// <summary>
        /// Actualiza la UI de la moneda (si existe)
        /// </summary>
        public void UpdateCurrencyUI()
        {
            // Intenta obtener la UI de moneda desde GameCanvasManager
            if (GameManager.Instance?.LevelManager?.GameCanvasManager?.CurrencyUI != null)
            {
                _cachedCurrencyUI = GameManager.Instance.LevelManager.GameCanvasManager.CurrencyUI;
                _cachedCurrencyUI.SetCurrencyAmount(_totalCurrency);
                Debug.Log("[CurrencyManager] Updated currency UI via GameCanvasManager");
                return;
            }
            
            //  Si no, usa la referencia guardada en cache
            if (_cachedCurrencyUI != null)
            {
                _cachedCurrencyUI.SetCurrencyAmount(_totalCurrency);
                Debug.Log("[CurrencyManager] Updated currency UI via cached reference");
                return;
            }
            
            // Como ultimo recurso, busca en la escena cualquier currency
            var currencyUI = FindObjectOfType<CurrencyUI>();
            if (currencyUI != null)
            {
                _cachedCurrencyUI = currencyUI;
                _cachedCurrencyUI.SetCurrencyAmount(_totalCurrency);
                Debug.Log("[CurrencyManager] Updated currency UI found via FindObjectOfType");
                return;
            }
            
            Debug.LogWarning("[CurrencyManager] Could not find any CurrencyUI to update");
        }
        
        /// <summary>
        /// Calcula monedas al completar un nivel
        /// </summary>
        /// <param name="movesLeft">Number of moves the player had remaining</param>
        /// <param name="storedFood">Amount of stored food collected</param>
        public void CalculateLevelCurrency(int movesLeft, int storedFood)
        {
            int movesCurrency = movesLeft * _movesMultiplier; // monedas por movimientos sobrantes
            int foodCurrency = storedFood * _foodMultiplier; // monedas por comida guardada
            int levelCurrency = movesCurrency + foodCurrency;

            Debug.Log($"[CurrencyManager] Level currency calculation: " +
                      $"({movesLeft} moves × {_movesMultiplier}) + ({storedFood} food × {_foodMultiplier}) = {levelCurrency}");

            AddCurrency(levelCurrency);
        }

        /// <summary>
        /// Añade monedas al total
        /// </summary>
        /// <param name="amount">Amount of currency to add</param>
        public void AddCurrency(int amount)
        {
            if (amount <= 0) return;
            //Efecto de moneda
            AkSoundEngine.PostEvent("Coin", gameObject);
            _totalCurrency += amount;
            Debug.Log($"[CurrencyManager] Added {amount} currency. New total: {_totalCurrency}");

            // Update UI
            UpdateCurrencyUI();
            
            // Save the updated currency value
            SaveCurrency();
        }

        /// <summary>
        /// Gasta monedas si hay suficientes
        /// </summary>
        /// <param name="amount">Amount of currency to spend</param>
        /// <returns>True if transaction successful, false if insufficient funds</returns>
        public bool SpendCurrency(int amount)
        {
            if (amount <= 0) return true;
            if (_totalCurrency < amount) return false;

            _totalCurrency -= amount;
            Debug.Log($"[CurrencyManager] Spent {amount} currency. Remaining: {_totalCurrency}");

            // Update UI
            UpdateCurrencyUI();
            
            // Save the updated currency value
            SaveCurrency();
            
            return true;
        }

        /// <summary>
        /// Obtiene la moneda actual
        /// </summary>
        /// <returns>Total currency value</returns>
        public int GetCurrentCurrency()
        {
            return _totalCurrency;
        }
        
        /// <summary>
        /// Reinicia la moneda a 0
        /// </summary>
        public void ResetCurrency()
        {
            _totalCurrency = 0;
            UpdateCurrencyUI();
            SaveCurrency();
            Debug.Log("[CurrencyManager] Currency reset to 0");
        }
        
        private void OnApplicationQuit()
        {
            // Asegura que se guarde la moneda al cerrar la aplicacion
            SaveCurrency();
        }
    }
}
