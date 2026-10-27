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

## Implemented MP2B team expansion

- Justin's **Arcane Conservatory** turns Water and Essence into plant Growth, Moisture, Fertility, Pollination, and Familiar Bond; a completed cycle produces two `ConservatoryBloom` resources.
- Lucas's **Astral Foundry** captures Star Fragments, charges Rune Power, consumes one Bloom as an organic binder, forges an `AstralCore`, and can intercept Falling Stars with its Shield.
- Michael's **Convergence Nexus** connects both teammate loops to Convergence Alchemy and exposes the player-triggered Final Ritual after the shared campaign is ready.
- The main scene now includes three visibly distinct districts, a large MP2B world title, and rune paths showing the cross-team resource flow.
- The existing mouse, controller-ray, resource, unlock, spellbook, feedback, and Quest/OpenXR systems remain the shared interaction foundation.

See [`Documentation/MP2B_TEAM_PREVIEW.png`](Documentation/MP2B_TEAM_PREVIEW.png) for the authored scene overview and [`Documentation/TEAM_COMPLETION_LOG.md`](Documentation/TEAM_COMPLETION_LOG.md) for benchmark and validation evidence.

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

First tilling unlocks the Moon Herb seed bag; the first personal Moon Herb harvest unlocks Bloom; the first Bloom harvest unlocks sprinkler purchase and placement. Each unlock introduces the mechanic before revealing its tool. A world-space farm board shows the current next step and persistent recovery instructions after errors. Existing stock does not count as a personal harvest milestone.

The locked sprinkler purchase button remains clickable to explain the requirement. Both its UI action and inventory purchase API reject locked or unaffordable purchases before any deduction. Missing-material lessons explain mining/trading and update when the player has enough. Seed/fertilizer contact attempts are handled once per visit to a plot, and immature crops accept the original grip interaction to explain why harvesting cannot succeed yet. These lesson flags currently live for the running session; no save system was introduced.

All related scripts are under `Assets/Scripts/Keliang`. Use `MP2B > Keliang > Check Farm Lessons and Spending` for the current integrated progression and transaction checks (25 checks passed). The earlier crop checks route to this sequential flow when farm progression is present. The original migration check suite predates these unlock gates. Quest headset validation remains outstanding.

## Collaboration rules

- `main` must stay buildable.
- Develop on `feature/michael-progression`, `feature/justin-conservatory`, or `feature/lucas-foundry`.
- Use Pull Requests and request one teammate review before merging.
- Build features in separate test scenes and prefabs; avoid simultaneous edits to `ArcaneWorkshop.unity`.
- Keep `.meta` files with their Unity assets.
- Do not commit generated folders or APKs.

## Third-party assets

The project contains a curated CC0 subset of KayKit Dungeon Remastered, Kenney Nature Kit, and Kenney audio packs. License copies are stored under `Assets/ThirdParty/Licenses/`.
