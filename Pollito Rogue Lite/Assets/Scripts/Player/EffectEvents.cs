using System;
using Enums;
using UnityEngine;

namespace Player
{
    public class EffectEvents : MonoBehaviour
    {
        public event Action<BuffType> OnEffectRegistered;
        public event Action<BuffType> OnEffectUnregistered;
        public event Action<BuffType> OnEffectActivated;
        public event Action<BuffType> OnEffectDeactivated;

        public void InvokeRegistered(BuffType type) => OnEffectRegistered?.Invoke(type);
        public void InvokeUnregistered(BuffType type) => OnEffectUnregistered?.Invoke(type);
        public void InvokeActivated(BuffType type) => OnEffectActivated?.Invoke(type);
        public void InvokeDeactivated(BuffType type) => OnEffectDeactivated?.Invoke(type);
    }
}