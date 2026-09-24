using System;
using UnityEngine;
using UnityEngine.UI;

namespace SynthOfRage.Scripts.UI
{
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private int _maxHP = 100;
        private int _hp;
        
        [Header("UI Elements")]
        [SerializeField] private Slider _hpGauge;

        private int MaxHP
        {
            get =>  _maxHP;
            set {
                _maxHP = value;
                _hpGauge.maxValue = _maxHP;
            }
        }
        private int HP
        {
            get => _hp;
            set {
                _hp = value;
                _hpGauge.value = _hp;
            }
        }

        private void Awake()
        {
            if (!_hpGauge)
                UnityEngine.Debug.LogError("[PlayerHealth] Missing HP Gauge reference");
        }

        private void Start()
        {
            HP = MaxHP;
        }

        /// <summary>
        /// Update the health parameters values relative to an incoming value.
        /// </summary>
        /// <param name="delta">The relative value of HP to change.<br/> A positive value will heal and a negative value will hurt</param>
        /// <param name="updateMaxHP">Should the change affect the MaxHPs instead of current HPs</param>
        /// <param name="reflectMaxHPChangeToCurrent">Should the MaxHP change reflects in current HPs.<br/>Increased MaxHP will also be given to current HPs. Current HPs will be clamped to the new max if needed.</param>
        public void UpdateHealth(int delta, bool updateMaxHP = false, bool reflectMaxHPChangeToCurrent = false)
        {
            if (delta == 0) return;

            if (updateMaxHP)
            {
                MaxHP = Mathf.Max(1, MaxHP + delta);

                if (!reflectMaxHPChangeToCurrent) return;

                if (delta > 0)
                    HP += delta;

                HP = Mathf.Clamp(HP, 0, MaxHP);
            }
            else
            {
                HP = Mathf.Clamp(HP + delta, 0, MaxHP);
            }
        }
    }
}
