# MP2B Shared Integration API

Michael's progression branch exposes a small contract on `ArcaneGameManager`. Justin and Lucas can integrate without reading or editing Michael's private inventory fields or campaign goal indexes.

## Submit teammate output

Justin's Conservatory submits two advanced crop outputs at harvest so one can bind the Foundry forge and one remains for the final Convergence recipe:

```csharp
manager.RegisterTeammateOutput(
    WorkshopResourceKind.ConservatoryBloom,
    2,
    "Justin");
```

Lucas's Astral Foundry spends one Conservatory Bloom plus its Crystal requirement, then submits the forged output with:

```csharp
manager.RegisterTeammateOutput(
    WorkshopResourceKind.AstralCore,
    1,
    "Lucas");
```

The first time both resource types are present, `AdvancedAlchemy` unlocks. Because the Foundry consumes only the first of Justin's two Blooms, one Bloom remains beside the new Astral Core. The player can then activate the Convergence Nexus or select Convergence Elixir, which atomically consumes those two teammate outputs together with its basic alchemy costs.

## Query or change shared resources

```csharp
int currentWater = manager.GetResource(WorkshopResourceKind.Water);
manager.AddResource(WorkshopResourceKind.Essence, 2);

bool paid = manager.TrySpendResources(
    new WorkshopResourceAmount(WorkshopResourceKind.Water, 1),
    new WorkshopResourceAmount(WorkshopResourceKind.Gold, 3));
```

`TrySpendResources` is atomic: either every listed cost is paid or no resource changes. Amounts must be non-negative. Use `RegisterTeammateOutput` rather than `AddResource` for Blooms and Astral Cores so the player receives delivery feedback.

Systems that need reactive presentation can subscribe to:

```csharp
manager.ResourceChanged += OnSharedResourceChanged;
```

The callback receives the resource kind, previous value, and new value.

## Query progression

```csharp
bool canBuild = manager.IsBuildingUnlocked(BuildingKind.StarCauldron);
bool canUseRecipe = manager.IsRecipeUnlocked(recipeIndex);
```

Do not depend on numeric goal indexes. Michael owns goal order and the final ritual; teammate systems own their local simulation and only exchange named resources through this API.

## Integration acceptance test

1. Harvest two Conservatory Blooms.
2. Confirm Convergence Elixir remains locked.
3. Forge one Astral Core, consuming one Bloom and leaving one Bloom available.
4. Confirm Convergence Elixir becomes selectable in both desktop UI and the VR spellbook.
5. Brew it at the Nexus and confirm the remaining Bloom and Astral Core both decrease by one.
6. Confirm the `Unite the Disciplines` campaign goal completes.

## Justin's Astral Foundry (in the shared scene)

Author: Michael (Ziyang Li), implementing Justin's part.

`AstralFoundry` (root `Justin Astral Foundry`, built by `MP2B > Build Justin Foundry Island`) runs the foundry loop locally and
only exchanges named resources: it spends Crystal (Star Catcher focus), Essence (Rune Well) and one Conservatory Bloom (forge
binder), then hands the forged core to Justin's receiver:

```csharp
receiver.Receive(1, "Justin's Astral Foundry"); // -> RegisterTeammateOutput(AstralCore, 1, ...)
```

`FoundryStation` components are routed by `ArcaneGameManager.ProcessPointer`; `AstralFoundry.InPointerZone(position)` keeps the
shared XR rig's workshop pointer active on the foundry island and its bridge. `IslandLinkGate.DockBridge(seal, tutorial)` marks
Justin's gate as docked: the solid seal blocks the bridge until the gate awakens on the first Bloom.
