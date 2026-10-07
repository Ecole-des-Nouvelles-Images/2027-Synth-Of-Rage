using UnityEngine;

namespace SynthOfRage.Scripts.Utilities.Attributes
{
    public class ReadOnlyAttribute : PropertyAttribute
    {
        public readonly string Tooltip;

        public ReadOnlyAttribute(string tooltip = null)
        {
            Tooltip = tooltip;
        }
    }
}
