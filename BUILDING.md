# Building PeglinOrbOrder

## Prerequisites

- .NET 8 SDK (`dotnet --version`).
- Peglin installed, with BepInEx 5 (the game's `BepInEx\core` folder must exist).
- The default game path is `C:\Program Files (x86)\Steam\steamapps\common\Peglin`. Pass another path with
  `-p:GamePath="D:\Games\Peglin"`.

## Commands

```powershell
dotnet test tests\PeglinOrbOrder.Tests.csproj
dotnet build PeglinOrbOrder.csproj -c Release   # player build, copies to BepInEx\plugins\PeglinOrbOrder
powershell -ExecutionPolicy Bypass -File pack.ps1   # tests, Release build, dist\PierreMarais-PeglinOrbOrder-<version>.zip
```

The Release build copies the DLL into the game, so building is enough to try a change in-game.

## Layout

- `src/Core/` — Unity-free rules, linked directly into the test project.
  - `DefaultWeights.cs` — the shipped weight for every orb.
  - `OrbWeightTable.cs` — lookups, clamping, unknown-orb fallback.
  - `WeightedOrder.cs` — the weighted sampling algorithm.
  - `OrbWeightsJson.cs` — the weights file format.
- `src/Plugin.cs` — BepInEx entry point, config, weights loading.
- `src/DeckShufflePatch.cs` — Harmony postfix that re-sorts the draw stack.
- `src/OrbKeys.cs` — maps a game orb to the key used in the weights file.
- `src/OrbWeightLabels.cs` — the number on each queued orb.

## How the draw works

`DeckManager.ShuffleCompleteDeck` Fisher-Yates shuffles the deck, pushes it onto `shuffledDeck` (top = next draw)
and fires `onDeckShuffled`. The mod postfixes that method, reads the stack, re-sorts it with weighted sampling and
pushes it back. Everything else the method did (Mirrorb copies, persistence resets, attack initialisation, the UI
event) is left untouched.

`DeckManager` is a `ScriptableObject` asset, so it is never found with `FindObjectOfType`; the mod only uses the
instance Harmony hands to the postfix, and the labels read from the `Attack` components on the orb prefabs.

`ShuffleCompleteDeck` is not only the battle shuffle: the game also calls it to pick a "random" orb for story and reward
effects. The postfix therefore requires `BattleController.BattleActive`; without that guard, event and reward picks would
silently follow the weights. Shop stock and post-battle orb rewards never touch this method at all — they come from
`SeededShopNodeData` / `SeededBattleNodeData`, which seed prefab lists with `UnityEngine.Random` when the map node is
created — so they stay uniform.
