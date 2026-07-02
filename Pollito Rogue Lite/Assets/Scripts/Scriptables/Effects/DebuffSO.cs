using Enums;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(fileName = "New Debuff", menuName = "Effects/Debuffs/Generic Debuff")]
    public class DebuffSO : BaseEffectSO
    {
        [SerializeField] private DebuffType _type;
        [SerializeField] private Color _color;
        
        public override EffectType EffectType => EffectType.Debuff;
        public DebuffType Type => _type;
    }
}