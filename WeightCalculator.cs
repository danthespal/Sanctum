namespace OriathHub.Plugins.Sanctum
{
    using System.Collections.Generic;
    using System.Text;
    using OriathHub.RemoteEnums;

    /// <summary>
    ///     Scores a room for the selected profile.
    /// </summary>
    internal sealed class WeightCalculator
    {
        private const double BaseWeight = 1000000;
        private readonly StringBuilder debugText = new();

        public (double Weight, string Debug) CalculateRoomWeight(SanctumRoom room, ProfileContent profile, bool withDebug)
        {
            this.debugText.Clear();
            var weight = BaseWeight;
            weight += this.Lookup("Type", room.RoomType, profile.RoomTypeWeights, withDebug);
            weight += this.AfflictionWeight(room.Affliction, profile, withDebug);
            weight += this.Lookup("Reward", room.Reward, profile.RewardWeights, withDebug);

            var connectivity = room.Outgoing.Count > 1 ? 100 : 0;
            if (withDebug)
            {
                this.debugText.AppendLine($"Connectivity: {connectivity}");
            }

            return (weight + connectivity, this.debugText.ToString());
        }

        private double Lookup(string label, string? key, Dictionary<string, float> weights, bool withDebug)
        {
            if (key == null)
            {
                return 0;
            }

            var found = weights.TryGetValue(key, out var value);
            if (withDebug)
            {
                this.debugText.AppendLine(found ? $"{key}: {value}" : $"{label} {key}: 0 (no weight)");
            }

            return found ? value : 0;
        }

        private double AfflictionWeight(string? affliction, ProfileContent profile, bool withDebug)
        {
            if (affliction == null)
            {
                return 0;
            }

            if (DynamicAfflictionWeight(affliction) is { } dynamic)
            {
                if (withDebug)
                {
                    this.debugText.AppendLine($"{affliction}: {dynamic} (dynamic)");
                }

                return dynamic;
            }

            return this.Lookup("Affliction", affliction, profile.AfflictionWeights, withDebug);
        }

        private static double? DynamicAfflictionWeight(string affliction) => affliction switch
        {
            "Iron Manacles" => IronManaclesWeight(),
            "Shattered Shield" => ShatteredShieldWeight(),
            "Worn Sandals" => QueenOfTheForestWeight(),
            "Corrosive Concoction" => (IronManaclesWeight() ?? 0) + (ShatteredShieldWeight() ?? 0),
            _ => null,
        };

        private static double? QueenOfTheForestWeight() =>
            SanctumReader.GetPlayerStat(GameStats.movement_speed_is_only_base_positive_1percentage_per_x_evasion_rating) > 0 ? 0 : null;

        private static double? IronManaclesWeight()
        {
            var evasion = SanctumReader.GetPlayerStat(GameStats.evasion_rating) ?? 0;
            return evasion > 20000 ? -5000 : evasion > 6000 ? -750 : null;
        }

        private static double? ShatteredShieldWeight()
        {
            var energyShield = SanctumReader.GetPlayerStat(GameStats.maximum_energy_shield) ?? 0;
            return energyShield > 6000 ? -5000 : energyShield > 1000 ? -750 : null;
        }
    }
}
