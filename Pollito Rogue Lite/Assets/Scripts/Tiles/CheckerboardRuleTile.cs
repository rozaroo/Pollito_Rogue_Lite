using UnityEngine;
using UnityEngine.Tilemaps;

namespace Tiles
{
    [CreateAssetMenu(fileName = "CheckerboardRuleTile", menuName = "Tiles/CheckerboardRuleTile")]
    public class CheckerboardRuleTile : RuleTile
    {
        [Tooltip("Dark theme sprites. Names should follow 'S_Floor_Grass_Dark_*' pattern matching light ones.")]
        public Sprite[] _variationTiles;

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            base.GetTileData(position, tilemap, ref tileData);
            var isOdd = (position.x + position.y) % 2 != 0;
            if (!isOdd || tileData.sprite == null || _variationTiles is not { Length: > 0 }) return;
            var lightName = tileData.sprite.name;
            // Convert light sprite name to dark sprite name dynamically
            // Example: "S_Floor_Grass_Light_32_14" -> "S_Floor_Grass_Dark_32_14"
            var darkName = lightName.Replace("_Light_", "_Dark_");
            // Search variationTiles for the dark sprite by name
            foreach (var t in _variationTiles)
            {
                if (t == null || t.name != darkName) continue;
                tileData.sprite = t;
                break;
            }
        }
    }
}
