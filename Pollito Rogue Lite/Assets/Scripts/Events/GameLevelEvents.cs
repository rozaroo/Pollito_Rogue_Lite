using System.Collections.Generic;
using Scriptables;
using UnityEngine;

public static class GameLevelEvents
{
    // Updated event to include buff/debuff information
    public delegate void PlayerSpawnedEventHandler(Transform playerTransform, List<BuffSO> selectedBuffs = null, List<DebuffSO> selectedDebuff = null);
    public static event PlayerSpawnedEventHandler OnPlayerSpawned;

    // Other existing events
    public delegate void PlayerSpawnRequestHandler(EntryPoint entryPoint);
    public static event PlayerSpawnRequestHandler OnPlayerSpawnRequested;

    public delegate void LevelCompletedEventHandler();
    public static event LevelCompletedEventHandler OnLevelCompleted;

    public static void PlayerSpawned(Transform playerTransform, List<BuffSO> selectedBuffs = null, List<DebuffSO> selectedDebuffs = null)
    {
        OnPlayerSpawned?.Invoke(playerTransform, selectedBuffs, selectedDebuffs);
    }

    public static void RequestPlayerSpawn(EntryPoint entryPoint)
    {
        OnPlayerSpawnRequested?.Invoke(entryPoint);
    }

    public static void LevelCompleted()
    {
        OnLevelCompleted?.Invoke();
    }
}