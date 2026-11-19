using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace _Dev.Vincent.ToonShaders.Rendering
{
    [Serializable, VolumeComponentMenu("SynthOfRage/Toon Outline")]
    public sealed class SOROutlineVolume : VolumeComponent, IPostProcessComponent
    {
        [Header("Outline")]
        public BoolParameter overrideOutlineColor = new BoolParameter(false);
        public ColorParameter outlineColor =
            new ColorParameter(Color.black, true, false, true);

        [Header("Graffiti Accent")]
        public BoolParameter overrideAccentColor = new BoolParameter(false);
        public ColorParameter accentColor =
            new ColorParameter(new Color(1f, 0.02f, 0.38f, 1f), true, false, true);

        public bool IsActive()
        {
            return overrideOutlineColor.value || overrideAccentColor.value;
        }

        public bool IsTileCompatible() => false;
    }
}