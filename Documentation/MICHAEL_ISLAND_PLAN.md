# Michael Island Submission Plan

Reference direction: the three-island concept supplied on 2026-10-26. This plan intentionally limits implementation to Michael's blue-and-gold Convergence Atelier island. Lucas and Justin islands remain external dependencies represented by two reserved connection gates.

## Submission definition

Michael Island is submission-ready when it:

- reads as a polished triangular floating alchemy workshop rather than the previous circular prototype;
- preserves the complete build, gather, brew, sell, progression, repair, hazard, and Final Ritual loops;
- provides two clearly labeled, inactive bridge gates for future Lucas and Justin island connections;
- can receive `ConservatoryBloom` and `AstralCore` through dedicated delivery receivers without depending on either teammate scene;
- keeps all gameplay surfaces safe for desktop and VR locomotion;
- presents readable, correctly oriented world text with no mirrored or negatively scaled text parents;
- compiles, passes automated validation, survives a full desktop critical-path rehearsal, and has a documented Quest device checklist.

## Visual language

- Michael: moon-blue, cyan mana flow, white stone, restrained gold trim.
- Layout: triangular silhouette, central construction court, palace/workshop skyline at the rear, one gate on each forward edge.
- Imported art first: reuse the licensed KayKit Dungeon and Kenney Nature assets already in the repository, then add only curated CC0 assets that visibly improve the island.
- Custom geometry is limited to structural pieces unique to this layout: triangular ground, rune paths, gate sockets, interaction pedestals, and safety colliders.
- Text is functional UI, never baked into generated textures. Every label uses a positive scale and an explicit camera-facing component.

## Small milestones

| ID | Deliverable | Independent acceptance check |
|---|---|---|
| MI-00 | Freeze scope, plan, and progress log | Plan and log are committed on Michael's branch |
| MI-01 | Curate licensed art assets | Source URL/license recorded; only selected files imported with `.meta` files |
| MI-02 | Build triangular island shell | Old circular island is absent; triangle floor, underside, rim, and rear skyline are present |
| MI-03 | Add two future bridge gates | Lucas and Justin gates are distinct, labeled, and cannot send the player into the void |
| MI-04 | Relocate the construction court | Runtime grid is centered on Michael Island and every cell remains usable |
| MI-05 | Rebuild workshop dressing | Existing buildable machines, portal, props, lighting, and resource areas fit the new island |
| MI-06 | Add teammate delivery receivers | Bloom and Astral Core receivers support inspect/test delivery without teammate islands |
| MI-07 | Add central Nexus indicator | Indicator is presentation-only until Final Ritual readiness and never becomes a fourth island |
| MI-08 | Make locomotion surface-aware | Walk and teleport destinations are restricted to authored safe surfaces |
| MI-09 | Polish interaction feedback | All interactive objects have consistent prompts, audio/visual feedback, and failure messages |
| MI-10 | Verify world text and cameras | No mirrored text, negative text scale, duplicate camera, or unreadable critical label |
| MI-11 | Add automated EditMode/PlayMode checks | Tests cover resources, receivers, layout contracts, and critical progression |
| MI-12 | Run desktop rehearsal | Full Michael loop reaches Final Ritual without project-script exceptions |
| MI-13 | Build and verify Quest APK | ARM64/OpenXR build succeeds and device checklist is completed |
| MI-14 | Final documentation and evidence | Fresh-clone instructions, screenshots, test results, and known limitations are current |

## File ownership for this branch

This branch owns the main scene, Michael layout builders, shared manager changes required by Michael Island, VR safety, Michael-specific prefabs, tests, screenshots, and submission documentation. It does not implement or redesign the teammate islands.

## Commit discipline

Each milestone is committed and pushed separately. `Documentation/MICHAEL_ISLAND_LOG.md` is updated in the same milestone commit so Git history and the log remain aligned without an extra bookkeeping commit.
