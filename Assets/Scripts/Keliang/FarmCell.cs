using UnityEngine;

public class FarmCell : MonoBehaviour
{
    [SerializeField] private GameObject untilledSoil;
    [SerializeField] private GameObject tilledSoilPrefab;
    [SerializeField] private GameObject seedlingPrefab;
    [SerializeField] private GameObject bloomPrefab;
    private FarmCropKind plantedKind;
    [SerializeField] private FarmResources resources;
    [SerializeField] private GameObject sprinklerPrefab;
    [SerializeField] private FarmCell[] neighbors;
    [SerializeField] private ActionFeedback feedback;

    private GameObject tilledSoil;
    private GameObject seedling;
    private SoilMoisture soil;
    private Sprinkler sprinkler;
    private CropGrowth cropGrowth;
    private CropHealth cropHealth;

    public bool IsTilled => tilledSoil != null;
    public bool IsPlanted => seedling != null;
    public bool HasSprinkler => sprinkler != null;
    public float Moisture => IsTilled ? soil.Moisture : 0f;
    public CropGrowth Crop => IsPlanted ? cropGrowth : null;
    public CropHealth Health => IsPlanted ? cropHealth : null;
    public Sprinkler Irrigator => sprinkler;
    public bool IsSoilUpgraded => IsTilled && soil.IsUpgraded;

    public bool Till()
    {
        if (IsTilled)
            return false;

        tilledSoil = Instantiate(tilledSoilPrefab, transform, false);
        tilledSoil.name = "tilledSoil";
        soil = tilledSoil.GetComponent<SoilMoisture>();
        untilledSoil.SetActive(false);
        resources.Progression?.OnTilled();
        return true;
    }

    public bool Plant(FarmCropKind kind = FarmCropKind.MoonHerb)
    {
        if (resources.Progression != null && !resources.Progression.CanPlant(kind))
        {
            resources.Progression.ClearGuide();
            return Reject("This seed is still locked. No seed spent.", resources.Progression.CurrentGoal, () => true);
        }
        if (!IsTilled) return Reject("Cannot plant in untilled soil.", "Use the harrow on this plot, then touch it with the seed bag.", () => IsTilled);
        if (IsPlanted || HasSprinkler) return Reject("This plot is occupied.", "Use another empty tilled plot.");
        if (kind == FarmCropKind.ConservatoryBloom && (bloomPrefab == null || !resources.CanPlantBloom))
            return Reject("Bloom is already planted or harvested. No seed spent.", "Care for your existing Bloom, or check your spellbook and take the harvest to the foundry.");
        if (!resources.TrySpend(FarmResource.Seeds, 1))
            return Reject("No seeds. Nothing was planted.", "Trade Moon Herbs for seeds at the farm shop.", () => resources.GetAmount(FarmResource.Seeds) > 0);

        plantedKind = kind;
        if (kind == FarmCropKind.ConservatoryBloom) resources.SetBloomPlanted(true);
        seedling = Instantiate(kind == FarmCropKind.ConservatoryBloom ? bloomPrefab : seedlingPrefab, tilledSoil.transform, false);
        seedling.name = kind == FarmCropKind.ConservatoryBloom ? "ConservatoryBloom" : "MoonHerb";
        cropGrowth = seedling.GetComponent<CropGrowth>();
        cropHealth = seedling.GetComponent<CropHealth>();
        cropGrowth.Initialize(soil);
        cropHealth.Initialize(this, soil);
        seedling.GetComponentInChildren<CropHarvest>(true).Initialize(this);
        resources.Progression?.OnPlanted(this, kind);
        return true;
    }

