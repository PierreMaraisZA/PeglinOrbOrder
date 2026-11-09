using System;
using System.Collections.Generic;
using System.Linq;
using Battle;
using HarmonyLib;
using PeglinOrbOrder.Core;
using UnityEngine;

namespace PeglinOrbOrder
{
    /// <summary>
    /// The heart of the mod: after the game builds its draw stack, put the orbs back in weighted order.
    ///
    /// The game shuffles the whole deck once per battle (Fisher-Yates, seeded by deckOrderRandomState) and pushes
    /// the result onto <see cref="DeckManager.shuffledDeck"/>, where the top is the next orb drawn. Reordering the
    /// stack afterwards leaves everything else (Mirrorb copies, persistence resets, attack setup, the shuffle event)
    /// exactly as the game made it.
    /// </summary>
    [HarmonyPatch(typeof(DeckManager), nameof(DeckManager.ShuffleCompleteDeck))]
    internal static class DeckShufflePatch
    {
        private static bool _missingRandomLogged;

        [HarmonyPostfix]
        private static void Postfix(DeckManager __instance)
        {
            if (!Plugin.Active)
            {
                return;
            }

            // The same method doubles as the game's uniform "pick a random orb" helper for story and reward effects
            // (DialoguePopulateRandomOrbs and friends). Only reorder an actual battle sack, or those rewards would
            // stop being random.
            if (!BattleController.BattleActive)
            {
                return;
            }

            try
            {
                Func<double> nextUnit = GameRandom(__instance);
                if (nextUnit == null)
                {
                    if (!_missingRandomLogged)
                    {
                        _missingRandomLogged = true;
                        Plugin.Log.LogWarning("No saved deck random state for this battle; leaving the game's own draw order.");
                    }
                    return;
                }
                Reorder(__instance, nextUnit);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Weighted shuffle failed, keeping the game's own order: {e}");
            }
        }

        /// <summary>
        /// Uses the game's saved deck-order RNG so a run reshuffles identically after saving and reloading,
        /// which is how the game keeps its own shuffle stable across saves. Null when the game has no state yet.
        /// </summary>
        internal static Func<double> GameRandom(DeckManager deckManager)
        {
            SerializableRandomState state = deckManager == null ? null : deckManager.deckOrderRandomState;
            if (state == null)
            {
                return null;
            }
            // GetRandomValue returns a float; widen it so the ordering maths stays in double.
            return () => state.GetRandomValue();
        }

        /// <summary>Rewrites the draw stack in weighted order. Does nothing when there is nothing to order.</summary>
        internal static void Reorder(DeckManager deckManager, Func<double> nextUnit)
        {
            Stack<GameObject> stack = deckManager == null ? null : deckManager.shuffledDeck;
            if (stack == null || stack.Count < 2)
            {
                return;
            }

            GameObject[] order = stack.ToArray(); // [0] is drawn next
            WeightedOrder.ShuffleInPlace(order, OrbKeys.Of, Plugin.Weights, Plugin.Exponent, nextUnit);

            stack.Clear();
            for (int i = order.Length - 1; i >= 0; i--)
            {
                stack.Push(order[i]);
            }

            if (Plugin.LogOrder.Value)
            {
                Plugin.Log.LogInfo("Weighted draw order: " + Describe(order));
            }
        }

        /// <summary>Names the first fifteen orbs of the order, and says how many more there were.</summary>
        internal static string Describe(GameObject[] order)
        {
            const int shown = 15;
            string names = string.Join(", ", order.Take(shown).Select(orb => $"{OrbKeys.DisplayName(orb)}={Plugin.Weights.For(OrbKeys.Of(orb))}"));
            return order.Length > shown ? $"{names} (+{order.Length - shown} more)" : names;
        }
    }
}
