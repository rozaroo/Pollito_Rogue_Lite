using Tiles;
using UnityEngine;
using UnityEngine.Serialization;

namespace Managers
{
    /// <summary>
    /// Administra referencias a tiles especiales que se usan en todo el juego.
    /// </summary>
    public class SpecialTilesManager : MonoBehaviour
    {
        [Header("Tile References")]
        [SerializeField] private CustomTile _fragileActiveTile; // Tile fragil en estado activo (cuando esta por romperse)
        [SerializeField] private CustomTile _fragileSleepTile; // Tile fragil en estado normal (sin activar)
        // Aquí se pueden agregar más tiles especiales en el futuro según sea necesario

        /// <summary>
        /// Retorna el tile que representa un frágil activado (a punto de romperse).
        /// </summary>
        public CustomTile GetFragileActiveTile() => _fragileActiveTile;

        /// <summary>
        /// Retorna el tile que representa un frágil normal (sin activar).
        /// </summary>
        public CustomTile GetFragileSleepTile() => _fragileSleepTile;

    }
}