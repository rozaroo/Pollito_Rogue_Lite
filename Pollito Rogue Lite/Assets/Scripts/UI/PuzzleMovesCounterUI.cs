using System;
using TMPro;
using UnityEngine;

namespace UI
{
    public class PuzzleMovesCounterUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _remainingMovesText;

        private void Awake()
        {
            _remainingMovesText.text = string.Empty;
        }

        public void SetRemainingMoves(int remainingMoves)
        {
            _remainingMovesText.text = remainingMoves.ToString();
        }
        
        /// <summary>
        /// Setea el texto de movimientos con infinito
        /// </summary>
        public void SetUnlimitedMoves()
        {
            // Usa el simbolo infinito para movimientos ilimitados
            _remainingMovesText.text = "∞";
        }

        public void Initialize(int amount)
        {
            SetRemainingMoves(amount);
        }
    }
}