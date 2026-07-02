using UnityEngine;
using UnityEngine.Audio;

public class ExitPoint : MonoBehaviour
{
    private GameObject _player;
    private bool _playerInExitArea;
    private bool _levelCompleted;

    [Header("Audio")]
    [SerializeField] private AudioClip _victorySound;
    [SerializeField] private float _victoryVolume = 1f;
    private AudioSource _audioSource;

    private void Awake()
    {
        // Puedes usar un AudioSource global o crear uno si no existe
        if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
    }

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
        //PlayVictorySound();
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
    private void PlayVictorySound()
    {
        if (_victorySound == null) return;
        _audioSource.spatialBlend = 0f; // 2D
        _audioSource.PlayOneShot(_victorySound, _victoryVolume);
    }
}