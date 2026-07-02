using Enums;
using UnityEngine;

namespace Scriptables.Tiles
{
    [CreateAssetMenu(fileName = "PickableTileSO", menuName = "Tiles/Pickable Tile SO")]
    public class PickableTileSO : CustomTileSO
    {
        [SerializeField] private PickableType _pickableType;
        
        public PickableType PickableType => _pickableType;
    }
}