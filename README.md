# Arcane Workshop: Reawakening — MP2B

Shared Unity project for the CS417 MP2B/MP2C unfolding simulation.

The repository begins from the stable, committed MP2A Arcane Workshop build and preserves its interaction language, visual style, OpenXR setup, materials, shaders, audio, prefabs, and working gameplay scene. MP2B extends that foundation with staged unlocks and three interconnected teammate simulations.

## Team

- Michael (Ziyang Li) — central alchemy workshop, shared progression, integration, and Quest build
- Justin — arcane conservatory, cultivation, and familiar ecology
- Lucas — astral foundry, rune logistics, and workshop defense

## Required Unity version

Unity `6000.5.6f1` with Android Build Support, OpenXR, SDK, NDK, and OpenJDK.

## Open the project

1. Clone the repository with Git LFS installed.
2. Open the repository root in Unity Hub with Unity `6000.5.6f1`.
3. Open `Assets/Scenes/ArcaneWorkshop.unity` if Unity does not open it automatically.
4. Use `MP2B > Configure Quest Settings` before the first Quest build.
5. Use `MP2B > Build Quest APK` to create `Builds/ArcaneWorkshopReawakening.apk`.

The Android package ID is `com.cs417.arcaneworkshop.mp2b`, so MP2B can be installed without replacing the MP2A submission.

## Migrated MP2A foundation

- Quest 3 OpenXR setup, wand pointer, spellbook, teleport, smooth locomotion, snap turn, and comfort vignette
- 7×7 construction grid, placement feedback, Rune Foundations, and building costs
- Moon Planter, Mana Still, Crystal Amplifier, Star Cauldron, and Merchant Gate
- shared player resources, recipes, passive Stats, neighbor transfers, and collision interactions
- wild Moon Herbs and Crystal Clusters, intermittent spawning, Falling Stars, Integrity, repair, and destruction
- tutorial bubbles, world labels, audio feedback, seven restoration goals, and the ending celebration
- blue/purple moon-magic materials, animated rune/flow textures, distorted brew shader, third-party environment models, and licensed audio

## Michael Island (MP2B)

![Michael Island](Documentation/MICHAEL_ISLAND_PREVIEW.png)

Michael's island keeps the full MP2A Arcane Workshop (round floating island, 7×7 rune grid, five machines, wild materials,
Merchant Gate, Falling Stars, Integrity/repair, seven restoration goals) and unfolds into the three-island MP2B campaign:

1. **Goals 1–6 (MP2A loop)** teach foundations, planting, mana links, brewing, selling, and the full production chain.
2. **Lucas's bridge gate (west rim)** awakens when goal 6 is complete. Its receiver orders a **Conservatory Bloom**
   (2 Water + 1 Moon Herb + 6 Gold); a crate crosses the gate and lands on the pedestal.
3. **Justin's bridge gate (east rim)** awakens after the first Bloom arrives. Its receiver orders an **Astral Core**
   (2 Crystal + 1 Essence + 6 Gold).
4. With both, the **Convergence Altar** (back wall) brews the Convergence Elixir (goal 7); 100 Gold of sales unlocks the
   **Final Ritual** at the same altar (goal 8).
5. The **Nexus Core** floats in the void above the workshop — dormant (blue), aligned (gold), resonating (white) after the ritual.

Controls are unchanged from MP2A (mouse/keyboard on desktop, wand ray + spellbook in VR). Gates, receivers, the altar and the
Nexus answer both click/trigger and right-click/grip, and their labels grow under the pointer.
Walls, furniture, trees, gates and pedestals are solid in VR: smooth walking slides along them and teleports cannot land inside them.

### Integration points for teammates (MP2C)

- `TeammateDeliveryReceiver.Receive(amount, "Lucas's island", optionalModel)` — deliver Blooms / Astral Cores from a teammate scene.
- `IslandLinkGate.Link()` — call once the teammate bridge exists; it drops the safety barrier. Lucas docks on the west pier
  (matching `mainToFarmBridge`), Justin on the east pier.
- `ArcaneGameManager.RegisterTeammateOutput(kind, amount, source)` — shared-inventory API used by both of the above.

### Rebuild and validate

- **MP2B › Build Michael Island** re-authors the MP2B layer (gates, receivers, altar, Nexus, rim, underside, sky, lighting, colliders). Safe to re-run.
- **MP2B › Validate Michael Progression** runs 46 editor checks (progression, recipes, receivers, gates, scene contract).
- Progress and hand-off notes: [`Documentation/CLAUDE_WORK_LOG.md`](Documentation/CLAUDE_WORK_LOG.md).

## Repository boundaries

The migration intentionally excludes MP2A `Library/`, `Logs/`, `Builds/`, `UserSettings/`, videos, contribution summaries, personal assistant files, and the old repository history. The baseline was taken from the committed MP2A revision, not from uncommitted local package changes.

See `Documentation/MP2B_SETUP.md` for ownership, extension points, and the completed setup milestones.

## Collaboration rules

- `main` must stay buildable.
- Develop on a personal branch (Michael: `michael/island-final`) and merge into `main` when it builds and validates.
- Use Pull Requests and request one teammate review before merging.
- Build features in separate test scenes and prefabs; avoid simultaneous edits to `ArcaneWorkshop.unity`.
- Keep `.meta` files with their Unity assets.
- Do not commit generated folders or APKs.

## Third-party assets

The project contains curated CC0 subsets of KayKit Dungeon Remastered, Kenney Nature Kit, Kenney Fantasy Town Kit,
Kenney Castle Kit, Kenney audio packs, and ambientCG textures (PavingStones070, Rock030). License copies and source notes are
stored under `Assets/ThirdParty/` (`Licenses/` and each pack's `SOURCE.md`). The night sky, ambience, and crystal/torus meshes
are generated by project code.
