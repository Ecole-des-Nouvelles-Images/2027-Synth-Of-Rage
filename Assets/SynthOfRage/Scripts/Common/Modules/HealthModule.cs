using System;
using UnityEngine;

namespace SynthOfRage.Scripts.Common.Modules
{
    /// <summary>
    /// Simple module that represent and manages the health of any unit.<br/>
    /// Raise an event when the unit's <c>HP</c> reach 0.
    /// </summary>
    public class HealthModule : MonoBehaviour
    {
        [SerializeField] private int _maxHP = 100;
        private int _hp;

        public event Action<int, int> OnHealthChanged;

        public int MaxHP
        {
            get =>  _maxHP;
            set => _maxHP = value;
        }
        public int HP
        {
            get => _hp;
            set {
                _hp = value;
                // TODO: Death
            }
        }

        private void Start()
        {
            HP = MaxHP;
        }

        /// <summary>
        /// Simply set a health parameter to a fixed value.
        /// </summary>
        /// <param name="value">The fixed value to set the <c>HP</c> property to.</param>
        /// <param name="clamped">Should the value of <c>HP</c> be clamped to <c>MaxHP</c> (<i>true by default</i>). If <c>MaxHP</c> is reduced, this parameter will clamp down the <c>HP</c> too.</param>
        /// <param name="setMaxHP">Should the <c>maxHP</c> be set rather than current <c>HP</c> (<i>false by default</i>).</param>
        /// <remarks><c>HP</c> and <c>MaxHP</c> can't ever be negative.<br/><c>MaxHP</c> can't be updated below 1.</remarks>
        public void Set(int value, bool clamped = true, bool setMaxHP = false)
        {
            if (setMaxHP)
            {
                MaxHP = Mathf.Max(1, value);

                if (clamped)
                {
                    HP = Mathf.Clamp(HP, 0, MaxHP);
                }
            }
            else
            {
                HP = clamped ? Mathf.Clamp(value, 0, MaxHP) : Mathf.Max(0, value);
            }
        }

        /// <summary>
        /// Variant method that set a health parameter value relative to an incoming value. This method will always clamp the <c>HP</c>.
        /// </summary>
        /// <param name="delta">The relative value of <c>HP</c> to change.<br/> A positive value will heal and a negative value will hurt</param>
        /// <param name="updateMaxHP">Should the change affect the <c>MaxHP</c> instead of current <c>HP</c>.</param>
        /// <param name="reflectMaxHPChangeToCurrent">Should the <c>MaxHP</c> change reflects in current <c>HP</c>.<br/>
        /// Increased <c>MaxHP</c> will also be given to current <c>HP</c>. Current <c>HP</c> will be clamped to the new max if needed.</param>
        /// <remarks><c>HP</c> and <c>MaxHP</c> can't ever be negative.<br/><c>MaxHP</c> can't be updated below 1.</remarks>
        public void Apply(int delta, bool updateMaxHP = false, bool reflectMaxHPChangeToCurrent = false)
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
