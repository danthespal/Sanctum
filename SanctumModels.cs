namespace OriathHub.Plugins.Sanctum
{
    using System.Collections.Generic;
    using System.Numerics;

    /// <summary>
    ///     One room of the Sanctum floor map.
    /// </summary>
    public sealed class SanctumRoom
    {
        public int Layer;
        public int Room;

        /// <summary>Room type parsed from the SanctumRooms Id, e.g. "Ritual", "Boss".</summary>
        public string? RoomType;

        /// <summary>Reward parsed from the SanctumRooms Id, e.g. "WaterMinor", "KeySilver".</summary>
        public string? Reward;

        /// <summary>Affliction display name, e.g. "Rapid Quicksand".</summary>
        public string? Affliction;

        public bool OnTakenPath;
        public bool IsNextChoice;
        public bool IsReachable;
        public bool IsBoss;
        public bool IsRevealed;

        /// <summary>Screen-space top-left corner.</summary>
        public Vector2 ScreenPosition;

        /// <summary>Screen-space size.</summary>
        public Vector2 ScreenSize;

        /// <summary>Room indices in the next layer this room connects to.</summary>
        public readonly List<int> Outgoing = new();
    }

    /// <summary>
    ///     Snapshot of the Sanctum floor window and the player's Sanctum resources.
    /// </summary>
    public sealed class SanctumFloor
    {
        public readonly List<List<SanctumRoom>> Layers = new();

        /// <summary>Deepest room on the taken path, or null when not known.</summary>
        public (int Layer, int Room)? PlayerRoom;

        public int SacredWater;
        public int SacredWaterGained;
        public int HonourLost;
        public int? MaxHonour;
        public int BronzeKeys;
        public int SilverKeys;
        public int GoldKeys;

        public int? CurrentHonour => this.MaxHonour is { } max ? System.Math.Max(0, max - this.HonourLost) : null;

        public SanctumRoom? GetRoom(int layer, int room) =>
            layer >= 0 && layer < this.Layers.Count && room >= 0 && room < this.Layers[layer].Count
                ? this.Layers[layer][room]
                : null;
    }
}
