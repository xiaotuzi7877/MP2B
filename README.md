# Arcane Workshop: Reawakening — MP2B

Shared Unity project for the CS417 MP2B/MP2C unfolding simulation.

The repository begins from the stable, committed MP2A Arcane Workshop build and preserves its interaction language, visual style, OpenXR setup, materials, shaders, audio, prefabs, and working gameplay scene. MP2B will extend that foundation with staged unlocks and interconnected teammate simulations.

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

## Repository boundaries

The migration intentionally excludes MP2A `Library/`, `Logs/`, `Builds/`, `UserSettings/`, videos, contribution summaries, personal assistant files, and the old repository history. The baseline was taken from the committed MP2A revision, not from uncommitted local package changes.

See `Documentation/MP2B_SETUP.md` for ownership, extension points, and the next setup milestones.

## Collaboration rules

- `main` must stay buildable.
- Develop on `feature/michael-progression`, `feature/justin-conservatory`, or `feature/lucas-foundry`.
- Use Pull Requests and request one teammate review before merging.
- Build features in separate test scenes and prefabs; avoid simultaneous edits to `ArcaneWorkshop.unity`.
- Keep `.meta` files with their Unity assets.
- Do not commit generated folders or APKs.

## Third-party assets

The project contains a curated CC0 subset of KayKit Dungeon Remastered, Kenney Nature Kit, and Kenney audio packs. License copies are stored under `Assets/ThirdParty/Licenses/`.
