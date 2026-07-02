using System;
using Managers;
using UnityEngine;

namespace Hub
{
    public class HubManager : MonoBehaviour
    {
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private HubCanvasManager _hubCanvasManager;
    
        public PlayerController PlayerController => _playerController;

        private void Start()
        {
            GameManager.Instance.SetHubManager(this);
            _hubCanvasManager.CurrencyUI.SetCurrencyAmount(GameManager.Instance.CurrencyManager.GetCurrentCurrency());
        }
    }
}
