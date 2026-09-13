namespace OriathHub.Plugins.Sanctum
{
    using System.Collections.Generic;

    /// <summary>
    ///     Room weights for one pathing profile. room type and reward keys
    ///     use the tokens of the SanctumRooms.dat Ids (e.g. "Caverns_Ritual_06" → "Ritual",
    ///     "Caverns_TreasureWaterMinor" → "WaterMinor"). Affliction keys are the in-game display names.
    /// </summary>
    public sealed class ProfileContent
    {
        public Dictionary<string, float> RoomTypeWeights { get; set; } = new();
        public Dictionary<string, float> AfflictionWeights { get; set; } = new();
        public Dictionary<string, float> RewardWeights { get; set; } = new();

        /// <summary>SanctumPersistentEffects.dat CurseDesc text for each AfflictionWeights key, shown as a settings tooltip.</summary>
        public static readonly Dictionary<string, string> AfflictionDescriptions = new()
        {
            ["Black Smoke"] = "You can see one fewer room ahead on the Trial Map",
            ["Blunt Sword"] = "You and your Minions deal 40% less Damage",
            ["Branded Balbalakh"] = "Cannot restore Honour",
            ["Chains of Binding"] = "Monsters inflict Binding Chains on Hit",
            ["Chiselled Stone"] = "Monsters Petrify on Hit",
            ["Corrosive Concoction"] = "You have no Armour, Evasion and Energy Shield",
            ["Costly Aid"] = "Gain a random Minor Affliction when you venerate a Maraketh Shrine",
            ["Dark Pit"] = "Traps deal 100% increased Damage",
            ["Deadly Snare"] = "Traps deal Triple Damage",
            ["Death Toll"] = "Monsters no longer drop Sacred Water,\nand you take increasing Physical damage as you complete more Rooms",
            ["Deceptive Mirror"] = "You are not always taken to the room you select",
            ["Dishonoured Tattoo"] = "100% increased Damage Taken while on Low Life",
            ["Exhausted Wells"] = "Chests no longer grant Sacred Water",
            ["Fiendish Wings"] = "Monsters' Action Speed cannot be slowed below base\nMonsters have 25% increased Attack, Cast and Movement Speed",
            ["Forgotten Traditions"] = "50% reduced Effect of your Non-Unique Relics",
            ["Gate Toll"] = "Lose 30 Sacred Water on room completion",
            ["Ghastly Scythe"] = "Losing Honour ends the Trial (removed after a few Rooms)",
            ["Glass Shard"] = "The next Boon you gain is converted into a random Minor Affliction",
            ["Golden Smoke"] = "Rewards are unknown on the Trial Map",
            ["Haemorrhage"] = "You cannot restore Honour (removed after killing the next Boss)",
            ["Honed Claws"] = "Monsters deal 30% more Damage",
            ["Hungry Fangs"] = "Monsters remove 5% of your Life, Mana and Energy Shield on Hit",
            ["Iron Manacles"] = "You have no Evasion",
            ["Leaking Waterskin"] = "Lose 20 Sacred Water when you take Damage from an Enemy Hit",
            ["Low Rivers"] = "50% less Sacred Water found",
            ["Myriad Aspersions"] = "When you gain an Affliction, gain an additional random Minor Affliction",
            ["Orb of Negation"] = "Non-Unique Relics have no Effect",
            ["Purple Smoke"] = "Afflictions are unknown on the Trial Map",
            ["Rapid Quicksand"] = "Traps are faster",
            ["Red Smoke"] = "Room types are unknown on the Trial Map",
            ["Rusted Mallet"] = "Monsters always Knockback\nMonsters have increased Knockback Distance",
            ["Season of Famine"] = "The Merchant offers 50% fewer choices",
            ["Sharpened Arrowhead"] = "You have no Armour",
            ["Shattered Shield"] = "You have 50% less Energy Shield",
            ["Spiked Exit"] = "Take Physical Damage on Room Completion",
            ["Spiked Shell"] = "Monsters have 50% increased Maximum Life",
            ["Suspected Sympathiser"] = "50% reduced Honour restored",
            ["Tattered Blindfold"] = "90% reduced Light Radius\nMinimap is hidden",
            ["Trade Tariff"] = "50% increased Merchant prices",
            ["Tradition's Demand"] = "The Merchant only offers one choice",
            ["Unassuming Brick"] = "You cannot gain any more Boons",
            ["Unquenched Thirst"] = "You cannot gain Sacred Water",
            ["Untouchable"] = "You are Cursed with Enfeeble",
            ["Veiled Sight"] = "Rooms are unknown on the Trial Map",
            ["Weakened Flesh"] = "25% less Maximum Honour",
            ["Winter Drought"] = "Lose all Sacred Water on floor completion",
            ["Worn Sandals"] = "30% reduced Movement Speed if you've been Hit by an Enemy Recently",
        };

        /// <summary>SanctumRoomTypes.dat descriptions for each RewardWeights key, shown as a settings tooltip.</summary>
        public static readonly Dictionary<string, string> RewardDescriptions = new()
        {
            ["Merchant"] = "Contains Merchant",
            ["LegendHonor"] = "Awards a Shrine to restore Honour",
            ["LegendPledge"] = "Awards a Pledge which can be accepted to change the Trial's Parameters",
            ["LegendWater"] = "Awards a Shrine to restore Honour and gain Sacred Water",
            ["LegendCurse"] = "Awards a Shrine that greatly restores Honour and burdens you with an Affliction",
            ["LegendBoon"] = "Awards a Shrine that restores Honour and grants you a Boon",
            ["LegendRandom"] = "Awards a Shrine that bestows the fickle Blessings of the Wind",
            ["WaterMajor"] = "Awards a Large Sacred Water Fountain",
            ["WaterMinor"] = "Awards a Sacred Water Fountain",
            ["KeyBronze"] = "Awards Bronze Key",
            ["KeySilver"] = "Awards Silver Key",
            ["KeyGold"] = "Awards Gold Key",
            ["ChestBronze"] = "Awards a Bronze Cache. Requires a Bronze Key to Open",
            ["ChestSilver"] = "Awards a Silver Cache. Requires a Silver Key to Open",
            ["ChestGold"] = "Awards a Gold Cache. Requires a Gold Key to Open",
        };

        public static ProfileContent CreateDefaultProfile() => CreateBaseProfile();

        public static ProfileContent CreateNoHitProfile()
        {
            var profile = CreateBaseProfile();

            profile.RoomTypeWeights["Gauntlet"] = -200;
            profile.RoomTypeWeights["Hourglass"] = -1000;

            profile.AfflictionWeights["Death Toll"] = -500000;
            profile.AfflictionWeights["Spiked Exit"] = -600000;
            profile.AfflictionWeights["Deceptive Mirror"] = -400000;
            profile.AfflictionWeights["Glass Shard"] = -50000;
            profile.AfflictionWeights["Myriad Aspersions"] = -50000;

            foreach (var free in new[]
            {
                "Ghastly Scythe", "Deadly Snare", "Branded Balbalakh", "Chiselled Stone", "Weakened Flesh",
                "Costly Aid", "Suspected Sympathiser", "Haemorrhage", "Leaking Waterskin", "Rusted Mallet",
                "Chains of Binding", "Dishonoured Tattoo", "Dark Pit", "Honed Claws", "Hungry Fangs",
            })
            {
                profile.AfflictionWeights[free] = 0;
            }

            return profile;
        }

        public static ProfileContent CreateForName(string name) =>
            name == "No-Hit" ? CreateNoHitProfile() : CreateDefaultProfile();

        public ProfileContent Clone() => new()
        {
            RoomTypeWeights = new Dictionary<string, float>(this.RoomTypeWeights),
            AfflictionWeights = new Dictionary<string, float>(this.AfflictionWeights),
            RewardWeights = new Dictionary<string, float>(this.RewardWeights),
        };

        /// <summary>Copy without null dictionaries, blank keys, or non-finite weights.</summary>
        public ProfileContent Sanitized() => new()
        {
            RoomTypeWeights = Clean(this.RoomTypeWeights),
            AfflictionWeights = Clean(this.AfflictionWeights),
            RewardWeights = Clean(this.RewardWeights),
        };

        private static Dictionary<string, float> Clean(Dictionary<string, float>? weights)
        {
            var result = new Dictionary<string, float>();
            if (weights == null)
            {
                return result;
            }

            foreach (var (key, value) in weights)
            {
                if (!string.IsNullOrWhiteSpace(key) && float.IsFinite(value))
                {
                    result[key] = value;
                }
            }

            return result;
        }

        private static ProfileContent CreateBaseProfile()
        {
            return new ProfileContent
            {
                RoomTypeWeights = new()
                {
                    ["Gauntlet"] = -1000,
                    ["Hourglass"] = -200,
                    ["Chalice"] = 0,
                    ["Ritual"] = 0,
                    ["Escape"] = 100,
                    ["Arena"] = 0,
                    ["Explore"] = 0,
                    ["Lair"] = 0,
                    ["Boss"] = 0,
                },
                AfflictionWeights = new()
                {
                    ["Orbala's Leathers"] = 0,
                    ["Glass Shard"] = -4000,
                    ["Ghastly Scythe"] = -4000,
                    ["Veiled Sight"] = -4000,
                    ["Myriad Aspersions"] = -4000,
                    ["Deceptive Mirror"] = -4000,
                    ["Purple Smoke"] = -4000,
                    ["Golden Smoke"] = -400,
                    ["Red Smoke"] = -4000,
                    ["Black Smoke"] = -4000,
                    ["Rapid Quicksand"] = -1000,
                    ["Deadly Snare"] = -1000,
                    ["Forgotten Traditions"] = -1000,
                    ["Season of Famine"] = -1000,
                    ["Orb of Negation"] = -1000,
                    ["Winter Drought"] = -1000,
                    ["Branded Balbalakh"] = -1000,
                    ["Chiselled Stone"] = -1000,
                    ["Weakened Flesh"] = -100,
                    ["Untouchable"] = -1000,
                    ["Costly Aid"] = -900,
                    ["Blunt Sword"] = -1000,
                    ["Spiked Shell"] = -1000,
                    ["Suspected Sympathiser"] = -200,
                    ["Haemorrhage"] = -100,
                    ["Corrosive Concoction"] = 0,
                    ["Iron Manacles"] = 0,
                    ["Shattered Shield"] = 0,
                    ["Unquenched Thirst"] = -200,
                    ["Unassuming Brick"] = -1000,
                    ["Tradition's Demand"] = -800,
                    ["Fiendish Wings"] = -400,
                    ["Hungry Fangs"] = -600,
                    ["Worn Sandals"] = -400,
                    ["Trade Tariff"] = -300,
                    ["Death Toll"] = -400,
                    ["Spiked Exit"] = -300,
                    ["Exhausted Wells"] = 0,
                    ["Gate Toll"] = -100,
                    ["Leaking Waterskin"] = -100,
                    ["Low Rivers"] = -100,
                    ["Sharpened Arrowhead"] = 0,
                    ["Rusted Mallet"] = 0,
                    ["Chains of Binding"] = 0,
                    ["Dishonoured Tattoo"] = 0,
                    ["Tattered Blindfold"] = 0,
                    ["Dark Pit"] = 0,
                    ["Honed Claws"] = 0,
                },
                RewardWeights = new()
                {
                    ["Merchant"] = 20,
                    ["LegendHonor"] = 0,
                    ["LegendPledge"] = 20,
                    ["LegendWater"] = 8,
                    ["LegendCurse"] = -1,
                    ["LegendBoon"] = 50,
                    ["LegendRandom"] = 300,
                    ["WaterMajor"] = 100,
                    ["WaterMinor"] = 50,
                    ["KeyBronze"] = 0,
                    ["KeySilver"] = 0,
                    ["KeyGold"] = 0,
                    ["ChestBronze"] = 0,
                    ["ChestSilver"] = 0,
                    ["ChestGold"] = 0,
                },
            };
        }
    }
}
