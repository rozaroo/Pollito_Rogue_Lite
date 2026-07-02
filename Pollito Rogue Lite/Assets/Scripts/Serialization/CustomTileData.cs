using UnityEngine;
using UnityEngine.Tilemaps;
using Enums;

[System.Serializable]
public class CustomTileData
{
    public string spriteReference;  // Path to the texture asset
    public string spriteName;       // Name of the specific sprite in the sheet
    public CustomTileLayer layer;
    public CustomTileAlpha alpha;
    public Color color = Color.white;
    public TileFlags flags;
    public Tile.ColliderType colliderType;
    public Matrix4x4 transformMatrix;
    public bool isMovable = false;
    public bool isBreakable = false;
    public bool isFragile;
    public int breakForceRequired = -1;
    public ObjectiveType objectiveType = ObjectiveType.None;
}