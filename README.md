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
2. **Lucas's bridge gate (south-west rim, facing `mainToFarmBridge`)** awakens when goal 6 is complete. Its receiver orders a **Conservatory Bloom**
   (2 Water + 1 Moon Herb + 6 Gold); a crate crosses the gate and lands on the pedestal.
3. **Justin's bridge gate (south-east rim, mirroring Lucas's)** awakens after the first Bloom arrives. Its receiver orders an **Astral Core**
   (2 Crystal + 1 Essence + 6 Gold).
4. With both, the **Convergence Altar** (back wall) brews the Convergence Elixir (goal 7); 100 Gold of sales unlocks the
   **Final Ritual** at the same altar (goal 8).
5. The **Nexus Core** floats over the unreachable **ritual islet** at the centre of the three islands (0, 3.2, -24) — dormant (blue),
   aligned (gold), resonating (white). Once aligned, triggering the Core or the altar begins the Final Ritual.
6. The **Merchant Gate** sits on the east rim. Every refused or accepted press also shows a short world-space message on the
   line of sight, because in the headset the spellbook feedback line is easy to miss.

Controls are unchanged from MP2A (mouse/keyboard on desktop, wand ray + spellbook in VR). Gates, receivers, the altar and the
Nexus answer both click/trigger and right-click/grip, and their labels grow under the pointer.
Walls, furniture, trees, gates and pedestals are solid in VR: smooth walking slides along them and teleports cannot land inside them.
Comfort on Quest: the XRI jump is off, the left-hand spellbook follows the hand through a tremor filter, and the Android
quality level renders without real-time shadow maps (they shimmered with head motion and cost frames).

### Integration points for teammates (MP2C)

- `TeammateDeliveryReceiver.Receive(amount, "Lucas's island", optionalModel)` — deliver Blooms / Astral Cores from a teammate scene.
- `IslandLinkGate.Link()` — call once the teammate bridge exists; it drops the safety barrier. Lucas docks at the south-west gate
  (Keliang's `mainToFarmBridge`), Justin on the south-east pier.
- `ArcaneGameManager.RegisterTeammateOutput(kind, amount, source)` — shared-inventory API used by both of the above.

### Rebuild and validate

- **MP2B › Build Michael Island** re-authors the MP2B layer (gates, receivers, altar, Nexus, rim, underside, sky, lighting, colliders). Safe to re-run.
- **MP2B › Validate Michael Progression** runs 46 editor checks (progression, recipes, receivers, gates, scene contract).
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

The shared XR rig uses left-stick walking at 1.3 m/s, right-stick 30-degree snap turns, and the existing right-stick teleport gesture. Main-island, bridge and farm teleport areas reject edge and occupied destinations. Walking uses the original comfort vignette; snap turns and teleports trigger its blink.

Workshop controls: right trigger selects/gathers/builds; right grip uses stations and machines; B repairs or performs a station's alternate action; left X cancels placement. Y toggles the left-hand spellbook, including outside the main island. The book opens on arrival at the workshop, hides while the left hand handles a farm object, and falls back near the view if left-hand tracking is lost. The original building, recipe, resource, goal and ritual UI is reused.

Farm interaction has priority: a hovered or held XRI object reserves that hand's input; teleport aiming also suppresses workshop actions. Blocked presses are consumed and the right-hand buttons must be released before workshop use resumes. Farm near/far detection includes its visual layer. Safety barriers use Ignore Raycast while remaining solid to the player; station click volumes are triggers, and flattened platform colliders follow their visible meshes.

Checks are available under `MP2B > Keliang`: static integration validation and Play Mode checks for real near/far detection, input arbitration, spellbook/station actions, teleport movement, bridge/station collision, cultivation, trade and irrigation. Play checks discard their temporary gameplay state on exit. Headset comfort, controller reach and hardware input still require a device walkthrough.

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
