using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Water : MonoBehaviour
{
    private PlayerController _playerController;
    public TilemapCollider2D _tilemapCollider;
    public GameObject Void;

    // Update is called once per frame
    void Update()
    {
        _playerController = FindObjectOfType<PlayerController>();
        // Si el jumpbuff está false, desactivar el collider
        if (!_playerController.jumpbuff)
        {
            if (_tilemapCollider.enabled) _tilemapCollider.enabled = false;
            Void.SetActive(true);
        }
        else
        {
            if (!_tilemapCollider.enabled) _tilemapCollider.enabled = true;
            Void.SetActive(false);
        }
    }
}
