namespace OriathHub.Plugins.Sanctum
{
    using System;
    using System.Collections.Generic;
    using OriathHub.RemoteEnums;
    using OriathHub.RemoteObjects.Components;
    using OriathHub.RemoteObjects.UiElement;

    /// <summary>
    ///     Builds a <see cref="SanctumFloor" /> snapshot from the host Sanctum API. Requires a lease from
    ///     <c>ImportantUiElements.RequestSanctumData()</c>.
    /// </summary>
    internal sealed class SanctumReader
    {
        private readonly Dictionary<(int, int), (string? Type, string? Reward, string? Affliction)> knownRooms = new();
        private string areaHash = string.Empty;

        /// <summary>
        ///     Reads the floor, or returns null when the floor map is not open.
        /// </summary>
        public SanctumFloor? Read()
        {
            if (Core.States.GameCurrentState != GameStateTypes.InGameState)
            {
                return null;
            }

            var inGame = Core.States.InGameStateObject;
            var area = inGame.CurrentAreaInstance;
            if (area.AreaHash != this.areaHash)
            {
                this.areaHash = area.AreaHash;
                this.knownRooms.Clear();
            }

            var gameUi = inGame.GameUi;
            var layers = gameUi.SanctumRoomsByLayer;
            if (!gameUi.IsSanctumFloorWindowOpen || layers.Count == 0)
            {
                return null;
            }

            var floor = new SanctumFloor();
            for (var layerIndex = 0; layerIndex < layers.Count; layerIndex++)
            {
                var rooms = new List<SanctumRoom>(layers[layerIndex].Count);
                floor.Layers.Add(rooms);
                for (var roomIndex = 0; roomIndex < layers[layerIndex].Count; roomIndex++)
                {
                    rooms.Add(this.ToRoom(layers[layerIndex][roomIndex], layerIndex, roomIndex));
                }
            }

            foreach (var connection in gameUi.SanctumRoomConnections)
            {
                if (floor.GetRoom(connection.From.Layer, connection.From.RoomIndex) is { } from
                    && connection.To.Layer == from.Layer + 1
                    && !from.Outgoing.Contains(connection.To.RoomIndex))
                {
                    from.Outgoing.Add(connection.To.RoomIndex);
                }
            }

            if (gameUi.SanctumCurrentRoom is { } current)
            {
                floor.PlayerRoom = (current.Layer, current.RoomIndex);
            }

            var state = area.ServerDataObject.SanctumState;
            if (state.IsAvailable)
            {
                floor.SacredWater = state.SacredWater;
                floor.SacredWaterGained = state.SacredWaterGained;
                floor.HonourLost = state.HonourLost;
                floor.BronzeKeys = state.BronzeKeys;
                floor.SilverKeys = state.SilverKeys;
                floor.GoldKeys = state.GoldKeys;
            }

            floor.MaxHonour = GetPlayerStat(GameStats.total_sanctum_honour);
            return floor;
        }

        /// <summary>
        ///     Reads a stat from the player's Stats component, or null when absent.
        /// </summary>
        public static int? GetPlayerStat(GameStats stat)
        {
            if (!Core.States.InGameStateObject.CurrentAreaInstance.Player.TryGetComponent<Stats>(out var stats))
            {
                return null;
            }

            if (stats.StatsChangedByBuffAndActions.TryGetValue(stat, out var value)
                || stats.StatsChangedByItems.TryGetValue(stat, out value))
            {
                return value;
            }

            return null;
        }

        private SanctumRoom ToRoom(SanctumRoomUiElement element, int layer, int roomIndex)
        {
            var key = (layer, roomIndex);
            this.knownRooms.TryGetValue(key, out var known);
            var room = new SanctumRoom
            {
                Layer = layer,
                Room = roomIndex,
                OnTakenPath = element.IsOnTakenPath,
                IsNextChoice = element.IsNextChoice,
                IsReachable = element.IsReachable,
                IsBoss = element.IsBoss,
                IsRevealed = element.IsRevealed,
                ScreenPosition = element.Position,
                ScreenSize = element.Size,
                RoomType = ParseIdToken(element.FightRoomId, null) ?? known.Type,
                Reward = ParseIdToken(element.RewardRoomId, "Treasure") ?? known.Reward,
                Affliction = string.IsNullOrEmpty(element.AfflictionName) ? known.Affliction : element.AfflictionName,
            };

            // Remember what was seen so rooms hidden by smoke afflictions keep their last known contents.
            this.knownRooms[key] = (room.RoomType, room.Reward, room.Affliction);
            return room;
        }

        // SanctumRooms Ids look like "Caverns_Ritual_06" or "Caverns_TreasureWaterMinor".
        private static string? ParseIdToken(string id, string? stripPrefix)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            var parts = id.Split('_');
            var token = parts.Length > 1 ? parts[1] : id;
            if (stripPrefix != null && token.StartsWith(stripPrefix, StringComparison.Ordinal))
            {
                token = token[stripPrefix.Length..];
            }

            return token.Length > 0 ? token : null;
        }
    }
}
