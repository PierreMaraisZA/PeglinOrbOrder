# Changelog

## 1.0.0

- Weighted draw order: the sack is re-sorted after the game builds it, so support orbs are more likely to be drawn early.
- Only the battle sack is reordered. Shop stock, post-battle orb rewards, event orbs and other "random orb" picks come from the game's own seeded pools and stay uniform.
- Weights are rolled fresh each battle, so the same deck can open differently every time.
- Default weights for all 110 deck orbs, grouped by function: Spinfection and slimes, debuffs, self-buffs and next-orb setup, crit engines, refresh, multiball, board shaping, defense, gold, healing, self-damage finishers, and plain damage.
- Pebballs and every no-effect damage orb share one baseline weight, so the mod never favours one attack orb over another.
- Late-game payoffs can be weighted below the baseline; 0 means never first.
- Weights file at `BepInEx/config/PeglinOrbOrder.weights.json` with `defaultWeight` and `exponent` controls.
- Weight numbers on queued orbs, colour-coded by band.
- Shuffle uses the game's saved deck-order random state, so saving and reloading keeps the same order.
