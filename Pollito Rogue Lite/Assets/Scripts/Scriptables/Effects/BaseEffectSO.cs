using Enums;
using UnityEngine;
using UnityEngine.Serialization;

namespace Scriptables
{
    public abstract class BaseEffectSO : ScriptableObject
    {
        public string effectName;
        public string description;
        public float duration = -1;
        public Sprite icon;
        public bool developmentOnly = true;
        public bool enabled = true;
        
        public abstract EffectType EffectType { get; }
    }
}