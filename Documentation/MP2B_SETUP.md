# MP2B Setup and Migration Notes

## Purpose of this baseline

This first setup milestone creates a playable shared foundation without breaking the working MP2A references. It gives the team one consistent interface metaphor, control scheme, visual language, audio language, Unity version, and Quest configuration before new member systems are added.

The imported scene remains `Assets/Scenes/ArcaneWorkshop.unity`. Renaming or reorganizing the existing assets is deliberately postponed because Unity scene and prefab references depend on their `.meta` GUIDs and because the assignment deadline favors a stable build over a large folder refactor.

## What was migrated

### Interaction and XR foundation

- `VRRig`, wand ray, reticle, left-hand spellbook, controller tracking fallback
- teleportation, smooth locomotion, snap turning, and comfort vignette
- desktop input and orbit-camera fallback for rapid Editor testing
- Android and Standalone OpenXR settings for Quest APK and Meta Horizon Link

### Simulation foundation

- shared Resource inventory and three Potion recipes
- grid cells, placement markers, Foundation requirement, building costs
- passive Stat simulation and neighbor transfers
- collision projectiles, wild spawning, hazards, repair, death, merchant delivery, and goal tracking

### Reusable presentation

- world-space labels, tutorial bubbles, spellbook panel, buttons, and feedback messages
- shared moon-magic palette: blue Mana, purple Crystals, green Herbs, gold trade feedback, red invalid/damage feedback
- scrolling mana-flow shader, radial amplifier effect, distorted brew shader, rune textures, materials, meshes, and audio cues
- KayKit workshop props and Kenney nature dressing with license files

### Content kept as Ziyang's starting feature set

- Rune Foundation
- Moon Planter
- Mana Still
- Crystal Amplifier
- Star Cauldron
- Merchant Gate
- Moon Herb, Crystal Cluster, and Crystal Shard

## What was intentionally excluded

- Unity-generated `Library/`, `Temp/`, `Logs/`, `Builds/`, and `UserSettings/`
- APKs and recorded videos
- MP2A submission summaries, rubric drafts, and personal documentation
- `.DS_Store`, local assistant bridges, and old Git metadata
- uncommitted changes in the MP2A working copy

## Shared extension points

Until the core is made data-driven, teammates should integrate through small, reviewed additions rather than expanding the existing manager with large feature-specific switch statements.

### Ziyang — shared progression and alchemy

- convert the seven goals into real unlock gates
- expose shared Resource add/spend methods for teammate systems
- connect advanced recipes to Justin and Lucas outputs
- maintain the main scene, global UI, final ritual, and Quest build

### Justin — conservatory feature module

- create prefabs and scripts in a dedicated Conservatory folder
- implement plant Growth, Moisture, Fertility, Pollination, and Familiar interactions
- consume Water/Mana and produce advanced crop ingredients
- demonstrate the feature first in a separate test scene

### Lucas — foundry feature module

- create prefabs and scripts in a dedicated Astral Foundry folder
- implement Star Fragment collection, Rune Paths, transport, forging, and shields
- consume Mana and organic binding material; produce Astral components
- demonstrate the feature first in a separate test scene

## Visual consistency rules

- Use the existing low-poly KayKit/Kenney proportions and material simplicity.
- Reuse `Mana.mat`, `Crystal.mat`, `Rune.mat`, `MoonHerb.mat`, and `DarkMetal.mat` before creating near-duplicate materials.
- Use blue for available Mana/valid placement, purple for advanced arcane power, green for healthy biological growth, gold for trade/progression, and red for invalid actions or damage.
- Reuse `WorldText.mat` and `UIPanel.mat` so all tutorials and Stats remain readable in VR.
- Use animated transfer effects when one teammate's system changes another teammate's Stats.

## Git workflow

1. Pull `main` before starting a feature.
2. Create one feature branch per member.
3. Keep feature work inside owned folders and test scenes.
4. Submit a Pull Request with a short test checklist and screenshots/video evidence.
5. Assign a teammate reviewer and record approval for the collaboration rubric.
6. Let the integration owner update the main scene after prefab review.

## Next setup milestones

1. Open the migrated project in Unity and allow a clean import.
2. Confirm the existing scene enters Play Mode without compile errors.
3. Run `MP2B > Configure Quest Settings`.
4. Test the existing critical path in Editor.
5. Create and assign the three feature branches.
6. Implement a shared unlock/progression layer before adding large amounts of new content.
7. Run a Quest APK smoke test before merging teammate gameplay.
