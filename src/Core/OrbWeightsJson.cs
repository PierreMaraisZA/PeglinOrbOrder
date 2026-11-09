using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PeglinOrbOrder.Core
{
    /// <summary>The on-disk weights file (BepInEx/config/PeglinOrbOrder.weights.json).</summary>
    public sealed class OrbWeightsDocument
    {
        [JsonProperty("readme", Order = 0)]
        public string Readme = WeightsFileReadme;

        // Long rather than int, so a stray 2147483648 is clamped instead of failing the whole file.
        /// <summary>Weight used for orb keys the mod doesn't know.</summary>
        [JsonProperty("defaultWeight", Order = 1)]
        public long DefaultWeight = OrbWeightTable.DefaultWeight;

        /// <summary>0 = every orb equally likely, 1 = weights as written, higher = a stronger bias.</summary>
        [JsonProperty("exponent", Order = 2)]
        public double Exponent = 1.0;

        // Long rather than int so a stray 2147483648 is clamped per entry instead of failing the whole file.
        [JsonProperty("orbs", Order = 3)]
        public Dictionary<string, long> Orbs = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        public const string WeightsFileReadme =
            "Draw weights for Peglin orbs, 0-100. Higher weights are drawn earlier more often, and the roll is fresh each " +
            "battle. 50 is the baseline (Pebballs and plain damage orbs). Support orbs sit at 60-100 so they tend to open " +
            "the battle. 0 means never first, always drawn last (no shipped orb uses it). 'exponent' sharpens or softens " +
            "every weight: 0 is a plain shuffle (orbs set to 0 still come last), 1 uses the weights as written, 2 makes " +
            "each point count twice as much. Orb keys this file leaves out keep their shipped weight; 'defaultWeight' " +
            "covers orb keys the mod doesn't know, such as one added by a game update.";

        private OrbWeightTable _table;

        [JsonIgnore]
        public OrbWeightTable Table
        {
            get
            {
                if (_table == null)
                {
                    Dictionary<string, int> weights = new Dictionary<string, int>(Orbs.Count, StringComparer.OrdinalIgnoreCase);
                    foreach (KeyValuePair<string, long> pair in Orbs)
                    {
                        weights[pair.Key] = (int)Math.Max(OrbWeightTable.MinWeight, Math.Min(OrbWeightTable.MaxWeight, pair.Value));
                    }
                    _table = new OrbWeightTable(weights, (int)DefaultWeight);
                }
                return _table;
            }
        }

        /// <summary>The orb keys in the file that the mod doesn't know, so a typo can be pointed out once.</summary>
        public IEnumerable<string> UnknownKeys()
        {
            foreach (string key in Orbs.Keys)
            {
                if (!DefaultWeights.All.ContainsKey(key))
                {
                    yield return key;
                }
            }
        }

        /// <summary>Clamps everything into the documented ranges so a hand-edited file can never break the shuffle.</summary>
        public OrbWeightsDocument Sanitize()
        {
            DefaultWeight = Math.Max(OrbWeightTable.MinWeight, Math.Min(OrbWeightTable.MaxWeight, DefaultWeight));

            if (double.IsNaN(Exponent) || double.IsInfinity(Exponent))
            {
                Exponent = 1.0;
            }
            Exponent = Math.Max(OrbWeightsJson.MinExponent, Math.Min(OrbWeightsJson.MaxExponent, Exponent));

            Dictionary<string, long> cleaned = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            if (Orbs != null)
            {
                foreach (KeyValuePair<string, long> pair in Orbs)
                {
                    if (!string.IsNullOrEmpty(pair.Key))
                    {
                        cleaned[pair.Key] = Math.Max(OrbWeightTable.MinWeight, Math.Min(OrbWeightTable.MaxWeight, pair.Value));
                    }
                }
            }
            Orbs = cleaned;
            _table = null;
            return this;
        }

        /// <summary>Fills in defaults for orbs the file doesn't mention, without touching the values it does.</summary>
        public OrbWeightsDocument WithMissingDefaults()
        {
            if (Orbs == null)
            {
                Orbs = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            }
            foreach (KeyValuePair<string, int> pair in DefaultWeights.All)
            {
                if (!Orbs.ContainsKey(pair.Key))
                {
                    Orbs[pair.Key] = pair.Value;
                }
            }
            _table = null;
            return this;
        }

        public int MissingDefaultCount()
        {
            int missing = 0;
            foreach (string key in DefaultWeights.All.Keys)
            {
                if (Orbs == null || !Orbs.ContainsKey(key))
                {
                    missing++;
                }
            }
            return missing;
        }
    }

    /// <summary>Reading and writing the weights JSON. Kept free of Unity and BepInEx so it can be unit tested.</summary>
    public static class OrbWeightsJson
    {
        public const double MinExponent = 0.0;
        public const double MaxExponent = 4.0;

        public static OrbWeightsDocument CreateDefault()
        {
            OrbWeightsDocument document = new OrbWeightsDocument
            {
                DefaultWeight = OrbWeightTable.DefaultWeight,
                Exponent = 1.0,
            };
            foreach (KeyValuePair<string, int> pair in DefaultWeights.All)
            {
                document.Orbs[pair.Key] = pair.Value;
            }
            return document;
        }

        /// <summary>Throws JsonException (or JsonSerializationException) if the text isn't a weights file.</summary>
        public static OrbWeightsDocument Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new JsonException("The weights file is empty.");
            }
            OrbWeightsDocument document = JsonConvert.DeserializeObject<OrbWeightsDocument>(json);
            if (document == null)
            {
                throw new JsonException("The weights file did not contain a JSON object.");
            }
            return document.Sanitize();
        }

        public static string Serialize(OrbWeightsDocument document)
        {
            return JsonConvert.SerializeObject(document.Sanitize(), Formatting.Indented);
        }
    }
}
