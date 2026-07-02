using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class MovementCounter : MonoBehaviour
{
    [SerializeField] private int _moveCount = 0;
    public void Increment() 
    {
        _moveCount++;
    }
    public int GetMoveCount() 
    {
        return _moveCount;
    }
    public void ResetCounter() 
    {
        _moveCount = 0;
    }
    public void Decrease()
    {
        _moveCount--;
    }
}

