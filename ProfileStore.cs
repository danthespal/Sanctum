namespace OriathHub.Plugins.Sanctum
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json;
    using OriathHub.Utils;

    /// <summary>
    ///     Stores each profile as its own JSON file (<c>&lt;name&gt;.json</c>) in the profiles folder.
    ///     The file name is the profile name.
    /// </summary>
    internal sealed class ProfileStore
    {
        public const string Extension = ".json";
        private const int MaxNameLength = 64;

        public ProfileStore(string directoryPath)
        {
            this.DirectoryPath = directoryPath;
        }

        public string DirectoryPath { get; }

        public Dictionary<string, ProfileContent> LoadAll()
        {
            var result = new Dictionary<string, ProfileContent>(StringComparer.OrdinalIgnoreCase);
            if (!Directory.Exists(this.DirectoryPath))
            {
                return result;
            }

            foreach (var file in Directory.EnumerateFiles(this.DirectoryPath, "*" + Extension))
            {
                try
                {
                    var profile = JsonConvert.DeserializeObject<ProfileContent>(File.ReadAllText(file));
                    if (profile != null)
                    {
                        result[Path.GetFileNameWithoutExtension(file)] = profile.Sanitized();
                    }
                }
                catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
                {
                    Log.Warning($"[Sanctum] Skipped unreadable profile '{file}': {ex.Message}");
                }
            }

            return result;
        }

        public bool TrySave(string name, ProfileContent profile, out string error)
        {
            try
            {
                Directory.CreateDirectory(this.DirectoryPath);
                File.WriteAllText(this.PathFor(name), JsonConvert.SerializeObject(profile.Sanitized(), Formatting.Indented));
                error = string.Empty;
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                error = ex.Message;
                Log.Warning($"[Sanctum] Could not save profile '{name}': {ex.Message}");
                return false;
            }
        }

        public bool TryRename(string oldName, string newName, out string error)
        {
            try
            {
                var from = this.PathFor(oldName);
                var to = this.PathFor(newName);
                if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
                {
                    // Case-only rename: the file system sees one file, so go through a temporary name.
                    var temp = Path.Combine(this.DirectoryPath, Guid.NewGuid().ToString("N") + ".tmp");
                    File.Move(from, temp);
                    try
                    {
                        File.Move(temp, to);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        File.Move(temp, from);
                        throw;
                    }
                }
                else
                {
                    File.Move(from, to);
                }

                error = string.Empty;
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool TryDelete(string name, out string error)
        {
            try
            {
                File.Delete(this.PathFor(name));
                error = string.Empty;
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool IsValidName(string name, out string error)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                error = "Profile name cannot be empty.";
                return false;
            }

            if (name != name.Trim() || name.Length > MaxNameLength || name is "." or ".."
                || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                error = $"'{name}' is not a valid profile name (no \\ / : * ? \" < > |, max {MaxNameLength} chars).";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public static string ToValidName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
            if (cleaned.Length > MaxNameLength)
            {
                cleaned = cleaned[..MaxNameLength].Trim();
            }

            return cleaned is "" or "." or ".." ? "Imported" : cleaned;
        }

        public static string UniqueName(string name, IReadOnlyDictionary<string, ProfileContent> existing)
        {
            if (!existing.ContainsKey(name))
            {
                return name;
            }

            for (var i = 2; ; i++)
            {
                var candidate = $"{name} ({i})";
                if (!existing.ContainsKey(candidate))
                {
                    return candidate;
                }
            }
        }

        private string PathFor(string name) => Path.Combine(this.DirectoryPath, name + Extension);
    }
}
