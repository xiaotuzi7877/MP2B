# Michael Island — Work Log (single source of truth)

Base copy: `/Users/miclee/Desktop/MP2/MP2B` · Branch: `michael/island-final` (from `main`, merged back into `main` at the end)
Unity 6000.5.6f1 · Built-in RP · OpenXR Quest 3 · Due 2026-10-27 8 pm

Direction (2026-10-27): keep the MP2A round Arcane Workshop island and gameplay exactly as designed, and add only what MP2B needs:
two bridge gates (Lucas west — matches his `mainToFarmBridge`; Justin east), teammate supply receivers, the Convergence Altar,
the floating Nexus Core between the islands, plus art, lighting, sound, colliders and interaction polish.
The old triangle branch (`feature/michael-progression`) is history; only its receiver API and label fix were reused.

Rules
- Each step ≈ 10–15 min. `[x]` = done (commit hash once pushed).
- Push after each finished group.
- `▶ STOPPED HERE` marks where the last session ended; the next person/AI starts from that line.
- Rebuild the MP2B additions any time with **MP2B › Build Michael Island**; check with **MP2B › Validate Michael Progression**.

## A. Reset onto MP2A
- [x] A1 New branch `michael/island-final` from `main`; triangle work set aside
- [x] A2 Audit `main` scene in Play Mode (screenshots): fake teammate districts on the island, rune paths over the grid, black sky, flat light, props without colliders, floor CapsuleCollider bug

## B. Island structure (MP2B additions) — commit e94e0d4
- [x] B1 Remove `MP2B Team Expansion` (fake Conservatory/Foundry districts, rune paths, old title)
- [x] B2 `MichaelIslandBuilder` editor tool (idempotent) — rebuilds only the MP2B layer
- [x] B3 Lucas bridge gate (west) + Justin bridge gate (east): stone ring, glyph ring, portal veil, swirl particles, 3D hum, solid barrier, wooden pier where the teammate bridge docks
- [x] B4 Gate progression `IslandLinkGate`: Sealed → Awakened (Lucas: Restoration Goal 6; Justin: first Bloom received) → Linked (`Link()` hook for MP2C)
- [x] B5 Supply receivers: `Receive()` integration API + standalone trade order (pay ingredients → crate flies in over the gate)
- [x] B6 Convergence Altar (replaces the back "Nexus" disc), click or trigger both work
- [x] B7 Floating Nexus Core in the void (display only, dormant → ready → resonating)
- [x] B8 Move MP2A trees/crates out of the two bridge corridors; wild spawns avoid the corridors
- [x] B9 Validator updated: 46/46 pass
- [x] B10 Nexus Core moved high above the back wall so it is visible from the spawn and during the ritual; title plaque raised above the ending panel

## C. Art and lighting — commit e94e0d4 (+ group 2)
- [x] C1 CC0 assets: Kenney Castle + Fantasy Town (curated), ambientCG PavingStones070 / Rock030
- [x] C2 Paved floor (texture + normal map), rock underside mesh + boulders, stone rim balustrade
- [x] C3 Night-sky shader (stars, moon, nebula), trilight ambient, matched fog
- [x] C4 Moon key light with soft shadows; point-light shadows off (Quest cost)
- [x] C5 Ambient mana motes; faceted crystal mesh for Nexus / altar / receivers
- [x] C6 Back wall reviewed in Play Mode — existing MP2A torches already give the warm interior; no change needed
- [x] C7 Screenshot pass from VR spawn, gates, altar, merchant, underside; README preview `Documentation/MICHAEL_ISLAND_PREVIEW.png`

