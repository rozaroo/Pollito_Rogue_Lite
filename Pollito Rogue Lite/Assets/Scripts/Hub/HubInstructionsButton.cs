using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HubInstructionsButton : MonoBehaviour
{
  private bool activated;
  private bool playerInRange;
  public InstructionsPanel instructionsPanel;
  private void Update()
    {
        // Solo escucha la tecla si el jugador está dentro del trigger
        if (playerInRange && Input.GetKeyDown(KeyCode.Space))
        {
            activated = !activated;
            OnOff(activated);
        }
    }
  private void OnTriggerEnter2D(Collider2D other)
  {
    if (other.CompareTag("Player")) playerInRange = true;
  }
  private void OnTriggerExit2D(Collider2D other)
  {
    if (other.CompareTag("Player")) playerInRange = false;
  }
  private void OnOff(bool variable)
  {
    if (variable) instructionsPanel.Show();
    else instructionsPanel.Hide();
  }  
}
