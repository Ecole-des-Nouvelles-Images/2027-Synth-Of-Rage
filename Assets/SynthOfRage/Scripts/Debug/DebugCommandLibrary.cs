#if UNITY_EDITOR || UNITY_INCLUDE_INSTRUMENTATION // Player settings > Managed Code Variant > "Instrumented" or below

using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;

using SynthOfRage.Scripts.Helper;

namespace SynthOfRage.Scripts.Debug
{
    public static partial class DebugCommandLibrary
    {
        [AutoStaticsCleanup]
        public static readonly List<DebugCommandBase> CmdList = new()
        {
            new DebugCommand(
                "quit",
                "Quit the application",
                "quit",
                _ => Utils.QuitGame()
            ),
            new DebugCommand(
                "restart",
                "Reload the current active scene.",
                "restart",
                _ => Utils.RestartLevel()
            ),
            new DebugCommand(
                "load",
                "Load any scene with the provided index.",
                "load <level_index>",
                args =>
                {
                    if (args.Length > 0 && int.TryParse(args[0], out int levelIndex))
                        Utils.LoadLevel(levelIndex);
                    else
                        UnityEngine.Debug.LogWarning("Usage: load <level_index>");
                }
            )
        };
    }
}

#endif
