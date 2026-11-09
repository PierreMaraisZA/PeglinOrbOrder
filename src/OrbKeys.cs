using Battle.Attacks;
using UnityEngine;

namespace PeglinOrbOrder
{
    /// <summary>
    /// Reads an orb's stable identity. The game's own key (Attack.locNameString) is what the weights file uses,
    /// so "stone" is the Pebball at any level and regardless of the display name's localisation.
    /// </summary>
    internal static class OrbKeys
    {
        public static string Of(GameObject orb)
        {
            if (orb == null)
            {
                return null;
            }
            Attack attack = orb.GetComponent<Attack>();
            return attack == null ? null : attack.locNameString;
        }

        public static string DisplayName(GameObject orb)
        {
            if (orb == null)
            {
                return "null";
            }
            Attack attack = orb.GetComponent<Attack>();
            if (attack == null)
            {
                return orb.name;
            }
            string name = attack.Name;
            return string.IsNullOrEmpty(name) ? attack.locNameString : name;
        }
    }
}
