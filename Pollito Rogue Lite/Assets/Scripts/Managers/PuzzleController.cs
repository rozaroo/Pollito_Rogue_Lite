using Scriptables;
using Tilemaps;
using UnityEngine;
using UnityEngine.Serialization;

namespace Managers
{
    public class PuzzleController : MonoBehaviour
    {
        [SerializeField] private TilemapManager _tilemapManager;
        [SerializeField] private GameObject _spawnpoint;
        [SerializeField] private GameObject _goal;
        [SerializeField] private PuzzleSO _currentPuzzle;
        
        public GameObject SpawnPoint => _spawnpoint;
        public GameObject Goal => _goal;
        public int PuzzleTotalMoves => _tilemapManager.CurrentPuzzle.MaxMoves; //Devuelve el número máximo de movimientos permitidos en el puzzle actual, consultando el TilemapManager
        public PuzzleSO CurrentPuzzle => _currentPuzzle; //Devuelve el puzzle asignado actualmente

        private void Awake()
        {
            InitializeCurrentPuzzle();
        }

        /// <summary>
        /// Initializes the current puzzle if it's set.
        /// </summary>
        private void InitializeCurrentPuzzle()
        {
            if (_currentPuzzle == null) Debug.LogWarning("No puzzle assigned to PuzzleController!");
        }
        
        /// <summary>
        /// Sets the current puzzle
        /// </summary>
        public void SetCurrentPuzzle(PuzzleSO puzzle)
        {
            _currentPuzzle = puzzle;
            // Actualiza _currentPuzzle y también notifica al TilemapManager que cambió el puzzle para que actualice el tablero si es necesario
            if (_tilemapManager != null) _tilemapManager.SetCurrentPuzzle(_currentPuzzle);
        }
    }
    //PuzzleController no resuelve la lógica del puzzle, solo gestiona cuál es el puzzle actual y
    // mantiene referencias clave para que el jugador y el tablero sepan dónde empezar y dónde está la meta.
}