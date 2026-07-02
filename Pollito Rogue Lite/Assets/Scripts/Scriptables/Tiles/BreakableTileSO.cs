using Scriptables.Tiles;
using UnityEngine;

[CreateAssetMenu(fileName = "BreakableTileSO", menuName = "Tiles/Breakable Tile SO")]
public class BreakableTileSO : CustomTileSO
{
    [SerializeField] private int _breakForce = 1;
    public int BreakForce => _breakForce;
}