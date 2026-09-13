namespace OriathHub.Plugins.Sanctum
{
    using System.Numerics;

    /// <summary>
    ///     Sanctum plugin settings class.
    /// </summary>
    public sealed class SanctumSettings
    {
        /// <summary>Draw the best path over the Sanctum floor map.</summary>
        public bool Show = true;

        /// <summary>Show the info window (sacred water, honour, keys, position).</summary>
        public bool ShowInfoWindow = true;

        /// <summary>Draw room weights and their breakdown on the floor map.</summary>
        public bool DebugEnable = false;

        /// <summary>Name of the active profile (its file in the profiles folder, without extension).</summary>
        public string CurrentProfile = "Default";

        /// <summary>Frame colour for rooms on the best path.</summary>
        public Vector4 BestPathColor = new(0f, 1f, 1f, 1f);

        /// <summary>Frame thickness for rooms on the best path.</summary>
        public float FrameThickness = 5f;

        /// <summary>Debug text colour.</summary>
        public Vector4 TextColor = new(1f, 1f, 1f, 1f);

        /// <summary>Debug text background colour.</summary>
        public Vector4 BackgroundColor = new(0f, 0f, 0f, 0.5f);

        /// <summary>Whether the Profiles settings section is expanded.</summary>
        public bool ProfilesSectionOpen = true;

        /// <summary>Whether the room type weights list is expanded.</summary>
        public bool RoomTypeWeightsOpen = false;

        /// <summary>Whether the affliction weights list is expanded.</summary>
        public bool AfflictionWeightsOpen = false;

        /// <summary>Whether the reward weights list is expanded.</summary>
        public bool RewardWeightsOpen = false;
    }
}
