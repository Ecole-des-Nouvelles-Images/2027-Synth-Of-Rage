using SynthOfRage.Scripts.Utilities;
using UnityEngine;

namespace _Dev.Christopher.Scripts.SystemTest
{
    [CreateAssetMenu(menuName = "Combat/Attack")]
    public class AttackData : ScriptableObject
    {
        public string AttackName;
        public CustomEnumLib.Priority Priority;

        public int StartupFrames;
        public int ActiveFrames;
        public int RecoveryFrames;

        public int HitStunFrames;
        public int BlockStunFrames;
        
        public int TotalFrames => StartupFrames+ActiveFrames+RecoveryFrames;
    }
}