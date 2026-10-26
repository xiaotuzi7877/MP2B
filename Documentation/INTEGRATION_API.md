# MP2B Shared Integration API

Michael's progression branch exposes a small contract on `ArcaneGameManager`. Justin and Lucas can integrate without reading or editing Michael's private inventory fields or campaign goal indexes.

## Submit teammate output

Justin's Conservatory should submit its advanced crop output with:

```csharp
manager.RegisterTeammateOutput(
    WorkshopResourceKind.ConservatoryBloom,
    1,
    "Justin");
```

Lucas's Astral Foundry should submit its forged output with:

```csharp
manager.RegisterTeammateOutput(
    WorkshopResourceKind.AstralCore,
    1,
    "Lucas");
```

The first time both resource types are present, `AdvancedAlchemy` unlocks. The player can then select Convergence Elixir, which atomically consumes one Bloom and one Astral Core together with its basic alchemy costs.

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

1. Submit one Conservatory Bloom.
2. Confirm Convergence Elixir remains locked.
3. Submit one Astral Core.
4. Confirm Convergence Elixir becomes selectable in both desktop UI and the VR spellbook.
5. Brew it and confirm both teammate resources decrease by one.
6. Confirm the `Unite the Disciplines` campaign goal completes.
