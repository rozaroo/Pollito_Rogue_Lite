using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterSensor : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && !player.jumpbuff) StartCoroutine(DelayedDeath(player, 0.1f));
        }
    }
    private IEnumerator DelayedDeath(PlayerController player, float delay) 
    {
        yield return new WaitForSeconds(delay);
        player.Die();
    }
}
