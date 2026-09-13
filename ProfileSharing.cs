namespace OriathHub.Plugins.Sanctum
{
    using System;
    using System.Text;
    using Newtonsoft.Json;

    /// <summary>
    ///     Clipboard exchange format for sharing profiles: Base64 of UTF-8 JSON.
    /// </summary>
    internal static class ProfileSharing
    {
        public const string FormatTag = "OriathHub.Sanctum.Profile/1";

        private const string NotAProfile = "Clipboard does not contain a Sanctum profile.";

        public static string Export(string name, ProfileContent profile)
        {
            var json = JsonConvert.SerializeObject(new SharedProfile { Format = FormatTag, Name = name, Profile = profile.Sanitized() });
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        public static bool TryImport(string? text, out string name, out ProfileContent profile, out string error)
        {
            name = string.Empty;
            profile = new ProfileContent();
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(text))
            {
                error = "Clipboard is empty.";
                return false;
            }

            SharedProfile? shared;
            try
            {
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(text.Trim()));
                shared = JsonConvert.DeserializeObject<SharedProfile>(json);
            }
            catch (Exception ex) when (ex is FormatException or JsonException)
            {
                error = NotAProfile;
                return false;
            }

            if (shared?.Format != FormatTag || shared.Profile == null)
            {
                error = NotAProfile;
                return false;
            }

            name = string.IsNullOrWhiteSpace(shared.Name) ? "Imported" : shared.Name.Trim();
            profile = shared.Profile.Sanitized();
            return true;
        }

        private sealed class SharedProfile
        {
            public string? Format { get; set; }
            public string? Name { get; set; }
            public ProfileContent? Profile { get; set; }
        }
    }
}
