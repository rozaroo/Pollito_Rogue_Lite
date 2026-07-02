using Enums;
using UnityEngine;
using UnityEngine.Serialization;

namespace Scriptables.Tiles
{
    public class CustomTileSO : ScriptableObject
    {
        [SerializeField] private string _tileName;
        [SerializeField] private bool _requiresSpecialAbility;
        [SerializeField] private ObstacleType _tileType;
        [SerializeField] private bool _isMoveable;
        [SerializeField] private BuffType _requiredAbility;
        
        public string TileName => _tileName;
        public bool RequiresSpecialAbility => _requiresSpecialAbility;
        public bool IsMoveable => _isMoveable;
        public BuffType RequiredAbility => _requiredAbility;
        public ObstacleType TileType => _tileType;
    }
}