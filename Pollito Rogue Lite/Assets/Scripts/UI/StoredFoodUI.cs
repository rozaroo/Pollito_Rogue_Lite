using TMPro;
using UnityEngine;

namespace UI
{
    public class StoredFoodUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _storedFoodText;

        private void Awake()
        {
            Initialize();
        }

        public void SetStoredFood(int totalMoves)
        {
            _storedFoodText.text = totalMoves.ToString();
        }

        private void Initialize(int amount = 0)
        {
            SetStoredFood(amount);
        }
    }
}