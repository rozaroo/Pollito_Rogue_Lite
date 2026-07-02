using Scriptables.Tiles;
using UnityEngine;

[CreateAssetMenu(fileName = "FeatherTileSO", menuName = "Tiles/Feather Tile SO")]
public class FeatherTileSO : CustomTileSO
{
    [SerializeField] private int _breakForce = 1;
    public int BreakForce => _breakForce;
}