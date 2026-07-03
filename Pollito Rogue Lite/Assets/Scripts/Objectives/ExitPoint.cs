using UnityEngine;

public class ExitPoint : MonoBehaviour
{
    private GameObject _player;
    private bool _playerInExitArea;
    private bool _levelCompleted;

    public void SetPlayer(GameObject player)
    {
        _player = player;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_player == null || other.gameObject != _player) return;
        _playerInExitArea = true;
            
        // Set level completed flag as soon as player enters the trigger
        if (_levelCompleted) return;
        _levelCompleted = true;
                
        var playerController = _player.GetComponent<PlayerController>();
        if (playerController == null) return;
        playerController.SetLevelCompleted(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (_player == null || other.gameObject != _player) return;
        _playerInExitArea = false;
    }

    private void Update()
    {
        // Only trigger level completion when player stops moving
        if (!_playerInExitArea || !_levelCompleted || _player == null) return;
        var playerController = _player.GetComponent<PlayerController>();
        // Only complete level if player has stopped moving
        if (playerController != null && !playerController.IsMoving) GameLevelEvents.LevelCompleted();
    }
}