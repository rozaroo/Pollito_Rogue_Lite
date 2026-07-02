using System;
using System.Collections.Generic;
using UnityEngine;
using Enums;
using Serialization;
using UnityEngine.Serialization;

namespace Scriptables
{
    [CreateAssetMenu(fileName = "New Puzzle", menuName = "4P-Externals/Puzzles/Puzzle Data")]
    public class PuzzleSO : ScriptableObject
    {
        [Header("General Info")]
        [SerializeField] private PuzzleDifficultyTier _difficulty = PuzzleDifficultyTier.Easy;
        [SerializeField] private int _maxMoves = 1;
        [SerializeField] private bool _unlimitedMoves = false;

        [Header("Solutions")]
        [Tooltip("Each entry represents a possible solution with different buff combinations")]
        [SerializeField] private List<SolutionBuffs> _possibleSolutions = new();
        
        public int MaxMoves => _maxMoves;

        public PuzzleDifficultyTier Difficulty
        {
            get => _difficulty;
            set => _difficulty = value;
        }

        /// <summary>
        /// Returns true if this puzzle has unlimited moves.
        /// </summary>
        public bool HasUnlimitedMoves => MaxMoves == -1 && _unlimitedMoves;

        /// <summary>
        /// Gets the effective max moves, accounting for unlimited moves.
        /// Returns int.MaxValue for unlimited moves.
        /// </summary>
        public int EffectiveMaxMoves => HasUnlimitedMoves ? int.MaxValue : _maxMoves;

        /// <summary>
        /// List of all possible buff combinations that can solve this puzzle
        /// </summary>
        public List<SolutionBuffs> PossibleSolutions => _possibleSolutions;

        /// <summary>
        /// Check if this puzzle can be solved with a specific set of buffs
        /// </summary>
        public bool CanBeSolvedWith(List<BuffType> availableBuffs)
        {
            // Check each solution to see if any of them can be solved with the provided buffs
            foreach (var solution in _possibleSolutions)
            {
                if (solution.CanSolveWith(availableBuffs))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if this puzzle requires a specific buff to be solved
        /// </summary>
        public bool RequiresBuff(BuffType buffType)
        {
            // If all solutions require this buff, then the puzzle requires it
            foreach (var solution in _possibleSolutions)
            {
                if (!solution.RequiresBuff(buffType))
                {
                    return false;
                }
            }
            return _possibleSolutions.Count > 0;
        }
        
        private void OnValidate()
        {
            // Update display names for all solutions
            if (_possibleSolutions != null)
            {
                foreach (var solution in _possibleSolutions)
                {
                    if (solution != null)
                    {
                        solution.OnValidate();
                    }
                }
            }
        }
        
        public string DifficultyName => _difficulty.ToString();
    }
}