#if UNITY_EDITOR || UNITY_INCLUDE_INSTRUMENTATION

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;

using Avatar = SynthOfRage.Scripts.Gameplay.Unit.Player.Avatar;
using Object = UnityEngine.Object;

namespace SynthOfRage.Scripts.Debug
{
    public static partial class DebugUtils
    {
        /// <summary>
        /// Represents a single settable property with its object type, path, and setter logic.
        /// </summary>
        private sealed class PropertySetter
        {
            public string Object { get; }
            public string Property { get; }
            private readonly Action<string> _setter;

            public PropertySetter(string entity, string property, Action<string> setter)
            {
                Object = entity;
                Property = property;
                _setter = setter;
            }

            public void Execute(string valueStr) => _setter(valueStr);
        }

        /// <summary>
        /// Registry of all supported set-property operations.
        /// </summary>
        [AutoStaticsCleanup]
        private static readonly List<PropertySetter> PropertySetterRegistry = new() {
            new PropertySetter(
                "Avatar",
                "HP",
                valueStr =>
                {
                    Avatar avatar = Object.FindAnyObjectByType<Avatar>();
                    if (!avatar) {
                        UnityEngine.Debug.Log($"[DebugUtils] set-property {{Avatar HP}}: can't find object Avatar in the scene");
                        return;
                    };
                    if (!int.TryParse(valueStr, out int newHP)) {
                        UnityEngine.Debug.Log($"[DebugUtils] set-property {{Avatar HP}}: can't parse value '{valueStr}'");
                        return;
                    }

                    avatar.HealthModuleDebug.Set(newHP, false);
                    UnityEngine.Debug.Log($"[DebugUtils] set-property {{Avatar HP}} → {newHP}");
                }
            ),
            new PropertySetter(
                "Avatar",
                "MaxHP",
                valueStr =>
                {
                    Avatar avatar = Object.FindAnyObjectByType<Avatar>();
                    if (!avatar) {
                        UnityEngine.Debug.Log($"[DebugUtils] set-property {{Avatar MaxHP}}: can't find object Avatar in the scene");
                        return;
                    };
                    if (!int.TryParse(valueStr, out int newMaxHP)) {
                        UnityEngine.Debug.Log($"[DebugUtils] set-property {{Avatar MaxHP}}: can't parse value '{valueStr}'");
                        return;
                    }

                    avatar.HealthModuleDebug.Set(newMaxHP, true, true);
                    UnityEngine.Debug.Log($"[DebugUtils] set-property {{Avatar MaxHP}} → {newMaxHP}");
                }
            ),
        };

        /// <summary>
        /// Set a property on a target object by name.
        /// </summary>
        /// <remarks>Command format: <c>set-property &lt;object&gt; &lt;property&gt; &lt;value&gt;</c>
        /// <list type="bullet">
        /// <item><term><c>object</c></term> <description>The entity to modify (example: "Avatar").</description></item>
        /// <item><term><c>property</c></term> <description>The shortname of the property to update (example: "hp").</description></item>
        /// <item><term><c>value</c></term> <description> The value to assign.</description></item>
        /// </list>
        /// <b>Note:</b> Supported combinations must be registered as <see cref="PropertySetter"/> instances.
        /// </remarks>
        /// <example><code>set-property avatar hp 50</code></example>
        public static void SetProperty(string[] args)
        {
            if (args == null || args.Length < 3)
            {
                UnityEngine.Debug.Log("[DebugUtils] Usage: set-property <object> <property> <value>");
                return;
            }

            string objectName = args[0];
            string propertyName = args[1];
            string valueStr = args[2];

            foreach (PropertySetter setter in PropertySetterRegistry)
            {
                if (setter.Object.Equals(objectName, StringComparison.OrdinalIgnoreCase) && setter.Property.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    setter.Execute(valueStr);
                    return;
                }
            }

            UnityEngine.Debug.Log($"[DebugUtils] set-property: '{objectName} {propertyName}' is unavailable.");
        }
    }
}

#endif
