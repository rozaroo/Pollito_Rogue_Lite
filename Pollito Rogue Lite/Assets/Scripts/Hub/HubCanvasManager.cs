using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;

public class HubCanvasManager : MonoBehaviour
{
    [SerializeField] private CurrencyUI _currencyUI;
    
    public CurrencyUI CurrencyUI => _currencyUI;
}
