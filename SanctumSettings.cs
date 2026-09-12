namespace OriathHub.Plugins.Sanctum
{
    using System.Numerics;

    /// <summary>
    ///     Sanctum plugin settings class.
    /// </summary>
    public sealed class SanctumSettings
    {
        /// <summary>
        ///     Show the Sanctum overlay window.
        /// </summary>
        public bool Show = true;

        /// <summary>
        ///     Draw debug information (room weights, ids, etc.) alongside the path.
        /// </summary>
        public bool DebugEnable = false;

        /// <summary>
        ///     Colour used to draw the suggested path line.
        /// </summary>
        public Vector4 PathColor = new(1f, 1f, 0f, 1f);
    }
}
