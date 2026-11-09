using System;
using System.Globalization;
using System.Reflection;
using HarmonyLib;
using PeglinOrbOrder.Core;
using PeglinUI.OrbDisplay;
using TMPro;
using UnityEngine;

namespace PeglinOrbOrder
{
    /// <summary>
    /// Puts each queued orb's weight on it, so the bias can be seen rather than inferred.
    ///
    /// Every orb waiting in the plunger is an <see cref="UpcomingOrbDisplay"/> with a text label of its own (the one
    /// the game uses for the "x3" persistence badge). Cloning just that text object gives a number in the game's own
    /// font, material and sorting order, without touching the badge or the orb itself.
    /// </summary>
    [HarmonyPatch(typeof(UpcomingOrbDisplay), nameof(UpcomingOrbDisplay.Initialize))]
    internal static class UpcomingOrbDisplayPatch
    {
        private static readonly FieldInfo TextCanvasField = AccessTools.Field(typeof(UpcomingOrbDisplay), "_textCanvas");

        // The number sits just below the orb's centre, clear of the game's own badge at +0.41, at 80% of the game's text size.
        private const float LabelOffsetX = 0f;
        private const float LabelOffsetY = -0.45f;
        private const float LabelScale = 0.8f;

        private static bool _missingTemplateLogged;
        private static bool _canvasWarningLogged;
        private static int _failuresLogged;

        [HarmonyPostfix]
        private static void Postfix(UpcomingOrbDisplay __instance, GameObject orbPrefab)
        {
            // The whole body is guarded: a throw here would escape into DeckInfoManager while it builds the preview
            // and leave the plunger half-populated.
            try
            {
                if (!Plugin.ShowWeights.Value || orbPrefab == null)
                {
                    return;
                }
                AddLabel(__instance, orbPrefab);
            }
            catch (Exception e)
            {
                if (_failuresLogged < 3)
                {
                    _failuresLogged++;
                    Plugin.Log.LogWarning($"Couldn't add a weight number: {e.Message}");
                }
            }
        }

        private static void AddLabel(UpcomingOrbDisplay display, GameObject orbPrefab)
        {
            CanvasGroup source = TextCanvasField?.GetValue(display) as CanvasGroup;
            TMP_Text template = source == null ? null : source.GetComponentInChildren<TMP_Text>(true);
            if (template == null)
            {
                if (!_missingTemplateLogged)
                {
                    _missingTemplateLogged = true;
                    Plugin.Log.LogWarning("The upcoming-orb display has no text label to copy, so weight numbers are off.");
                }
                return;
            }

            OrbWeightLabelHolder holder = display.GetComponent<OrbWeightLabelHolder>();
            if (holder == null)
            {
                holder = display.gameObject.AddComponent<OrbWeightLabelHolder>();
            }
            if (holder.Label != null)
            {
                // Deactivate before the deferred Destroy so a re-initialise never shows two numbers for a frame.
                holder.Label.SetActive(false);
                UnityEngine.Object.Destroy(holder.Label);
                holder.Label = null;
            }

            int weight = Plugin.Weights.For(OrbKeys.Of(orbPrefab));

            // Parented to the orb widget rather than the text canvas: the canvas may be faded out (alpha 0) or
            // inactive when the orb isn't persisting, and a child would inherit that.
            GameObject label = UnityEngine.Object.Instantiate(template.gameObject, display.transform);
            label.name = "OrbOrderWeight";
            label.SetActive(true);

            CanvasGroup group = label.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = 1f;
            }

            TMP_Text text = label.GetComponent<TMP_Text>();
            if (text == null)
            {
                UnityEngine.Object.Destroy(label);
                return;
            }

            // Set after activating, in case the clone carries a localiser that writes on enable.
            text.text = weight.ToString(CultureInfo.InvariantCulture);
            text.color = ColorFor(weight);
            text.raycastTarget = false; // never steal clicks from the orb

            Transform labelTransform = label.transform;
            labelTransform.localPosition = new Vector3(LabelOffsetX, LabelOffsetY, -0.05f);
            labelTransform.localRotation = Quaternion.identity;
            labelTransform.localScale = Vector3.one * LabelScale;

            holder.Label = label;

            // A UI text outside every Canvas never draws. Worth one line in the log if that is the case.
            if (!_canvasWarningLogged && template is TextMeshProUGUI && label.GetComponentInParent<Canvas>() == null)
            {
                _canvasWarningLogged = true;
                Plugin.Log.LogWarning("Weight numbers have no Canvas ancestor, so they may not be visible. " +
                    "Set ShowWeights=false to turn them off and report this.");
            }
        }

        internal static Color ColorFor(int weight)
        {
            if (weight > OrbWeightTable.DefaultWeight)
            {
                return new Color(0.45f, 1f, 0.45f); // weighted above baseline: wants to be drawn earlier
            }
            if (weight == OrbWeightTable.DefaultWeight)
            {
                return Color.white; // baseline damage
            }
            if (weight > 0)
            {
                return new Color(1f, 0.72f, 0.25f); // below baseline
            }
            return new Color(0.7f, 0.7f, 0.7f); // never first
        }
    }

    /// <summary>Hides the number once its orb becomes the one in hand, so the active orb stays uncluttered.</summary>
    [HarmonyPatch(typeof(UpcomingOrbDisplay), nameof(UpcomingOrbDisplay.SetActive))]
    internal static class ActiveOrbLabelPatch
    {
        [HarmonyPostfix]
        private static void Postfix(UpcomingOrbDisplay __instance)
        {
            OrbWeightLabelHolder holder = __instance.GetComponent<OrbWeightLabelHolder>();
            if (holder != null && holder.Label != null)
            {
                holder.Label.SetActive(false);
            }
        }
    }

    /// <summary>Links a display widget to the weight number it owns.</summary>
    internal sealed class OrbWeightLabelHolder : MonoBehaviour
    {
        public GameObject Label;
    }
}
