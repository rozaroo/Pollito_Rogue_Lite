using Scriptables;
using UnityEngine;

namespace Serialization
{
    [System.Serializable]
    public class SceneLevelReference
    {
        [Tooltip("Reference to the puzzle level")]
        [SerializeField] private PuzzleSO _level;
        
        [Tooltip("Name of the scene to load")]
        [SerializeField] private string _sceneName;
        
        public PuzzleSO Level => _level;
        public string SceneName => _sceneName;
    }
}