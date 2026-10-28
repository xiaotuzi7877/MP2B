using System;
using ArcaneWorkshop;
using UnityEngine;

public class FarmResources : MonoBehaviour
{
    [SerializeField] private ArcaneGameManager workshop;
    [SerializeField] private FarmProgression progression;
    public FarmProgression Progression => progression;
    [SerializeField, Min(0)] private int crops;
    private bool bloomPlanted;
    private bool bloomHarvested;
    private bool changingSharedHerbs;

    private void OnEnable()
    {
        if (workshop != null) workshop.ResourceChanged += OnSharedResourceChanged;
    }

    private void OnDisable()
    {
        if (workshop != null) workshop.ResourceChanged -= OnSharedResourceChanged;
    }

    private void OnSharedResourceChanged(WorkshopResourceKind kind, int oldValue, int newValue)
    {
        if (kind == WorkshopResourceKind.MoonHerb && !changingSharedHerbs) Changed?.Invoke();
    }

    public bool CanPlantBloom => workshop != null && !bloomPlanted && !bloomHarvested;
    public bool BloomHarvested => bloomHarvested;
    public int SharedBlooms => workshop != null ? workshop.GetResource(WorkshopResourceKind.ConservatoryBloom) : 0;
    public void SetBloomPlanted(bool planted) => bloomPlanted = planted;

    public bool HarvestBloom()
    {
        if (workshop == null || bloomHarvested) return false;
        bloomHarvested = true;
        bloomPlanted = false;
        workshop.RegisterTeammateOutput(WorkshopResourceKind.ConservatoryBloom, 2, "Keliang's Conservatory");
        Changed?.Invoke();
        return true;
    }

    [SerializeField, Min(0)] private int seeds = 9;
    [SerializeField, Min(0)] private int coins = 5;
    [SerializeField, Min(0)] private int fertilizer;
    [SerializeField, Min(0)] private int stones;
    [SerializeField, Min(0)] private int sprinklerKit;
    [SerializeField, Min(0)] private int pickaxeKit;
    [SerializeField, Min(0)] private int tankKit;
    [SerializeField, Min(0)] private int soilKit;

    public int Crops => GetAmount(FarmResource.Crops);
    public event Action Changed;

    public int GetAmount(FarmResource resource) => resource switch
    {
        FarmResource.Crops => workshop != null ? workshop.GetResource(WorkshopResourceKind.MoonHerb) : crops,
        FarmResource.Seeds => seeds,
        FarmResource.Coins => coins,
        FarmResource.Fertilizer => fertilizer,
        FarmResource.Stones => stones,
        FarmResource.SprinklerKit => sprinklerKit,
        FarmResource.PickaxeKit => pickaxeKit,
        FarmResource.TankKit => tankKit,
        FarmResource.SoilKit => soilKit,
        _ => throw new ArgumentOutOfRangeException(nameof(resource))
    };

    public void AddCrop() => Add(FarmResource.Crops, 1);

    public void Add(FarmResource resource, int amount)
    {
        Change(resource, amount);
        Changed?.Invoke();
    }

    public bool TrySpend(FarmResource resource, int amount)
    {
        if (GetAmount(resource) < amount)
            return false;

        Change(resource, -amount);
        Changed?.Invoke();
        return true;
    }

    public bool TryTrade(FarmResource cost, int costAmount, FarmResource reward, int rewardAmount)
    {
        if (GetAmount(cost) < costAmount)
            return false;

        Change(cost, -costAmount);
        Change(reward, rewardAmount);
        Changed?.Invoke();
        return true;
    }

    private void Change(FarmResource resource, int amount)
    {
        switch (resource)
        {
            case FarmResource.Crops:
                // Keep the original farm shop working against the same harvested herbs as the spellbook.
                if (workshop == null) crops += amount;
                else
                {
                    changingSharedHerbs = true;
                    try
                    {
                        if (amount >= 0) workshop.AddResource(WorkshopResourceKind.MoonHerb, amount);
                        else workshop.TrySpendResource(WorkshopResourceKind.MoonHerb, -amount);
                    }
                    finally { changingSharedHerbs = false; }
                }
                break;
            case FarmResource.Seeds: seeds += amount; break;
            case FarmResource.Coins: coins += amount; break;
            case FarmResource.Fertilizer: fertilizer += amount; break;
            case FarmResource.Stones: stones += amount; break;
            case FarmResource.SprinklerKit: sprinklerKit += amount; break;
            case FarmResource.PickaxeKit: pickaxeKit += amount; break;
            case FarmResource.TankKit: tankKit += amount; break;
            case FarmResource.SoilKit: soilKit += amount; break;
            default: throw new ArgumentOutOfRangeException(nameof(resource));
        }
    }

    public bool TryBuyKit(int stoneCost, int coinCost, FarmResource kit) => TryBuyKit(stoneCost, coinCost, kit, out _);

    public bool TryBuyKit(int stoneCost, int coinCost, FarmResource kit, out string failure)
    {
        failure = null;
        if (progression != null && !progression.CanBuyKit(kit))
        {
            failure = "Invalid purchase: locked.\n" + progression.KitRequirement(kit) + "\nNo resources spent.";
            return false;
        }
        if (stoneCost < 0 || coinCost < 0)
        {
            failure = "Invalid purchase cost. No resources spent.";
            return false;
        }
        if (stones < stoneCost || coins < coinCost)
        {
            failure = $"Purchase failed: need {stoneCost} stones + {coinCost} coins.\nNo resources spent.";
            return false;
        }

        stones -= stoneCost;
        coins -= coinCost;
        Change(kit, 1);
        Changed?.Invoke();
        return true;
    }
}