    public bool Harvest(CropGrowth crop)
    {
        if (!IsPlanted || crop.gameObject != seedling) return false;
        if (!crop.IsMature)
            return Reject("Not ready to harvest. Plant and resources unchanged.", "Keep the soil moist. When the plant is fully grown, press GRIP to harvest.", () => !IsPlanted || crop.IsMature);

        if (plantedKind == FarmCropKind.ConservatoryBloom)
        {
            if (!resources.HarvestBloom()) return false;
            feedback.Success("+2 Conservatory Blooms - see your spellbook");
        }
        else
        {
            resources.AddCrop();
            feedback.Success("+1 Moon Herb - see your spellbook");
        }
        seedling.SetActive(false);
        Destroy(seedling);
        seedling = null;
        resources.Progression?.OnHarvested(plantedKind);
        return true;
    }

    public void Water(float amount)
    {
        if (HasSprinkler)
            sprinkler.AddWater(amount);
        else if (IsTilled)
        {
            float before = soil.Moisture;
            soil.AddWater(amount);
            if (IsPlanted && soil.Moisture > before)
                resources.Progression?.OnWatered(this, plantedKind);
        }
    }

    public bool BuildSprinkler()
    {
        if (resources.Progression != null && !resources.Progression.SprinklerUnlocked)
        {
            resources.Progression.ClearGuide();
            return Reject("Sprinkler locked. No kit spent.", resources.Progression.CurrentGoal, () => true);
        }
        if (!IsTilled || IsPlanted || HasSprinkler)
        {
            return Reject("Cannot place a sprinkler here. No kit spent.", "Use the harrow to prepare an empty plot without a plant or sprinkler.");
        }
        if (!resources.TrySpend(FarmResource.SprinklerKit, 1))
        {
            return Reject("No sprinkler kit. Nothing was placed.", "Buy a sprinkler kit at the farm shop, then touch an empty tilled plot with its carrier.", () => resources.GetAmount(FarmResource.SprinklerKit) > 0);
        }
        var placed = Instantiate(sprinklerPrefab, transform, false);
        placed.name = "sprinkler";
        sprinkler = placed.GetComponent<Sprinkler>();
        sprinkler.Initialize(resources, neighbors);
        resources.Progression?.OnSprinklerBuilt(sprinkler);
        resources.Progression?.Teach("Fill the sprinkler with your watering can to irrigate nearby tilled plots.", () => sprinkler == null || sprinkler.Water > 0f);
        feedback.Success("Sprinkler built - fill with water");
        return true;
    }

    public bool UpgradeSoil()
    {
        if (!IsTilled || HasSprinkler || soil.IsUpgraded)
        {
            feedback.Failure("Use a tilled, unupgraded crop plot");
            return false;
        }
        if (!resources.TrySpend(FarmResource.SoilKit, 1))
        {
            feedback.Failure("Buy a soil kit first");
            return false;
        }
        soil.Upgrade();
        feedback.Success("Soil upgraded: it dries half as fast");
        return true;
    }

    public void CropDied(CropHealth crop)
    {
        if (seedling != crop.gameObject)
            return;

        if (plantedKind == FarmCropKind.ConservatoryBloom) resources.SetBloomPlanted(false);
        seedling.SetActive(false);
        Destroy(seedling);
        seedling = null;
    }

    public bool Fertilize()
    {
        if (!IsPlanted) return Reject("No crop to fertilize.", "Plant a seed in tilled soil before applying fertilizer.", () => IsPlanted);

        var crop = seedling.GetComponent<CropGrowth>();
        if (crop.IsMature || crop.IsFertilized)
            return Reject("This crop does not need more fertilizer. None spent.", "Keep it watered, then harvest when mature.", () => !IsPlanted || crop.IsMature);
        if (!resources.TrySpend(FarmResource.Fertilizer, 1))
            return Reject("No fertilizer. Nothing spent.", "Obtain fertilizer at the farm shop, or continue growing with water.", () => resources.GetAmount(FarmResource.Fertilizer) > 0 || !IsPlanted || crop.IsMature);

        crop.Fertilize();
        resources.Progression?.ClearGuide();
        return true;
    }

    private bool Reject(string reason, string nextStep, System.Func<bool> completed = null)
    {
        feedback.Failure(reason + "\nNext: " + nextStep);
        resources.Progression?.Teach(nextStep, completed);
        return false;
    }
}
