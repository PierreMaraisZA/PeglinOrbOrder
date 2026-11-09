using System;
using System.Collections.Generic;

namespace PeglinOrbOrder.Core
{
    /// <summary>
    /// The weight of every orb, looked up by its game key (Attack.locNameString, e.g. "stone", "poison_orb").
    /// Weights are 0-100 and only their ratios matter: a higher weight is more likely to be drawn earlier.
    /// </summary>
    public sealed class OrbWeightTable
    {
        public const int DefaultWeight = 50;
        public const int MinWeight = 0;
        public const int MaxWeight = 100;

        private readonly Dictionary<string, int> _weights;

        public OrbWeightTable(IEnumerable<KeyValuePair<string, int>> weights, int defaultWeight = DefaultWeight)
        {
            DefaultForUnknown = Clamp(defaultWeight);
            _weights = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (weights == null)
            {
                return;
            }
            foreach (KeyValuePair<string, int> pair in weights)
            {
                if (!string.IsNullOrEmpty(pair.Key))
                {
                    _weights[pair.Key] = Clamp(pair.Value);
                }
            }
        }

        /// <summary>Weight used for orbs this table has never heard of, so a game update cannot break the mod.</summary>
        public int DefaultForUnknown { get; }

        public int Count => _weights.Count;

        public int For(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return DefaultForUnknown;
            }
            return _weights.TryGetValue(key, out int weight) ? weight : DefaultForUnknown;
        }

        public static int Clamp(int weight)
        {
            if (weight < MinWeight)
            {
                return MinWeight;
            }
            return weight > MaxWeight ? MaxWeight : weight;
        }

        public static OrbWeightTable Defaults() => new OrbWeightTable(DefaultWeights.All);
    }
}
