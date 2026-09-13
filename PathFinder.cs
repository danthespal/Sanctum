namespace OriathHub.Plugins.Sanctum
{
    using System.Collections.Generic;

    /// <summary>
    ///     Result of a pathing pass over one floor snapshot.
    /// </summary>
    internal sealed class SanctumPlan
    {
        public readonly Dictionary<(int, int), double> Weights = new();
        public readonly Dictionary<(int, int), string> DebugTexts = new();

        /// <summary>Best path from the player's room (inclusive) to the boss.</summary>
        public readonly List<(int Layer, int Room)> BestPath = new();
    }

    /// <summary>
    ///     Finds the maximum-weight route to the boss. The floor is a layered graph where every edge
    ///     goes from layer N to N+1, so a single backward pass gives the same result as the original
    ///     plugin's priority-queue search.
    /// </summary>
    internal static class PathFinder
    {
        public static SanctumPlan Compute(SanctumFloor floor, ProfileContent profile, WeightCalculator calculator, bool withDebug)
        {
            var plan = new SanctumPlan();
            foreach (var layer in floor.Layers)
            {
                foreach (var room in layer)
                {
                    var (weight, debug) = calculator.CalculateRoomWeight(room, profile, withDebug);
                    plan.Weights[(room.Layer, room.Room)] = weight;
                    plan.DebugTexts[(room.Layer, room.Room)] = debug;
                }
            }

            var best = new Dictionary<(int, int), double>();
            var next = new Dictionary<(int, int), int>();
            for (var layerIndex = floor.Layers.Count - 1; layerIndex >= 0; layerIndex--)
            {
                foreach (var room in floor.Layers[layerIndex])
                {
                    var key = (room.Layer, room.Room);
                    if (layerIndex == floor.Layers.Count - 1)
                    {
                        best[key] = plan.Weights[key];
                        continue;
                    }

                    var bestChild = -1;
                    var bestChildScore = double.MinValue;
                    foreach (var child in room.Outgoing)
                    {
                        if (best.TryGetValue((layerIndex + 1, child), out var score) && score > bestChildScore)
                        {
                            bestChildScore = score;
                            bestChild = child;
                        }
                    }

                    if (bestChild >= 0)
                    {
                        best[key] = plan.Weights[key] + bestChildScore;
                        next[key] = bestChild;
                    }
                }
            }

            var current = floor.PlayerRoom ?? (0, 0);
            if (!best.ContainsKey(current))
            {
                return plan;
            }

            plan.BestPath.Add(current);
            while (next.TryGetValue(current, out var child))
            {
                current = (current.Item1 + 1, child);
                plan.BestPath.Add(current);
            }

            return plan;
        }
    }
}
