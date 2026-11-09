# PeglinOrbOrder

Your sack is weighted towards the orbs that set the battle up, not the ones that just hit things.

Peglin shuffles your whole deck once per battle and then draws from the top. PeglinOrbOrder re-sorts that stack with a weighted roll, so support orbs come up earlier more often than plain damage.

## Features

- **Support orbs are weighted higher.** Spinfection, Blind, slimes, peg conversion and shields tend to be drawn earlier than plain damage.
- **Pebballs and plain damage orbs are all equal.** They share the baseline weight of **50**, so the mod never favours one attack orb over another.
- **Late-game payoffs can sit below the baseline.** Memorb, Snowball, Bullyball and heals tend to arrive once they are worth it.
- **Every battle is a fresh roll.** The same deck can open differently from one fight to the next.
- **All 110 orbs carry a weight.** Change any of them in `BepInEx/config/PeglinOrbOrder.weights.json`.
- **Weight numbers on queued orbs.** Each orb waiting in the plunger shows its weight, colour-coded by band.
- **Only the battle sack is reordered.** Shop stock, post-battle orb rewards and event orbs come from the game's own seeded pools and stay uniform.

## How it works

The game builds the whole draw order at the start of a battle and whenever the sack is reloaded. PeglinOrbOrder takes that finished stack and re-sorts it with weighted random sampling: every orb gets one independent random roll, weighted by its number, and the heaviest rolls are drawn first. A heavy orb rolls high more often, never always. A weight of **0** means the orb is never first and always comes last; no shipped orb uses it, it is only there if you want to force an orb to the back.

Ordering happens after the game has already done everything else — Mirrorb copies, persistent-orb charges, attack setup and deck stats — so the mod only changes *when* an orb appears, never what it does.

The shuffle uses the game's own saved deck-order random state, so saving and reloading a run reproduces the same draw order.

## Weights

There are 110 orbs, weighted by what they do:

| Band | Weight | What it means |
|---|---|---|
| Opening support | 90-100 | Spinfection, Blind, "next orb hits everything", summon effects |
| Setup | 60-89 | Slimes, shields, peg conversion, self-buffs, crit seeds, refresh |
| Light support | 51-59 | Small nudges ahead of baseline: mild board and coin effects |
| Baseline | 50 | Pebball and every no-effect damage orb, plus neutral orbs |
| Below baseline | 35-49 | Scales with the battle, self-damage, heals, once-per-battle finishers |
| Never first | 0 | Reserved; nothing uses it by default |

Only the ratios matter. Raising a support orb from 50 to 80 makes it 1.6× as heavy, so against a single Pebball it takes about 62% of opening draws instead of 50%. The `exponent` setting sharpens or flattens every weight at once: **0** gives every weighted orb equal odds, **1** uses the numbers as written, **2** counts each point twice as much. Orbs set to 0 still come last.

## Orb weights file

`BepInEx/config/PeglinOrbOrder.weights.json` is created on first launch with all 110 orbs, and read every time the game starts — restart after editing it. Each entry is `"orb key": weight`:

```json
{
  "readme": "how the file works; ignored by the mod",
  "defaultWeight": 50,
  "exponent": 1.0,
  "orbs": {
    "instigatorb": 100,
    "poison_orb": 95,
    "stone": 50,
    "memorb": 40
  }
}
```

- **`orbs`** — the weight of each orb, 0-100. An orb the file leaves out keeps its shipped weight; `defaultWeight` only covers orb keys the mod doesn't know, such as one a game update adds. The mod names unknown keys in the log so typos are easy to spot.
- **`defaultWeight`** — weight for an orb key the mod doesn't know.
- **`exponent`** — 0-4. **1** uses the weights as written, **0** gives every weighted orb equal odds, higher values make the bias stronger. Orbs set to 0 still come last.
- **`readme`** — a description of the format; it is ignored by the mod.

If the file can't be read, the mod leaves it untouched, plays with the built-in weights and writes the reason to `BepInEx/LogOutput.log`.

Orb keys are the game's own (for example `stone` is the Pebball, `multiball` is Matryorbshka, `poison_orb` is Spinfectious Sting, `self_damage_boost` is Bullyball), so a weight applies at every orb level.

## Installation

**With a mod manager (recommended):**
1. Install r2modman or the Thunderstore Mod Manager.
2. Search for PeglinOrbOrder and install it. BepInEx is installed for you.
3. Launch Peglin.

**Manually:**
1. Install [BepInExPack_Peglin](https://thunderstore.io/c/peglin/p/BepInEx/BepInExPack_Peglin/).
2. Put `PeglinOrbOrder.dll` in `Peglin/BepInEx/plugins/`.
3. Start the game once to create the config file.

## Settings

Change these in `BepInEx/config/PeglinOrbOrder.cfg` (start the game once to create it). Restart the game after changing them.

| Setting | Default | What it does |
|---|---|---|
| `Enabled` | true | Reorder the sack. Turn off for the game's normal shuffle |
| `ShowWeights` | true | Print each queued orb's weight on it: green above baseline, white at 50, amber below, grey at 0 |
| `LogOrder` | false | Write the draw order to `BepInEx/LogOutput.log` on every shuffle |

## Compatibility

- Built for **Peglin v2.0.12**. On other versions the mod writes a warning to the log, as some features may not work.
- Requires [BepInExPack_Peglin](https://thunderstore.io/c/peglin/p/BepInEx/BepInExPack_Peglin/).

## Good to know

- The game shuffles once per battle, so weights shape the order of the whole sack rather than a fresh roll before every shot. With enough orbs in the deck the difference between adjacent weights is small; raise `exponent` to make the bias more pronounced.
- Relics that refresh the board work as they did before. A relic that reshuffles the deck gives a fresh weighted order rather than the game's own shuffle.
- Shop stock, post-battle orb rewards, peg minigame rewards, event orbs and Haglin's Satchel come from the game's own seeded orb pools, so their contents are unchanged.
- The weight numbers show the value from the file, before `exponent` is applied.
- Single-player only, and it changes draw order alone: no orb stats, no enemy stats, no rewards.

## Some of my other mods

- [PeglinBalatroScore](https://thunderstore.io/c/peglin/p/PierreMarais/PeglinBalatroScore/) — a Balatro-style pegs × damage score, with flames and a bomb chest.
- [PeglinFonts](https://thunderstore.io/c/peglin/p/PierreMarais/PeglinFonts/) — change the font of every text in the game.
- [PeglinKeybinds](https://thunderstore.io/c/peglin/p/PierreMarais/PeglinKeybinds/) — rebind keyboard and controller controls from inside the game.
- [PeglinMaxSpeed](https://thunderstore.io/c/peglin/p/PierreMarais/PeglinMaxSpeed/) — play at up to 15× speed without physics bugs.
- [PeglinQuickRestart](https://thunderstore.io/c/peglin/p/PierreMarais/PeglinQuickRestart/) — hold a button to restart your run.
- [PeglinRelicRerolls](https://thunderstore.io/c/peglin/p/PierreMarais/PeglinRelicRerolls/) — reroll the relics you're offered.

[Check out all my Peglin mods](https://old.thunderstore.io/c/peglin/p/PierreMarais/)

### [Please consider buying me a coffee ☕](https://ko-fi.com/pierremarais)
