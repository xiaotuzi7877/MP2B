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

## Collaboration rules

- `main` must stay buildable.
- Develop on `feature/michael-progression`, `feature/justin-conservatory`, or `feature/lucas-foundry`.
- Use Pull Requests and request one teammate review before merging.
- Build features in separate test scenes and prefabs; avoid simultaneous edits to `ArcaneWorkshop.unity`.
- Keep `.meta` files with their Unity assets.
- Do not commit generated folders or APKs.

## Third-party assets

The project contains a curated CC0 subset of KayKit Dungeon Remastered, Kenney Nature Kit, and Kenney audio packs. License copies are stored under `Assets/ThirdParty/Licenses/`.
