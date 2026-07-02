using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Components
{
    public class CharacterEffectUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text titleText;
        
        public void Setup(Sprite icon, string title, string description, Color color)
        {
            if (iconImage != null && icon != null)
            {
                iconImage.sprite = icon;
                iconImage.gameObject.SetActive(true);
            }
            else if (iconImage != null)
            {
                iconImage.gameObject.SetActive(false);
            }
            
            if (titleText != null)
            {
                titleText.text = title;
            }
        }
    }
}