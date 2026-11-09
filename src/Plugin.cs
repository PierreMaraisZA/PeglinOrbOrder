using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using PeglinOrbOrder.Core;
using UnityEngine;

namespace PeglinOrbOrder
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        // BepInEx names the config file after the GUID, so this gives BepInEx/config/PeglinOrbOrder.cfg.
        public const string Guid = "PeglinOrbOrder";
        public const string Name = "PeglinOrbOrder";
        public const string Version = "1.0.0";

        /// <summary>The Peglin version this mod was built and tested against (Unity's Application.version).</summary>
        public const string SupportedGameVersion = "2.0.12";

        internal const string WeightsFileName = "PeglinOrbOrder.weights.json";

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<bool> ShowWeights;
        internal static ConfigEntry<bool> LogOrder;

        private static OrbWeightsDocument _document;
        private static OrbWeightTable _weights;

        internal static OrbWeightTable Weights => _weights ?? (_weights = OrbWeightTable.Defaults());
        internal static double Exponent => _document?.Exponent ?? 1.0;
        internal static bool Active => Enabled == null || Enabled.Value;

        internal static string WeightsPath => Path.Combine(Paths.ConfigPath, WeightsFileName);

        private void Awake()
        {
            Log = Logger;
            CheckGameVersion();

            Enabled = Config.Bind("General", "Enabled", true,
                "Reorder the sack so heavier orbs (see the weights JSON) are more likely to be drawn earlier. " +
                "Turn this off to play with the game's normal shuffle.");
            ShowWeights = Config.Bind("General", "ShowWeights", true,
                "Show each queued orb's draw weight as a number on it, colour-coded: green above baseline, white at 50, " +
                "amber below, grey at 0 (never first).");
            LogOrder = Config.Bind("General", "LogOrder", false,
                "Write the weighted draw order to the log every time the sack is shuffled.");

            ReloadWeights();

            Harmony harmony = new Harmony(Guid);
            harmony.PatchAll(typeof(Plugin).Assembly);
            int patched = harmony.GetPatchedMethods().Count();
            if (patched < 3)
            {
                Log.LogError($"{Name} only patched {patched} of 3 game methods. Peglin may have changed; the mod will do nothing.");
            }
            else
            {
                Log.LogInfo($"{Name} {Version} loaded; patched {patched} game method(s).");
            }
        }

        /// <summary>
        /// Reads the weights file, creating it with the shipped defaults if it doesn't exist. A broken file never
        /// kills the mod: the built-in defaults are used and the error is logged.
        /// </summary>
        private static void ReloadWeights()
        {
            string path = WeightsPath;
            try
            {
                if (!File.Exists(path))
                {
                    OrbWeightsDocument seeded = OrbWeightsJson.CreateDefault();
                    WriteAtomically(path, OrbWeightsJson.Serialize(seeded));
                    _document = seeded;
                    _weights = seeded.Table;
                    Log.LogInfo($"Wrote default orb weights to {path} ({seeded.Orbs.Count} orbs).");
                    return;
                }

                OrbWeightsDocument parsed = OrbWeightsJson.Parse(File.ReadAllText(path));
                ReportUnknownKeys(parsed);
                int missing = parsed.MissingDefaultCount();
                parsed.WithMissingDefaults();
                _document = parsed;
                _weights = parsed.Table;
                Log.LogInfo($"Loaded orb weights from {path} ({parsed.Orbs.Count} orbs, exponent " +
                    parsed.Exponent.ToString("0.##", CultureInfo.InvariantCulture) +
                    (missing > 0 ? $", {missing} orb(s) filled from defaults" : "") + ").");
            }
            catch (Exception e)
            {
                Log.LogError($"Couldn't read {path}: {e.Message} Using the built-in default weights.");
                _document = OrbWeightsJson.CreateDefault();
                _weights = _document.Table;
            }
        }

        /// <summary>Writes via a temp file so a crash mid-write can't leave a truncated weights file behind.</summary>
        private static void WriteAtomically(string path, string contents)
        {
            string temp = path + ".tmp";
            File.WriteAllText(temp, contents);
            File.Move(temp, path);
        }

        private static void ReportUnknownKeys(OrbWeightsDocument document)
        {
            List<string> unknown = document.UnknownKeys().ToList();
            if (unknown.Count == 0)
            {
                return;
            }
            string sample = string.Join(", ", unknown.Take(5));
            Log.LogWarning($"Weights file has {unknown.Count} orb key(s) this mod doesn't know: {sample}" +
                (unknown.Count > 5 ? ", ..." : "") + ". Check for typos; those lines do nothing.");
        }

        /// <summary>
        /// Warns (always, regardless of logging settings) when Peglin isn't the version this mod was built for.
        /// The mod patches the game's own deck code, so a game update can make it quietly stop working.
        /// </summary>
        private void CheckGameVersion()
        {
            string gameVersion = Application.version;
            if (gameVersion == SupportedGameVersion)
            {
                return;
            }
            Log.LogWarning($"{Name} {Version} was built for Peglin v{SupportedGameVersion}, but this is Peglin v{gameVersion}. " +
                "Orb weights may not apply until the mod is updated for this game version.");
        }
    }
}
