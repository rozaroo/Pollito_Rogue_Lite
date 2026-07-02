using UnityEngine;
using UnityEngine.Tilemaps;
using Enums;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tiles
{
    [CreateAssetMenu(menuName = "4P-Externals/Tiles/Custom Tile")]
    public class CustomTile : Tile
    {
        [Header("Tile Properties")]
        [SerializeField] private CustomTileLayer _layer = CustomTileLayer.Floor;
        [SerializeField] private CustomTileAlpha _alpha = CustomTileAlpha.Opaque;
        [SerializeField] private ObjectiveType _objectiveType = ObjectiveType.None;

        [SerializeField] private bool _isMovable = false;
        [SerializeField] private bool _isBreakable = false;
        [SerializeField] private bool _isFragile = false;
        [SerializeField] private int _breakForceRequired = -1;
        [SerializeField] private bool _isBomb = false;
        // Position in 2D space
        public Vector2Int Position { get; set; }
        public CustomTileLayer Layer { get => _layer;
            set => _layer = value;
        }
        public CustomTileAlpha Alpha { get => _alpha;
            set => _alpha = value;
        }
        
        public int BreakForceRequired
        {
            get => _breakForceRequired;
            set => _breakForceRequired = value;
        }
        
        public bool IsBreakable
        {
            get => _isBreakable && _breakForceRequired > 0;
            set => _isBreakable = value;
        }
        
        public bool IsMovable {
            get => _isMovable;
            set => _isMovable = value;
        }
        
        public bool IsFragile {
            get => _isFragile;
            set => _isFragile = value;
        }
        public ObjectiveType ObjectiveType {
            get => _objectiveType;
            set => _objectiveType = value;
        }
        public bool IsBomb
        {
            get => _isBomb;
            set => _isBomb = value;
        }
    }
}