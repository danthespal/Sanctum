namespace OriathHub.Plugins.Sanctum
{
    using System.Collections.Generic;
    using System.IO;
    using Coroutine;
    using ImGuiNET;
    using OriathHub.CoroutineEvents;
    using OriathHub.Plugin;
    using OriathHub.RemoteEnums;
    using OriathHub.Utils;

    /// <summary>
    ///     Sanctum pathfinding plugin — port of PathfindSanctum (ExileCore2) to OriathHub.
    ///     This is the initial scaffold: settings, lifecycle, and an overlay window that reports
    ///     whether we are in a Sanctum area. Room enumeration, weighting, and path drawing are
    ///     added on top of this once the required offsets/API are in place.
    /// </summary>
    public sealed class SanctumPlugin : PluginBase
    {
        private SanctumSettings settings = new();
        private ActiveCoroutine? areaChangeCoroutine;

        // Settings live next to the plugin DLL. DllDirectory is set by the loader before OnEnable.
        private FileInfo SettingsFile => new(Path.Combine(DllDirectory, "config", "settings.json"));

        /// <inheritdoc/>
        public override string Name => "Sanctum";

        /// <inheritdoc/>
        public override string Description => "Pathfinding helper for the Sanctum encounter.";

        /// <inheritdoc/>
        public override string Author => "OriathHub";

        /// <inheritdoc/>
        public override string Version => "0.1.0";

        /// <inheritdoc/>
        public override void OnEnable(bool isGameOpened)
        {
            settings = JsonHelper.CreateOrLoadJsonFile<SanctumSettings>(SettingsFile);
            Log.Info("enabled", this.Name);
            areaChangeCoroutine = StartCoroutine(LogAreaChanges(), "Sanctum.AreaChange");
        }

        /// <inheritdoc/>
        public override void OnDisable()
        {
            areaChangeCoroutine?.Cancel();
            areaChangeCoroutine = null;
        }

        private IEnumerator<Wait> LogAreaChanges()
        {
            while (true)
            {
                yield return new Wait(RemoteEvents.AreaChanged);
                Log.Info("area changed", this.Name);
            }
        }

        /// <inheritdoc/>
        public override void DrawSettings()
        {
            ImGui.Checkbox("Show overlay", ref settings.Show);
            ImGui.Checkbox("Debug info", ref settings.DebugEnable);
            ImGui.ColorEdit4("Path colour", ref settings.PathColor);
        }

        /// <inheritdoc/>
        public override IEnumerable<SettingSearchEntry> GetSearchableSettings() => new[]
        {
            new SettingSearchEntry("Settings", "Show overlay",
                () => ImGui.Checkbox("Show overlay", ref settings.Show), "visible sanctum toggle"),
            new SettingSearchEntry("Settings", "Debug info",
                () => ImGui.Checkbox("Debug info", ref settings.DebugEnable), "room weights ids"),
            new SettingSearchEntry("Settings", "Path colour",
                () => ImGui.ColorEdit4("Path colour", ref settings.PathColor), "color line"),
        };

        /// <inheritdoc/>
        public override void DrawUI()
        {
            if (!settings.Show)
            {
                return;
            }

            if (Core.States.GameCurrentState != GameStateTypes.InGameState)
            {
                return;
            }

            ImGui.SetNextWindowBgAlpha(0.6f);
            if (ImGui.Begin("Sanctum"))
            {
                ImGui.TextDisabled("Scaffold — room pathfinding not implemented yet.");
                ImGui.Text($"Area: {Core.States.InGameStateObject.CurrentAreaInstance.AreaName}");
            }

            ImGui.End();
        }

        /// <inheritdoc/>
        public override void SaveSettings()
        {
            JsonHelper.SaveToFile(settings, SettingsFile);
        }
    }
}
