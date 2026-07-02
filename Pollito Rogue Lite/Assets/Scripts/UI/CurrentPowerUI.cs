using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scriptables;

namespace UI
{
    public class CurrentPowerUI : MonoBehaviour
    {
        [SerializeField] private Image _buffIcon;
        [SerializeField] private TextMeshProUGUI _buffNameText;

        /// <summary>
        /// Actualiza la UI para mostrar informacion sobre el buff activo
        /// </summary>
        /// <param name="buff">El BuffSO que contiene los datos del buff</param>
        public void UpdateBuffDisplay(BuffSO buff)
        {
            if (buff == null)
            {
                // Si no hay buff activo, se oculta el panel de buff
                gameObject.SetActive(false);
                return;
            }
            // Si hay un buff, se activa el panel de buff y se actualiza la UI
            gameObject.SetActive(true);
            _buffIcon.sprite = buff.DefaultHeadSprite; // Asigna el icono del buff (spite que trae el ScriptableObject BuffSO)
            _buffNameText.text = buff.effectName; //Asigna el nombre del efecto del buff
        }

        /// <summary>
        /// Limpia la UI del buff (se oculta el panel de buff actual)
        /// </summary>
        public void ClearBuffDisplay()
        {
            gameObject.SetActive(false);
        }
    }
    //Este script se encarga de mostrar en pantalla el buff actual del jugador
    //Usa un Image para el icono y un TextMeshProUGUI para el nombre
    //Si no hay buff el objeto que contiene esta UI se oculta (setactive(false))
    //Si hay buff se actualizan el icono y el nombre
    //Tambien tiene un metodo clearbuffdisplay() para ocultar manualmente la UI cuando el buff termina
}
