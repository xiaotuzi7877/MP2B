# Arcane Workshop: Reawakening — MP2B

Shared Unity project for the CS417 MP2B/MP2C unfolding simulation.

The repository begins from the stable, committed MP2A Arcane Workshop build and preserves its interaction language, visual style, OpenXR setup, materials, shaders, audio, prefabs, and working gameplay scene. MP2B extends that foundation with staged unlocks and three interconnected teammate simulations.

## Team

- Michael (Ziyang Li) — central alchemy workshop, shared progression, integration, and Quest build
- Lucas (Keliang) — Verdant Conservatory farm: cultivation, magic crops, Conservatory Bloom
- Justin's part — Astral Foundry: star catching, rune power, forging the Astral Core (designed and implemented by Michael on Justin's behalf)

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

- Quest 3 OpenXR setup, wand pointer, spellbook, teleport, smooth locomotion, continuous turning, and comfort vignette
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
2. **Lucas's bridge gate (south-west rim, facing `mainToFarmBridge`)** awakens when goal 6 is complete. Its receiver orders a **Conservatory Bloom**
   (2 Water + 1 Moon Herb + 6 Gold); a crate crosses the gate and lands on the pedestal.
3. **Justin's bridge gate (south-east rim, mirroring Lucas's)** awakens after the first Bloom arrives; until then a solid seal
   blocks it. (Justin's part, built by Michael.) Behind it a stone bridge (`mainToFoundryBridge`) leads to **Justin's Astral Foundry**, mirrored from Lucas's farm
   at (28, 0, -36). The star-smith loop:
   - **Star Catcher** — 1 Crystal focuses it for 2 catches; each falling star it catches becomes a Star Fragment. An unfocused
     catcher lets the star crash into the island and drain 15 Rune Power.
   - **Rune Well** — 1 Essence pours in 50 Rune Power, which slowly leaks away; the floor channel to the forge lights up with it.
   - **Astral Forge** — 2 Star Fragments + 50 Rune Power + 1 Conservatory Bloom (Lucas's binder) forge an **Astral Core**, which
     floats back across the bridge into Justin's receiver. The receiver no longer sells cores; it points the player to the forge.
   Machines use the same click/trigger language as the gates and receivers; the next machine to use is marked `>>`.
4. With both, the **Convergence Altar** (back wall) brews the Convergence Elixir (goal 7); 100 Gold of sales unlocks the
   **Final Ritual** at the same altar (goal 8).
5. The **Nexus Core** floats over the unreachable **ritual islet** at the centre of the three islands (0, 3.2, -24) — dormant (blue),
   aligned (gold), resonating (white). Once aligned, triggering the Core or the altar begins the Final Ritual.
6. The **Merchant Gate** sits on the east rim. Every refused or accepted press also shows a short world-space message on the
   line of sight, because in the headset the spellbook feedback line is easy to miss.

Controls are unchanged from MP2A (mouse/keyboard on desktop, wand ray + spellbook in VR). In Play Mode without a headset an
**inspect view** replaces the fixed rig eye: middle drag or Alt + left drag looks around, wheel zooms, WASD/QE move, Tab cycles
workshop / Lucas's farm / Justin's foundry / ritual islet, V switches to the player eye. Gates, receivers, the altar and the
Nexus answer both click/trigger and right-click/grip, and their labels grow under the pointer.
Walls, furniture, trees, gates and pedestals are solid in VR: smooth walking slides along them and teleports cannot land inside them.
Comfort on Quest: the XRI jump is off, the left-hand spellbook follows the hand through a tremor filter, and the Android
quality level renders without real-time shadow maps (they shimmered with head motion and cost frames).

### Integration points for teammates (MP2C)

- `TeammateDeliveryReceiver.Receive(amount, "Lucas's island", optionalModel)` — deliver Blooms / Astral Cores from a teammate scene.
- `IslandLinkGate.Link()` — call once the teammate bridge exists; it drops the safety barrier. Lucas docks at the south-west gate
  (Keliang's `mainToFarmBridge`), Justin at the south-east gate (`mainToFoundryBridge`, docked with `DockBridge`).
- `ArcaneGameManager.RegisterTeammateOutput(kind, amount, source)` — shared-inventory API used by both of the above.

### Rebuild and validate

- **MP2B › Build Michael Island** re-authors the MP2B layer (gates, receivers, altar, Nexus, rim, underside, sky, lighting, colliders). Safe to re-run.
- **MP2B › Build Justin Foundry Island** re-authors Justin's island, bridge and gate seal (also run at the end of Build Michael Island).
- **MP2B › Validate Michael Progression** runs 139 editor checks (progression, recipes, receivers, gates, foundry, scene contract).
- Progress and hand-off notes: [`Documentation/CLAUDE_WORK_LOG.md`](Documentation/CLAUDE_WORK_LOG.md).

## Repository boundaries

The migration intentionally excludes MP2A `Library/`, `Logs/`, `Builds/`, `UserSettings/`, videos, contribution summaries, personal assistant files, and the old repository history. The baseline was taken from the committed MP2A revision, not from uncommitted local package changes.

See `Documentation/MP2B_SETUP.md` for ownership, extension points, and the completed setup milestones.

## Keliang farm migration

`Assets/Scenes/ArcaneWorkshop.unity` now contains the complete MP2A farm at `keliangFarmIsland` (position `-28, 0, -36`), the `mainToFarmBridge`, and one shared `playerRig` imported from the farm's XRI setup. Keliang owns the farm/conservatory gameplay; the older reference-station labels have not yet been reassigned.

- Nine plots, tools, crop growth/health, fertilizer, trading, mining, sprinklers, upgrade kits and interaction prompts are retained; the old fixed farm HUD is disabled in favor of the shared spellbook. The empty 80 m background ground was trimmed to a 32 m island; gameplay objects remain at their original scale. The entrance boundary has a bridge opening.
- Farm scripts and integration/editor tools are in `Assets/Scripts/Keliang`. External models, sounds, XRI Starter Assets and TMP resources are under `Assets/ThirdParty/Keliang`, with original GUIDs and available licenses. Farm models come from LowPolyFarmLite, Gridness Studios Lite Farm Pack and Kenney Nature Kit; used effects come from Leohpaz RPG Essentials Free.
- Farm materials target the existing Built-in renderer. Soil/crop tint and flowing-water texture properties were adapted. `farmSoil` uses layer 8; `farmVisual` uses layer 9. Farm-only sunlight avoids illuminating the main workshop with a second sun.
- XRI owns movement, turning and grabbing across both islands. `FarmWorkshopRig` forwards right trigger/grip/B and left X to the workshop near the main island and uses the shared left-hand spellbook; Y can also open it outside the main island. The old workshop rig is suppressed, so its origin-centered movement clamp cannot pull the player off the bridge. The main island uses a mesh collider matching its visible ground instead of the oversized cylinder capsule collider.
- The original `mp2aunity` project is unchanged. Moon Herb harvests and farm crop trades use the existing shared MoonHerb balance. The separate purple Bloom seed bag grows Conservatory Bloom; its first harvest grants exactly two shared Blooms. Existing spellbook resource text reflects both harvests.

The shared XR rig uses left-stick walking at 1.3 m/s, right-stick linear continuous turning (up to 60 degrees/second, proportional to horizontal stick input), and the existing right-stick teleport gesture. Snap turning is disabled. Main-island, bridge and farm teleport areas reject edge and occupied destinations. Walking uses the original comfort vignette; teleports trigger its blink. The scene starts the player on the main island at (0, 0, -7.5), facing its center.

Workshop controls: right trigger selects/gathers/builds; right grip uses stations and machines; B repairs or performs a station's alternate action; left X cancels placement. Y toggles the left-hand spellbook, including outside the main island. The book opens on arrival at the workshop, hides while the left hand handles a farm object, and falls back near the view if left-hand tracking is lost. The original building, recipe, resource, goal and ritual UI is reused.

Farm interaction has priority: a hovered or held XRI object reserves that hand's input; teleport aiming also suppresses workshop actions. Blocked presses are consumed and the right-hand buttons must be released before workshop use resumes. Farm near/far detection includes its visual layer. Safety barriers use Ignore Raycast while remaining solid to the player; station click volumes are triggers, and flattened platform colliders follow their visible meshes.

Checks are available under `MP2B > Keliang`: static integration validation and Play Mode checks for real near/far detection, input arbitration, spellbook/station actions, teleport movement, bridge/station collision, cultivation, trade and irrigation. Play checks discard their temporary gameplay state on exit. Headset comfort, controller reach and hardware input still require a device walkthrough.

### Farm quick-start for teammates

Cross `mainToFarmBridge` to Lucas/Keliang's farm. Use GRIP to grab tools, select board buttons, and harvest mature plants; release GRIP to drop a held tool. With the watering can held, hold TRIGGER with its spout above a plot to water it. Left Y opens the shared spellbook.

**Two seed bags:** the teal/green bag with silver details and the label **MOON** plants Moon Herb. The purple bag with gold details and the label **BLOOM** plants Conservatory Bloom. The Moon bag appears after the first tilling; the Bloom bag appears after your first personal Moon Herb harvest. Hold a bag and touch an empty tilled plot to plant. Both use the same seed stock (1 seed per planting). Moon Herbs can be grown repeatedly. In the current session-based implementation, Bloom can be harvested once and gives exactly **2 shared Blooms**; repeat Bloom planting is blocked after that harvest.

**The two shop boards (identify them by their titles if viewing from the opposite side):**

- **Left / FARM MARKET:** shows Moon Herbs, seeds, farm coins, fertilizer and stones. Point at a trade button and press GRIP. Sell 1 Moon Herb for 3 farm coins, buy 1 seed for 1 coin, or buy fertilizer for 2 coins. Farm coins are separate from the main workshop's Gold. Selling herbs also reduces the shared Moon Herb balance in the spellbook.
- **Right / WORKSHOP:** buys sprinkler kits and pickaxe, tank and soil upgrades using stones and farm coins. Buying adds a kit to inventory; it does not automatically install it. Grab the corresponding carrier and touch the correct target: an empty tilled plot for a sprinkler, the pickaxe for its upgrade, the sprinkler for a tank upgrade, or a tilled plot for a soil upgrade. Locked or unaffordable purchases do not deduct resources.
- **Tutorial / VERDANT CONSERVATORY — NEXT STEP:** shows the current goal, relevant controls and recovery instructions after errors. It updates as you progress; it is a guide, not another shop. When an action fails, read the short error and then this board for what to do next.

**Recommended play order:**

1. Grab the harrow and touch an untilled plot. This unlocks the teal MOON seed bag.
2. Plant Moon Herb, then water it. Mining unlocks: grab the pickaxe and strike rocks for stones while the plant grows.
3. Keep the soil moist; point at the mature herb and press GRIP to harvest. This unlocks the purple BLOOM seed bag. Sell extra herbs at FARM MARKET if you need coins.
4. Plant and water Bloom on an empty tilled plot, then harvest it for 2 Blooms. This unlocks the sprinkler. Buy a sprinkler kit (5 stones + 3 farm coins), grab its carrier and touch an empty tilled plot to install it.
5. Fill the sprinkler with the watering can and till a neighboring plot without a sprinkler. Let the sprinkler actually water that neighbor; filling its tank alone does not unlock the next stage.
6. Buy the now-unlocked tank upgrade (4 stones + 4 farm coins). Grab its carrier and touch the sprinkler to increase capacity from 100 to 200.
7. Open the spellbook with Y to see the shared Moon Herbs and Blooms. Walk to the foundry: 1 Bloom is consumed when forging the Astral Core. Keep the other Bloom for the main island's Convergence Altar. Harvests are already in shared inventory; no physical crop delivery platform is needed.

### Farm lessons and unlocks

Five discoveries guide the farm: first tilling unlocks the Moon Herb seed bag; planting and then watering Moon Herb unlocks mining; the first personal Moon Herb harvest unlocks Bloom; the first personal Bloom harvest unlocks sprinkler purchase and placement; successful sprinkler irrigation of a neighboring tilled plot unlocks the tank upgrade. Filling the sprinkler alone does not count. Each unlock introduces the mechanic before revealing its tool. A world-space farm board shows the next step and persistent recovery instructions after errors. Existing stock does not count as a personal harvest milestone.

Locked sprinkler, pickaxe-upgrade and tank-upgrade purchase buttons remain clickable to explain their requirements and the current next step. Both the UI action and inventory purchase API reject locked or unaffordable purchases before any deduction; stocked upgrade kits cannot bypass their unlocks. Missing-material lessons explain mining/trading and update when the player has enough. Seed/fertilizer contact attempts are handled once per visit to a plot, and immature crops accept the original grip interaction to explain why harvesting cannot succeed yet. These lesson flags currently live for the running session; no save system was introduced.

All related scripts are under `Assets/Scripts/Keliang`. Use `MP2B > Keliang > Check Farm Lessons and Spending` for the integrated five-stage progression and transaction checks. The earlier crop checks route to this sequential flow when farm progression is present. The original migration check suite predates these unlock gates. Quest headset validation remains outstanding.

The merge with Michael's latest island preserves its gates, receivers, altar, Nexus, environment and removal of the old prototype teammate districts, alongside the full farm, bridge and shared XR prefab. The scene's local object references resolve, and only one camera is active. Merge validation compiled successfully: Michael's 46 checks and all 25 farm assertions passed. The batch farm report still recorded a Unity Editor Search indexing exception (`SearchDatabase.EnumerateAll`), so it was not an entirely clean automated run. No headset validation was performed.

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
