using System;

namespace Enums
{
    /// <summary>
    /// Enum representing the various reasons a player can die in the game.
    /// </summary>
    public enum DeathReason
    {
        /// <summary>
        /// Player died due to running out of moves.
        /// </summary>
        Moves,
        
        /// <summary>
        /// Player died by falling into the void.
        /// </summary>
        Void,
        
        /// <summary>
        /// Player died by stepping on a fragile tile.
        /// </summary>
        FragileTile,
        
        /// <summary>
        /// Unknown or unspecified death reason.
        /// </summary>
        Unknown
    }
}
