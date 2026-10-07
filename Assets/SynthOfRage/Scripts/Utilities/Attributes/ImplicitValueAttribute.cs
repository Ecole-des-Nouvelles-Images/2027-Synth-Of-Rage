using UnityEngine;

namespace SynthOfRage.Scripts.Utilities.Attributes
{
    /// <summary>
    /// A property attribute that mirror the behavior a property with that defaults to an implicit value under a certain condition.<br/>
    /// If the condition is met, the attribute enables the inspector to warn the user of that implicit value.
    /// </summary>
    public class ImplicitValueAttribute : PropertyAttribute
    {
        public readonly ComparisonType Comparison;
        public readonly object ComparisonValue;
        public readonly string Tooltip;

        public ImplicitValueAttribute(ComparisonType comparison, object comparisonValue, string tooltip = null)
        {
            Comparison = comparison;
            ComparisonValue = comparisonValue;
            Tooltip = tooltip;
        }

        /// <summary>Overload that compares Equality (==) by default when no operator is provided.</summary>
        /// <example><c>[ImplicitValue(0f, "Defaults to Time.deltaTime")]</c></example>
        public ImplicitValueAttribute(object comparisonValue, string tooltip = null)
            : this(ComparisonType.Equal, comparisonValue, tooltip)
        {
        }
    }
}
