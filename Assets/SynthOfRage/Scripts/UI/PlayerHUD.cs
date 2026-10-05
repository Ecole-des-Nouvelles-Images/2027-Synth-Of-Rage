using UnityEngine;
using UnityEngine.UI;

namespace SynthOfRage.Scripts.UI
{
    public class PlayerHUD: MonoBehaviour
    {
        [SerializeField] private Slider _hpGauge;

        private void Awake()
        {
            if (!_hpGauge)
                UnityEngine.Debug.LogError("[PlayerHealth] Missing HP Gauge reference");
        }
    }
}
