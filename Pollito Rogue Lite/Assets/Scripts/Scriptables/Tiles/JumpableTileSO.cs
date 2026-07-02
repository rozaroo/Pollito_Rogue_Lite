using Scriptables.Tiles;
using UnityEngine;

[CreateAssetMenu(fileName = "JumpableTileSO", menuName = "Tiles/Jumpable Tile SO")]
public class JumpableTileSO : CustomTileSO
{
    [SerializeField] private int _jumpHeight = 1;
    public int JumpHeight => _jumpHeight;
}