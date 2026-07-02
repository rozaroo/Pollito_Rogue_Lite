using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scriptables;

namespace UI
{
    public class CurrentDebuffUI : MonoBehaviour
    {
        /// Actualiza la UI para mostrar informacion sobre el debuff activo
        public void UpdateDebuffDisplay()
        {
            // Si hay dos debuff, se activa el panel de debuff y se actualiza la UI
            gameObject.SetActive(true);
        }

        /// Limpia la UI del debuff (se oculta el panel de debuff actual)
        public void ClearDebuffDisplay()
        {
            gameObject.SetActive(false);
        }
    }
    //Este script se encarga de mostrar en pantalla el debuff actual del jugador
    //Usa un Image para el icono
    //Si no hay debuff el objeto que contiene esta UI se oculta (setactive(false))
    //Si hay debuff se actualizan el icono y el nombre
    //Tambien tiene un metodo clearbuffdisplay() para ocultar manualmente la UI cuando el buff termina
}