## D. Colliders, sound, interaction
- [x] D1 Floor CapsuleCollider → MeshCollider; BoxColliders + `WalkBlocker` on walls, pillars, furniture, trees, merchant pillars
- [x] D2 VR smooth walk slides along solid props; teleport rejects targets inside props
- [x] D3 Ambient night loop synthesised in code (wind, drone, chimes; seamless) + 3D hum at altar and awakened gates
- [x] D4 Hover feedback: gate/receiver/altar/Nexus labels grow while the mouse or VR ray rests on them
- [x] D5 Goal hints name the bridge steps (goal 6 awakens Lucas's gate; goal 7 says where to order and converge)

## E. Verification and hand-off
- [x] E1 Play Mode chain test: goal 6 → Lucas gate awakens → order Bloom → Justin gate awakens → order Astral Core → Advanced Alchemy → altar Convergence, no errors
- [x] E2 Full run in Play Mode through real pointer rays: gather → foundations → planter → still → cauldron brew → merchant sale → amplifier → Lucas gate → Bloom → Justin gate → Astral Core → altar Convergence → Final Ritual → ending panel + Nexus resonating. Only the slow Mana-Link growth and the 100-gold sales total were fast-forwarded. No Console errors.
- [x] E3a VR collision logic checked in Play Mode with the rig's capsule test: workbench, barrels, receivers, altar, gate barrier and back wall block; grid, spawn, both gate corridors and the altar front are free
- [ ] E3b On-headset check (lab PC + Quest Link): spawn, walk into props, teleport, ray at gates — not possible on this Mac
- [x] E4 README + this log, merged `michael/island-final` → `main` (0f5aaa9), pushed


## F. Headset feedback fixes (2026-10-27, branch `michael/fixes-oct27` from main 8118739 incl. Keliang's farm)
Findings: Lucas gate was moved by Keliang to the SW diagonal (yaw 218°, r 9) to meet `mainToFarmBridge`; MP2A props sit at y 0
while the paved floor top is -0.16 (16 cm floating); back-wall segments overlap on the same plane (z-fighting = flicker);
altar gold disc coplanar with floor; VR feedback text only appears on the spellbook, so failed altar/ritual presses look like
"nothing happens"; XRI JumpProvider is live; spellbook is hard-parented to the tracked hand (every tremor shows).
- [x] F1 Layout: Justin gate mirrored to the SE diagonal (yaw 142°), Merchant Gate moved to Justin's old east slot; receivers beside their gates; balustrade gaps follow the gates
- [x] F2 Grounding: MP2A props, merchant gate and gate parts sit on the floor (-0.16)
- [x] F3 Flicker: back-wall segments offset off the shared plane; altar disc lifted; Quest shadow settings
- [x] F4 Ritual island: Nexus Core becomes a floating, non-landable ritual islet at the centre of the three islands
- [x] F5 Step 7/8 interaction: world-space feedback toast at the pointer target + error sounds; altar label lists exact needs and progress
- [x] F6 Comfort: jump off, spellbook tremor filter (6 mm hand jitter → 0.5 mm), Quest quality without shadow maps (no refresh-rate change: XR runtime default kept)
- [x] F5b Altar no longer overwrites the exact failure reason with a generic line; failed presses play the error sound; Nexus Core starts the ritual once aligned
- [x] F7 Validator 123/123 (new layout checks: gate mirror, merchant clearance, grounded props, wall planes, unreachable islet); Play Mode presses at altar/Nexus/receiver/merchant, steps 7 → 8 → ritual, no Console errors
- [ ] F8 On-headset check of comfort and the toast (lab)

- [x] F9 (PR #5 merged, approved by Keliang23) Lucas gate barrier was solid forever (nothing calls `Link()`), blocking the walk onto `mainToFarmBridge`; it is now a clickable trigger where a real bridge docks, Justin's stays solid. Validator 125/125

## G. Michael (work on Justin's part): Astral Foundry (2026-10-28, branch `michael/justin-foundry`)
Author: Michael. Justin's branch had no island work, so Michael implemented Justin's part in Michael Island's art and interaction language.
- [x] G1 Runtime: `AstralFoundry` + `FoundryStation` (Star Catcher -> Rune Well -> Astral Forge -> core crosses the bridge to Justin's receiver); falling stars, crash penalty, rune leak, forge animation, `>>` next-step labels, arrival tutorial, toasts and error sounds
- [x] G2 Integration: pointer dispatch in `ProcessPointer`; XR rig pointer active on the foundry/bridge; `Region.Foundry` teleport surface; receiver redirects orders to the forge; `IslandLinkGate.DockBridge` seal; goal 7 hint
- [x] G3 `JustinFoundryBuilder`: island mirrored from Keliang's farm (28,0,-36), stone bridge, rim + invisible edge wall, Michael Island edge-wall gap at 142°; run by Build Michael Island too
- [x] G4 Validator 139/139 (foundry placement, bridge/teleport safety, seal, machines)
- [x] G5 Play Mode: sealed gate blocks the CharacterController at r 8.8, awakened gate walks to the island; bridge sides and island edge hold; pointer-ray loop forge -> catcher -> well -> forge; core delivered (AstralCore 1); no Console errors
- [x] G7 Desktop inspect view (no headset): free camera, Tab cycles workshop / farm / foundry / ritual islet, V returns to the player eye; mouse clicks still work through it
- [ ] G6 On-headset check of the foundry (lab)

▶ STOPPED HERE — pushed `michael/justin-foundry`; user opens the PR. Remaining: F8/G6 on headset, video, contribution summary.
