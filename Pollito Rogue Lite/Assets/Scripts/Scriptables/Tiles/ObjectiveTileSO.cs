using Enums;
using UnityEngine;

namespace Scriptables.Tiles
{
    [CreateAssetMenu(fileName = "ObjectiveTileSO", menuName = "Tiles/Objective Tile SO")]
    public class ObjectiveTileSO : CustomTileSO
    {
        [SerializeField] private ObjectiveType _objectiveType;
        
        public ObjectiveType ObjectiveType => _objectiveType;
    }
}