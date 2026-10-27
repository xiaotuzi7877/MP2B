using UnityEngine;

public class Sprinkler : MonoBehaviour
{
    [SerializeField] private GameObject basicVisual;
    [SerializeField] private GameObject upgradedVisual;
    [SerializeField] private Transform waterLevel;
    [SerializeField] private TextMesh waterText;
    [SerializeField] private LineRenderer[] streams;
    [SerializeField] private Transform nozzle;
    [SerializeField] private ActionFeedback feedback;
    [SerializeField] private float water;
    [SerializeField] private float capacity = 100f;
    [SerializeField] private float wateringRate = 6f;
    [SerializeField] private float consumptionRate = 2f;
    private FarmResources resources;
    private FarmCell[] neighbors;
    public float Water => water;
    public float Capacity => capacity;
    public bool IsUpgraded { get; private set; }

    public void Initialize(FarmResources inventory, FarmCell[] adjacentCells)
    {
        resources = inventory;
        neighbors = adjacentCells;
        RefreshDisplay();
    }
    public void AddWater(float amount)
    {
        water = Mathf.Clamp(water + amount, 0f, capacity);
        RefreshDisplay();
    }
    public bool Upgrade()
    {
        if (resources.Progression != null && !resources.Progression.TankUnlocked)
        {
            resources.Progression.ClearGuide();
            feedback.Failure("Tank upgrade locked. No kit spent.\n" + resources.Progression.KitRequirement(FarmResource.TankKit)
                + "\nNext: " + resources.Progression.CurrentGoal);
            return false;
        }
        if (IsUpgraded) { feedback.Failure("Tank already upgraded"); return false; }
        if (!resources.TrySpend(FarmResource.TankKit, 1))
        {
            feedback.Failure("No tank kit. Buy one at the farm shop. Nothing spent.");
            resources.Progression?.Teach("Buy a tank upgrade kit at the farm shop, then touch the sprinkler with its carrier.", () => resources.GetAmount(FarmResource.TankKit) > 0);
            return false;
        }
        IsUpgraded = true;
        capacity = 200f;
        basicVisual.SetActive(false);
        upgradedVisual.SetActive(true);
        RefreshDisplay();
        feedback.Success("Tank upgraded: 100 -> 200");
        resources.Progression?.OnTankUpgraded();
        return true;
    }
    private void Update()
    {
        bool watered = false;
        for (int i = 0; i < streams.Length; i++)
        {
            bool active = i < neighbors.Length && water > 0f && Time.deltaTime > 0f
                && neighbors[i].IsTilled && !neighbors[i].HasSprinkler && neighbors[i].Moisture < 100f;
            streams[i].enabled = active;
            if (!active) continue;
            float fraction = Mathf.Min(1f, water / (consumptionRate * Time.deltaTime));
            float before = neighbors[i].Moisture;
            neighbors[i].Water(wateringRate * Time.deltaTime * fraction);
            if (neighbors[i].Moisture > before) resources.Progression?.OnIrrigated(this);
            Vector3 end = neighbors[i].transform.position + Vector3.up * .18f;
            streams[i].SetPosition(0, nozzle.position);
            streams[i].SetPosition(1, (nozzle.position + end) / 2f + Vector3.up * .35f);
            streams[i].SetPosition(2, end);
            watered = true;
        }
        if (watered)
        {
            water = Mathf.Max(0f, water - consumptionRate * Time.deltaTime);
            RefreshDisplay();
        }
    }
    private void RefreshDisplay()
    {
        float fraction = water / capacity;
        waterLevel.localScale = new Vector3(.13f, .5f * fraction, .018f);
        waterLevel.localPosition = new Vector3(0f, .3f + .25f * fraction, -.325f);
        waterText.text = $"Water: {water:0} / {capacity:0}";
    }
}
