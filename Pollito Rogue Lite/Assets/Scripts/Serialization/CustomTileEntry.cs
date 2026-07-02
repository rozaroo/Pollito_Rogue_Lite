using System.Collections.Generic;
using UnityEngine;

namespace Serialization
{
    [System.Serializable]
    public class CustomTileEntry
    {
        public CustomTileData tileData;
        public List<Vector2Int> positions = new List<Vector2Int>();
    }
}