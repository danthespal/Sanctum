namespace OriathHub.Plugins.Sanctum
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Coroutine;
    using ImGuiNET;
    using OriathHub.CoroutineEvents;
    using OriathHub.Plugin;
    using OriathHub.RemoteObjects.States.InGameStateObjects;
    using OriathHub.Utils;

    /// <summary>
    ///     Sanctum pathfinding plugin
    ///     Highlights the best route to the boss on the Sanctum floor map and shows the player's
    ///     Sanctum resources. Weight profiles live as JSON files in the plugin's profiles folder.
    /// </summary>
    public sealed class SanctumPlugin : PluginBase
    {
        private const string DeletePopupId = "Delete Sanctum profile?";

        private readonly SanctumReader reader = new();
        private readonly WeightCalculator calculator = new();
        private SanctumSettings settings = new();
        private Dictionary<string, ProfileContent> profiles = new(StringComparer.OrdinalIgnoreCase);
        private ProfileStore? store;
        private IDisposable? sanctumLease;
        private ActiveCoroutine? updateCoroutine;
        private SanctumFloor? floor;
        private SanctumPlan? plan;

        private string newProfileName = string.Empty;
        private bool copyCurrentOnCreate = true;
        private string saveTargetName = string.Empty;
        private string profileStatus = string.Empty;

        private FileInfo SettingsFile => new(Path.Combine(DllDirectory, "config", "settings.json"));

        private ProfileContent ActiveProfile => profiles[settings.CurrentProfile];

        /// <inheritdoc/>
        public override string Name => "Sanctum";

        /// <inheritdoc/>
        public override string Description => "Highlights the best route through the Sanctum floor map.";

        /// <inheritdoc/>
        public override string Author => "OriathHub";

        /// <inheritdoc/>
        public override string Version => "1.0.0";

        /// <inheritdoc/>
        public override void OnEnable(bool isGameOpened)
        {
            store = null;
            EnsureLoaded();
            try
            {
                sanctumLease = AcquireSanctumLease();
            }
            catch (Exception ex) when (ex is MissingMethodException or MissingMemberException or TypeLoadException)
            {
                Log.Error($"[Sanctum] This plugin needs OriathHub SDK 0.16.5 or newer: {ex.Message}");
                return;
            }

            updateCoroutine = StartCoroutine(UpdateLoop(), "Sanctum.Update");
        }

        /// <inheritdoc/>
        public override void OnDisable()
        {
            updateCoroutine?.Cancel();
            updateCoroutine = null;
            sanctumLease?.Dispose();
            sanctumLease = null;
            floor = null;
            plan = null;
        }

        /// <inheritdoc/>
        public override void SaveSettings()
        {
            SaveSettingsFile();
            if (store == null)
            {
                return;
            }

            foreach (var (name, profile) in profiles)
            {
                store.TrySave(name, profile, out _);
            }
        }

        // The host draws settings for disabled plugins too, possibly before OnEnable ever ran.
        private void EnsureLoaded()
        {
            if (store != null)
            {
                return;
            }

            settings = JsonHelper.CreateOrLoadJsonFile<SanctumSettings>(SettingsFile);
            store = new ProfileStore(Path.Combine(DllDirectory, "profiles"));
            LoadProfiles();
        }

        private void LoadProfiles()
        {
            profiles = store!.LoadAll();
            if (profiles.Count == 0)
            {
                foreach (var name in new[] { "Default", "No-Hit" })
                {
                    profiles[name] = ProfileContent.CreateForName(name);
                    store.TrySave(name, profiles[name], out _);
                }
            }

            settings.CurrentProfile = profiles.Keys.FirstOrDefault(
                name => string.Equals(name, settings.CurrentProfile, StringComparison.OrdinalIgnoreCase)) ?? FirstProfileName();
        }

        // Separate, non-inlined method so a host without the Sanctum API fails here (catchable in OnEnable)
        // instead of while OnEnable itself is being compiled.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IDisposable AcquireSanctumLease() => ImportantUiElements.RequestSanctumData();

        private string FirstProfileName() => profiles.Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).First();

        private IEnumerator<Wait> UpdateLoop()
        {
            string? lastError = null;
            while (true)
            {
                yield return new Wait(OriathEvents.PerFrameDataUpdate);
                try
                {
                    floor = settings.Show || settings.ShowInfoWindow ? reader.Read() : null;
                    plan = floor != null
                        ? PathFinder.Compute(floor, ActiveProfile, calculator, settings.DebugEnable)
                        : null;
                    lastError = null;
                }
                catch (Exception ex)
                {
                    // An exception escaping a coroutine aborts the host's per-frame event dispatch for
                    // everyone else, so contain it here and log each distinct failure once.
                    floor = null;
                    plan = null;
                    var message = ex.ToString();
                    if (message != lastError)
                    {
                        Log.Error($"[Sanctum] Update failed: {ex}");
                        lastError = message;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override void DrawUI()
        {
            if (floor == null || plan == null)
            {
                return;
            }

            if (settings.Show)
            {
                DrawPath(floor, plan);
            }

            if (settings.DebugEnable)
            {
                DrawDebug(floor, plan);
            }

            if (settings.ShowInfoWindow)
            {
                DrawInfoWindow(floor, plan);
            }
        }

        private void DrawPath(SanctumFloor current, SanctumPlan currentPlan)
        {
            var drawList = ImGui.GetBackgroundDrawList();
            var color = ImGui.GetColorU32(settings.BestPathColor);
            foreach (var (layer, roomIndex) in currentPlan.BestPath)
            {
                if (current.PlayerRoom == (layer, roomIndex) || current.GetRoom(layer, roomIndex) is not { } room)
                {
                    continue;
                }

                drawList.AddRect(room.ScreenPosition, room.ScreenPosition + room.ScreenSize, color, 0f, ImDrawFlags.None, settings.FrameThickness);
            }
        }

        private void DrawDebug(SanctumFloor current, SanctumPlan currentPlan)
        {
            var drawList = ImGui.GetBackgroundDrawList();
            var textColor = ImGui.GetColorU32(settings.TextColor);
            var backgroundColor = ImGui.GetColorU32(settings.BackgroundColor);
            foreach (var room in current.Layers.SelectMany(layer => layer))
            {
                var key = (room.Layer, room.Room);
                var text = $"Weight: {currentPlan.Weights.GetValueOrDefault(key):F0}\n{currentPlan.DebugTexts.GetValueOrDefault(key)}";
                var size = ImGui.CalcTextSize(text);
                drawList.AddRectFilled(room.ScreenPosition, room.ScreenPosition + size, backgroundColor);
                drawList.AddText(room.ScreenPosition, textColor, text);
            }
        }

        private void DrawInfoWindow(SanctumFloor current, SanctumPlan currentPlan)
        {
            ImGui.SetNextWindowBgAlpha(0.6f);
            if (ImGui.Begin("Sanctum", ImGuiWindowFlags.AlwaysAutoResize))
            {
                var honour = current.CurrentHonour is { } cur ? $"{cur} / {current.MaxHonour}" : $"? (lost {current.HonourLost})";
                ImGui.Text($"Honour: {honour}");
                ImGui.Text($"Sacred Water: {current.SacredWater} (+{current.SacredWaterGained})");
                ImGui.Text($"Keys: Bronze {current.BronzeKeys}  Silver {current.SilverKeys}  Gold {current.GoldKeys}");
                ImGui.Text(current.PlayerRoom is { } p ? $"Current room: L{p.Layer} R{p.Room}" : "Current room: ?");
                ImGui.Text($"Profile: {settings.CurrentProfile}");
                ImGui.TextDisabled("Path: " + string.Join(" > ", currentPlan.BestPath.Select(r => $"L{r.Layer}R{r.Room}")));
            }

            ImGui.End();
        }

        /// <inheritdoc/>
        public override void DrawSettings()
        {
            EnsureLoaded();
            ImGui.Checkbox("Show best path", ref settings.Show);
            ImGui.Checkbox("Show info window", ref settings.ShowInfoWindow);
            ImGui.Checkbox("Debug weights", ref settings.DebugEnable);

            DrawProfileManagement();

            ImGui.ColorEdit4("Path colour", ref settings.BestPathColor);
            ImGui.SliderFloat("Frame thickness", ref settings.FrameThickness, 0f, 10f);
            ImGui.ColorEdit4("Debug text colour", ref settings.TextColor);
            ImGui.ColorEdit4("Debug background", ref settings.BackgroundColor);

            var profile = ActiveProfile;
            DrawWeights("Room type weights", profile.RoomTypeWeights, ref settings.RoomTypeWeightsOpen);
            DrawWeights("Affliction weights", profile.AfflictionWeights, ref settings.AfflictionWeightsOpen, ProfileContent.AfflictionDescriptions);
            DrawWeights("Reward weights", profile.RewardWeights, ref settings.RewardWeightsOpen, ProfileContent.RewardDescriptions);
        }

        /// <inheritdoc/>
        public override IEnumerable<SettingSearchEntry> GetSearchableSettings() => new[]
        {
            new SettingSearchEntry("Settings", "Show best path",
                () => ImGui.Checkbox("Show best path", ref settings.Show), "sanctum route frame"),
            new SettingSearchEntry("Settings", "Show info window",
                () => ImGui.Checkbox("Show info window", ref settings.ShowInfoWindow), "honour sacred water keys"),
            new SettingSearchEntry("Settings", "Debug weights",
                () => ImGui.Checkbox("Debug weights", ref settings.DebugEnable), "room weights"),
        };

        private void DrawProfileManagement()
        {
            ImGui.SetNextItemOpen(settings.ProfilesSectionOpen, ImGuiCond.Always);
            if (!RememberOpen(ImGui.CollapsingHeader("Profiles"), ref settings.ProfilesSectionOpen))
            {
                return;
            }

            ImGui.TextDisabled("Each profile is a JSON file in the plugin's profiles folder.");

            if (ImGui.BeginCombo("Active profile", settings.CurrentProfile))
            {
                foreach (var name in profiles.Keys.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList())
                {
                    if (ImGui.Selectable(name, name == settings.CurrentProfile))
                    {
                        settings.CurrentProfile = name;
                        SaveSettingsFile();
                    }
                }

                ImGui.EndCombo();
            }

            if (ImGui.Button("Reset to defaults"))
            {
                profiles[settings.CurrentProfile] = ProfileContent.CreateForName(settings.CurrentProfile);
                SaveProfile(settings.CurrentProfile, $"Reset '{settings.CurrentProfile}' to defaults.");
            }

            ImGui.SameLine();
            if (ImGui.Button("Rename profile"))
            {
                renameProfileName = settings.CurrentProfile;
                renameError = string.Empty;
                ImGui.OpenPopup(RenamePopupId);
            }

            if (profiles.Count > 1)
            {
                ImGui.SameLine();
                if (ImGui.Button("Delete profile"))
                {
                    ImGui.OpenPopup(DeletePopupId);
                }
            }

            if (ImGui.BeginPopupModal(RenamePopupId))
            {
                ImGui.Text($"Rename profile '{settings.CurrentProfile}' to:");
                ImGui.InputText("##SanctumRenameProfile", ref renameProfileName, 64);
                if (ImGui.Button("Rename") && RenameActiveProfile())
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
                if (ImGui.Button("Cancel##SanctumRename"))
                {
                    ImGui.CloseCurrentPopup();
                }

                if (renameError.Length > 0)
                {
                    ImGui.TextDisabled(renameError);
                }

                ImGui.EndPopup();
            }

            if (ImGui.BeginPopupModal(DeletePopupId))
            {
                ImGui.Text($"Delete profile '{settings.CurrentProfile}' and its file? This cannot be undone.");
                if (ImGui.Button("Delete"))
                {
                    DeleteActiveProfile();
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            ImGui.Separator();
            ImGui.Text("New profile");
            ImGui.InputText("Name##SanctumNewProfile", ref newProfileName, 64);
            ImGui.Checkbox("Copy current weights##SanctumNewProfile", ref copyCurrentOnCreate);
            if (ImGui.Button("Create profile"))
            {
                CreateProfile();
            }

            ImGui.Separator();
            ImGui.Text($"Save weights of '{settings.CurrentProfile}' to");
            ImGui.InputText("Target##SanctumSaveProfile", ref saveTargetName, 64);
            ImGui.SameLine();
            if (ImGui.BeginCombo("##SanctumSaveProfilePick", string.Empty, ImGuiComboFlags.NoPreview))
            {
                foreach (var name in profiles.Keys.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList())
                {
                    if (ImGui.Selectable(name))
                    {
                        saveTargetName = name;
                    }
                }

                ImGui.EndCombo();
            }

            if (ImGui.Button("Save to profile"))
            {
                SaveToProfile();
            }

            ImGui.Separator();
            ImGui.Text("Share");
            if (ImGui.Button("Export to clipboard"))
            {
                ImGui.SetClipboardText(ProfileSharing.Export(settings.CurrentProfile, ActiveProfile));
                profileStatus = $"Copied '{settings.CurrentProfile}' to the clipboard.";
            }

            ImGui.SameLine();
            if (ImGui.Button("Import from clipboard"))
            {
                ImportFromClipboard();
            }

            if (profileStatus.Length > 0)
            {
                ImGui.TextDisabled(profileStatus);
            }

            ImGui.Separator();
        }

        private void CreateProfile()
        {
            var name = newProfileName.Trim();
            if (!ProfileStore.IsValidName(name, out var error))
            {
                profileStatus = error;
                return;
            }

            if (profiles.ContainsKey(name))
            {
                profileStatus = $"Profile '{name}' already exists.";
                return;
            }

            profiles[name] = copyCurrentOnCreate ? ActiveProfile.Clone() : ProfileContent.CreateDefaultProfile();
            if (!SaveProfile(name, $"Created '{name}'."))
            {
                profiles.Remove(name);
                return;
            }

            settings.CurrentProfile = name;
            newProfileName = string.Empty;
            SaveSettingsFile();
        }

        private void SaveToProfile()
        {
            var target = saveTargetName.Trim();
            if (!ProfileStore.IsValidName(target, out var error))
            {
                profileStatus = error;
                return;
            }

            var existingName = profiles.Keys.FirstOrDefault(name => string.Equals(name, target, StringComparison.OrdinalIgnoreCase));
            var finalName = existingName ?? target;
            if (finalName == settings.CurrentProfile)
            {
                SaveProfile(finalName, $"Saved '{finalName}'.");
                return;
            }

            profiles[finalName] = ActiveProfile.Clone();
            SaveProfile(finalName, existingName != null
                ? $"Overwrote '{finalName}' with '{settings.CurrentProfile}'."
                : $"Created '{finalName}' from '{settings.CurrentProfile}'.");
        }

        private const string RenamePopupId = "Rename Sanctum profile";
        private string renameProfileName = string.Empty;
        private string renameError = string.Empty;

        private bool RenameActiveProfile()
        {
            var oldName = settings.CurrentProfile;
            var newName = renameProfileName.Trim();
            if (newName == oldName)
            {
                return true;
            }

            if (!ProfileStore.IsValidName(newName, out var error))
            {
                renameError = error;
                return false;
            }

            if (!string.Equals(newName, oldName, StringComparison.OrdinalIgnoreCase) && profiles.ContainsKey(newName))
            {
                renameError = $"Profile '{newName}' already exists.";
                return false;
            }

            if (!store!.TryRename(oldName, newName, out error))
            {
                renameError = $"Could not rename: {error}";
                return false;
            }

            var profile = profiles[oldName];
            profiles.Remove(oldName);
            profiles[newName] = profile;
            settings.CurrentProfile = newName;
            SaveSettingsFile();
            profileStatus = $"Renamed '{oldName}' to '{newName}'.";
            return true;
        }

        private void DeleteActiveProfile()
        {
            var name = settings.CurrentProfile;
            if (!store!.TryDelete(name, out var error))
            {
                profileStatus = $"Could not delete '{name}': {error}";
                return;
            }

            profiles.Remove(name);
            settings.CurrentProfile = FirstProfileName();
            SaveSettingsFile();
            profileStatus = $"Deleted '{name}'.";
        }

        private void ImportFromClipboard()
        {
            if (!ProfileSharing.TryImport(ImGui.GetClipboardText(), out var importedName, out var imported, out var error))
            {
                profileStatus = error;
                return;
            }

            var finalName = ProfileStore.UniqueName(ProfileStore.ToValidName(importedName), profiles);
            profiles[finalName] = imported;
            if (!SaveProfile(finalName, finalName == importedName
                    ? $"Imported '{finalName}'."
                    : $"Imported '{importedName}' as '{finalName}'."))
            {
                profiles.Remove(finalName);
                return;
            }

            settings.CurrentProfile = finalName;
            SaveSettingsFile();
        }

        private bool SaveProfile(string name, string successMessage)
        {
            if (store!.TrySave(name, profiles[name], out var error))
            {
                profileStatus = successMessage;
                return true;
            }

            profileStatus = $"Could not save '{name}': {error}";
            return false;
        }

        private void SaveSettingsFile() => JsonHelper.SaveToFile(settings, SettingsFile);

        private bool RememberOpen(bool isOpen, ref bool stored)
        {
            if (isOpen != stored)
            {
                stored = isOpen;
                SaveSettingsFile();
            }

            return isOpen;
        }

        private void DrawWeights(string label, Dictionary<string, float> weights, ref bool open, Dictionary<string, string>? descriptions = null)
        {
            ImGui.SetNextItemOpen(open, ImGuiCond.Always);
            if (!RememberOpen(ImGui.TreeNode(label), ref open))
            {
                return;
            }

            var boxWidth = ImGui.CalcTextSize("00000000000000000000").X + (ImGui.GetStyle().FramePadding.X * 2);
            var maxLabelWidth = weights.Keys.Select(k => ImGui.CalcTextSize(k).X).DefaultIfEmpty(0f).Max();
            var inputX = ImGui.GetCursorPosX() + maxLabelWidth + (ImGui.GetStyle().ItemSpacing.X * 3);

            foreach (var key in weights.Keys.OrderBy(k => k).ToList())
            {
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(key);

                if (descriptions != null && descriptions.TryGetValue(key, out var description) && ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.TextUnformatted(description);
                    ImGui.EndTooltip();
                }

                ImGui.SameLine(inputX);
                ImGui.SetNextItemWidth(boxWidth);
                var value = weights[key];
                if (ImGui.InputFloat($"##{key}", ref value) && float.IsFinite(value))
                {
                    weights[key] = value;
                }

                if (ImGui.IsItemDeactivatedAfterEdit())
                {
                    SaveProfile(settings.CurrentProfile, $"Saved '{settings.CurrentProfile}'.");
                }
            }

            ImGui.TreePop();
        }
    }
}
