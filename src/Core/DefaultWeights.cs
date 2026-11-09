using System.Collections.Generic;

namespace PeglinOrbOrder.Core
{
    /// <summary>
    /// The shipped weights for all 110 deck orbs, grouped by what the orb does.
    ///
    /// The scale:
    ///   100      wants the very first draw, everything after it benefits
    ///   60-95    setup: debuffs, slimes, board conversion, lasting self-buffs
    ///   50       neutral: Pebball and every no-effect damage orb
    ///   35-45    payoffs that want to be drawn late, once the battle has built something up
    ///   0        never first, always drawn last
    ///
    /// These are defaults only. BepInEx/config/PeglinOrbOrder.weights.json overrides them per orb.
    /// </summary>
    public static class DefaultWeights
    {
        public static readonly IReadOnlyDictionary<string, int> All = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase)
        {
            // Spinfection, poison and slime.
            { "poison_orb", 95 },
            { "cauldron_bubball", 95 },
            { "toil_and_trouball", 90 },
            { "balleviation", 85 },
            { "generatorb", 80 },
            { "splatorb", 80 },
            { "twooze_company", 80 },
            { "chemiball_hazard", 70 },
            { "potion_belt", 70 },
            { "ballm", 65 },
            { "douball_douball", 45 },

            // Debuffs on enemies.
            { "blind", 90 },
            { "transpherency_orb", 90 },
            { "blind_ice", 85 },
            { "debuff", 80 },
            { "bramble", 80 },
            { "susceptiball", 75 },
            { "roundabout", 75 },
            { "exacorbate", 60 },
            { "ballestra", 40 },

            // Self-buffs and next-orb setup.
            { "instigatorb", 100 },
            { "summoning_circle", 90 },
            { "activatorb", 85 },
            { "orbust", 80 },
            { "ballad", 75 },
            { "arccentuate", 70 },
            { "rubber_applier", 70 },
            { "twinorb", 70 },
            { "best_foot_forworb", 65 },
            { "renewed_vigorb", 60 },
            { "potentiorb", 45 },
            { "nimball", 40 },

            // Crit engines.
            { "critiball", 80 },
            { "critorb", 75 },
            { "cleavorb", 50 },
            { "dagger", 50 },
            { "evis", 50 },
            { "all_orb_nothing", 50 },
            { "extra_orbinary", 45 },

            // Refresh and shuffle.
            { "refreshorb", 70 },
            { "refreshiball", 65 },
            { "shuffle", 65 },
            { "double_edged_sworb", 60 },
            { "refresh_on_exit", 60 },

            // Multiball and summons.
            { "circcae", 75 },
            { "double_trouball", 70 },
            { "multiball", 65 },

            // Board shaping: pegs, bombs, shields.
            { "forbification", 85 },
            { "bomborb", 80 },
            { "buff", 80 },
            { "protectorb", 80 },
            { "bucklorb", 75 },
            { "chopping_ballock", 75 },
            { "collatorball_damage", 75 },
            { "ultra_buff_orb", 70 },
            { "duplicatorb", 70 },
            { "magnet", 70 },
            { "jupitorb", 65 },
            { "fireball", 65 },
            { "spin_doctorb", 65 },
            { "morbid", 55 },
            { "destroy_peg_orb", 55 },
            { "necorb", 55 },
            { "special_pegs_orb", 50 },

            // Defense and evasion.
            { "evasive_maneuvorb", 80 },
            { "ellucircle", 75 },
            { "best_orbfence", 45 },
            { "irresponsiball", 40 },

            // Gold.
            { "creates_coins", 70 },
            { "ballectrum", 70 },
            { "coins_contribute_damage", 60 },
            { "coin_doubling_orb", 55 },
            { "spinterest", 55 },
            { "oreb", 50 },

            // Healing and lifesteal.
            { "pupil_orb", 70 },
            { "egg", 50 },
            { "heal_orb", 45 },
            { "heal_orb_basic", 45 },
            { "vampire_orb", 45 },

            // Self-damage and scaling finishers.
            { "orbrasion", 55 },
            { "orberos", 55 },
            { "offering", 55 },
            { "snowball", 45 },
            { "bomb_buff", 45 },
            { "spinpoint", 45 },
            { "memorb", 40 },
            { "self_damage_basic", 40 },
            { "countorb_attack", 40 },
            { "angry_tomahawk", 40 },
            { "self_damage_boost", 35 },

            // Baseline damage and neutral.
            { "stone", 50 },
            { "sworb", 50 },
            { "warhammer", 50 },
            { "boulder", 50 },
            { "icicle", 50 },
            { "lightning", 50 },
            { "ballanx", 50 },
            { "lazorb", 50 },
            { "sphear", 50 },
            { "rubber", 50 },
            { "horriball", 50 },
            { "terriball", 50 },
            { "portal", 50 },
            { "mirror", 50 },
            { "assemball", 50 },
            { "dense_orb", 50 },
            { "echo_orb", 50 },
            { "ghost", 50 },
            { "orbelisk", 50 },
            { "orbbery", 50 },
        };
    }
}
