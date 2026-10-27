using UnityEngine;
using UnityEngine.UI;

public class PlayerFarmHud : MonoBehaviour
{
    [SerializeField] private FarmResources resources;
    [SerializeField] private FarmCell[] cells;
    [SerializeField] private Transform view;
    [SerializeField] private Text inventoryText;
    [SerializeField] private Text kitsText;
    [SerializeField] private Text statusText;
    [SerializeField] private Text focusText;
    [SerializeField] private float inspectDistance = 6f;
    private float nextRefresh;

    private void OnEnable()
    {
        resources.Changed += RefreshInventory;
        RefreshInventory();
        nextRefresh = 0f;
    }

    private void OnDisable() => resources.Changed -= RefreshInventory;

    private void RefreshInventory()
    {
        inventoryText.text = $"Crops {resources.Crops}   Seeds {resources.GetAmount(FarmResource.Seeds)}   Coins {resources.GetAmount(FarmResource.Coins)}   Fertilizer {resources.GetAmount(FarmResource.Fertilizer)}   Stones {resources.GetAmount(FarmResource.Stones)}";
        kitsText.text = $"KITS   Sprinkler {resources.GetAmount(FarmResource.SprinklerKit)}   Pickaxe {resources.GetAmount(FarmResource.PickaxeKit)}   Tank {resources.GetAmount(FarmResource.TankKit)}   Soil {resources.GetAmount(FarmResource.SoilKit)}";
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + .2f;
        int ready = 0, dry = 0, unhealthy = 0, empty = 0;
        foreach (var cell in cells)
        {
            if (cell.IsPlanted)
            {
                if (cell.Crop.IsMature) ready++;
                if (cell.Moisture <= 0f) dry++;
                if (cell.Health.Health < 50f) unhealthy++;
            }
            if (cell.HasSprinkler && cell.Irrigator.Water <= 0f) empty++;
        }
        statusText.text = $"Ready {ready}   Dry crops {dry}   Low health {unhealthy}   Empty tanks {empty}";
        if (resources.GetAmount(FarmResource.Seeds) == 0) statusText.text += "   BUY SEEDS";
        statusText.color = dry + unhealthy + empty > 0 ? new Color(1f, .74f, .35f) : new Color(.7f, .94f, .76f);

        FarmCell target = null;
        if (Physics.Raycast(view.position, view.forward, out var hit, inspectDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            target = hit.collider.GetComponentInParent<FarmCell>();
        if (target == null)
        {
            focusText.text = "Look at a plot to inspect it";
            return;
        }
        string label = $"Plot {System.Array.IndexOf(cells, target) + 1}";
        if (target.HasSprinkler)
        {
            var tank = target.Irrigator;
            focusText.text = $"{label} | Sprinkler {(tank.IsUpgraded ? "II" : "I")} | Water {tank.Water:0}/{tank.Capacity:0}";
        }
        else if (!target.IsTilled) focusText.text = $"{label} | Untilled - use the rake";
        else
        {
            focusText.text = $"{label} | Water {target.Moisture:0}% | Soil {(target.IsSoilUpgraded ? "II" : "I")}";
            if (target.IsPlanted)
                focusText.text += $"\nGrowth {target.Crop.Growth:0}%   Health {target.Health.Health:0}%   {(target.Crop.IsMature ? "READY TO HARVEST" : target.Crop.IsFertilized ? "Fertilized" : "Growing")}";
            else focusText.text += " | Ready to sow";
        }
    }
}
