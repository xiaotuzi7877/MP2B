using System;
using ArcaneWorkshop;
using UnityEngine;

public class FarmProgression : MonoBehaviour, IIslandGuide
{
    [SerializeField] private FarmResources resources;
    [SerializeField] private GameObject moonSeeds;
    [SerializeField] private GameObject bloomSeeds;
    [SerializeField] private GameObject sprinklerCarrier;
    [SerializeField] private GameObject miningTool;
    [SerializeField] private GameObject miningField;
    [SerializeField] private GameObject pickaxeCarrier;
    [SerializeField] private GameObject tankCarrier;
    [SerializeField] private KitPurchase sprinklerPurchase;
    [SerializeField] private KitPurchase tankPurchase;
    [SerializeField] private KitPurchase pickaxePurchase;
    [SerializeField] private TextMesh goalText;
    [SerializeField] private TextMesh lessonText;
    [SerializeField] private TextMesh sprinklerLabel;
    [SerializeField] private string sprinklerPrice;
    [SerializeField] private float revealSeconds = 2f;
    private int pendingStage;
    private float revealAt, nextRefresh;
    private FarmCell lessonCrop;
    private string introduction, recovery;
    private Func<bool> recoveryComplete;
    private bool shoppingHelp;
    private int shoppingStones, shoppingCoins;
    private FarmResource shoppingKit;
    private bool plantedAndWatered, harvestedHerb, harvestedBloom, irrigatedNeighbor, tankUpgraded;
    private Sprinkler lessonSprinkler;

    public int Stage { get; private set; }
    public bool Revealing => pendingStage > Stage;
    public bool MiningUnlocked => Stage >= 2;
    public bool SprinklerUnlocked => Stage >= 4;
    public bool TankUnlocked => Stage >= 5;
    public string CurrentGoal { get; private set; }
    public bool CanPlant(FarmCropKind kind) => Stage >= (kind == FarmCropKind.MoonHerb ? 1 : 3);
    public bool CanBuyKit(FarmResource kit) => kit switch
    {
        FarmResource.PickaxeKit => MiningUnlocked,
        FarmResource.SprinklerKit => SprinklerUnlocked,
        FarmResource.TankKit => TankUnlocked,
        _ => true
    };
    public string KitRequirement(FarmResource kit) => kit switch
    {
        FarmResource.PickaxeKit => "Plant and water a Moon Herb to unlock mining.",
        FarmResource.SprinklerKit => "Harvest Bloom to unlock the sprinkler.",
        FarmResource.TankKit => "Use a sprinkler to water a nearby tilled plot to unlock tank upgrades.",
        _ => "Complete the current farm lesson."
    };

    private void Awake()
    {
        ApplyVisibility();
        RefreshGoal();
    }

    private void OnEnable() => IslandGuides.Register(this);
    private void OnDisable() => IslandGuides.Unregister(this);

    // Spellbook guide (Michael): the same next step as the lesson board, always in the player's left hand.
    public bool Contains(Vector3 position)
    {
        Vector3 local = transform.root.InverseTransformPoint(position);
        return Mathf.Abs(local.x) < 16.5f && Mathf.Abs(local.z) < 16.5f;
    }

    public string GuideTitle => resources.BloomHarvested
        ? "LUCAS'S FARM  -  BLOOM DONE"
        : "LUCAS'S FARM  -  STEP " + Mathf.Min(5, (Revealing ? pendingStage : Stage) + 1) + " / 5";

    public string GuideText()
    {
        var text = new System.Text.StringBuilder();
        text.Append(Revealing ? Wrap(introduction, 42) : "NEXT STEP\n" + Wrap(CurrentGoal, 42));
        text.Append("\n\nRIGHT HAND\nGRIP near a tool: pick it up, release: drop\nHarrow / seed bag: touch a plot\n")
            .Append("Watering can: tilt it over a plot\nRipe plant: point at it + GRIP to harvest\n\n");
        text.Append("FARM  Seeds ").Append(resources.GetAmount(FarmResource.Seeds))
            .Append("   Coins ").Append(resources.GetAmount(FarmResource.Coins))
            .Append("   Stones ").Append(resources.GetAmount(FarmResource.Stones))
            .Append("\nSHARED  Moon Herbs ").Append(resources.Crops)
            .Append("   Blooms ").Append(resources.SharedBlooms);
        if (resources.BloomHarvested)
            text.Append("\n\n").Append(Wrap("Lucas's part is done. Forge 1 Bloom at Justin's foundry (east bridge), " +
                "keep 1 for the altar at home. Sprinkler lessons are optional.", 42));
        return text.ToString();
    }

    private void Update()
    {
        if (Revealing && Time.unscaledTime >= revealAt)
        {
            Stage = pendingStage;
            ApplyVisibility();
            ClearGuide();
            Advance();
        }
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + .25f;
        if (recoveryComplete != null && recoveryComplete()) ClearGuide();
        RefreshGoal();
    }

    public void OnTilled()
    {
        if (Stage == 0) Reveal(1, "MOON HERB UNLOCKED", "Plant a seed, water the soil, then harvest the mature herb.");
        else ClearGuide();
    }

    public void OnPlanted(FarmCell cell, FarmCropKind kind)
    {
        if ((Stage <= 2 && kind == FarmCropKind.MoonHerb) || (Stage == 3 && kind == FarmCropKind.ConservatoryBloom)) lessonCrop = cell;
        ClearGuide();
    }

    public void OnHarvested(FarmCropKind kind)
    {
        if (kind == FarmCropKind.MoonHerb) harvestedHerb = true;
        else harvestedBloom = true;
        Advance();
    }

    public void OnWatered(FarmCell cell, FarmCropKind kind)
    {
        if (plantedAndWatered || kind != FarmCropKind.MoonHerb || !cell.IsPlanted) return;
        plantedAndWatered = true;
        lessonCrop = cell;
        Advance();
    }

    public void OnSprinklerBuilt(Sprinkler sprinkler) { lessonSprinkler = sprinkler; }

    public void OnIrrigated(Sprinkler sprinkler)
    {
        lessonSprinkler = sprinkler;
        if (irrigatedNeighbor) return;
        irrigatedNeighbor = true;
        Advance();
    }

    public void OnTankUpgraded() { tankUpgraded = true; ClearGuide(); }

    // Remember successful actions during introductions; never skip a reveal or lose a milestone.
    private void Advance()
    {
        if (Revealing) return;
        if (Stage == 1 && plantedAndWatered)
            Reveal(2, "MINING UNLOCKED", "Swing the pickaxe into rocks and collect stone drops. Mine while your Moon Herb grows.");
        else if (Stage == 2 && harvestedHerb)
            Reveal(3, "BLOOM UNLOCKED", "Grow Bloom with the same tools. Its first harvest gives 2 shared crafting ingredients.");
        else if (Stage == 3 && harvestedBloom)
            Reveal(4, "SPRINKLER UNLOCKED", "Buy a kit, place it on empty tilled soil, then fill it to water nearby plots automatically.");
        else if (Stage == 4 && irrigatedNeighbor)
            Reveal(5, "TANK UPGRADE UNLOCKED", "Buy a tank kit and touch your sprinkler with its carrier. Capacity increases from 100 to 200.");
    }

    private void Reveal(int next, string title, string text)
    {
        if (Revealing || next != Stage + 1) return;
        ClearGuide();
        pendingStage = next;
        introduction = title + "\n" + text;
        revealAt = Time.unscaledTime + revealSeconds;
        RefreshGoal();
    }

    private void ApplyVisibility()
    {
        moonSeeds.SetActive(Stage >= 1);
        bloomSeeds.SetActive(Stage >= 3);
        sprinklerCarrier.SetActive(SprinklerUnlocked);
        if (miningTool) miningTool.SetActive(MiningUnlocked);
        if (miningField) miningField.SetActive(MiningUnlocked);
        if (pickaxeCarrier) pickaxeCarrier.SetActive(MiningUnlocked);
        if (tankCarrier) tankCarrier.SetActive(TankUnlocked);
        sprinklerLabel.text = SprinklerUnlocked ? sprinklerPrice : "LOCKED\nHarvest Bloom\nGRIP for help";
        if (pickaxePurchase) pickaxePurchase.RefreshAvailabilityLabel();
        if (tankPurchase) tankPurchase.RefreshAvailabilityLabel();
    }

    public void Teach(string nextStep, Func<bool> completed)
    {
        shoppingHelp = false;
        recovery = nextStep;
        recoveryComplete = completed;
        RefreshGoal();
    }

    public void TeachPurchase(FarmResource kit, int stones, int coins)
    {
        ClearGuide();
        if (!CanBuyKit(kit)) return;
        shoppingHelp = true;
        shoppingKit = kit;
        shoppingStones = stones;
        shoppingCoins = coins;
        RefreshGoal();
    }

    public void ClearGuide()
    {
        recovery = null;
        recoveryComplete = null;
        shoppingHelp = false;
        RefreshGoal();
    }

    private void RefreshGoal()
    {
        string controls = "GRIP: grab / harvest\nTilt the watering can to pour\nY: hide / show spellbook";
        if (Revealing)
        {
            CurrentGoal = "A new farming tool is opening...";
            goalText.text = "DISCOVERY " + pendingStage + " / 5";
            lessonText.text = Wrap(introduction, 43);
            return;
        }
        if (shoppingHelp)
        {
            int stones = Mathf.Max(0, shoppingStones - resources.GetAmount(FarmResource.Stones));
            int coins = Mathf.Max(0, shoppingCoins - resources.GetAmount(FarmResource.Coins));
            CurrentGoal = stones > 0 || coins > 0
                ? (stones > 0 ? "Need " + stones + " stones: grab the pickaxe, break rocks and collect the drops. " : "")
                    + (coins > 0 ? "Need " + coins + " coins: trade Moon Herbs at the farm shop." : "")
                : "Materials ready. Return to the shop and GRIP the " + (shoppingKit == FarmResource.SprinklerKit ? "sprinkler" : "upgrade") + " purchase button.";
        }
        else if (!string.IsNullOrEmpty(recovery)) CurrentGoal = recovery;
        else if (Stage == 0) CurrentGoal = "Grab the harrow and touch an untilled plot to prepare the soil.";
        else if (Stage == 4)
        {
            if (!lessonSprinkler)
                CurrentGoal = resources.GetAmount(FarmResource.SprinklerKit) > 0
                    ? "Take the sprinkler carrier and touch an empty tilled plot."
                    : PurchaseGoal(sprinklerPurchase, "sprinkler");
            else if (lessonSprinkler.Water <= 0f)
                CurrentGoal = "Fill your sprinkler with the watering can. Prepare a nearby plot for it to water.";
            else CurrentGoal = "Till a nearby plot without a sprinkler. Let automatic watering increase its moisture to unlock the tank upgrade.";
            controls = "2 Blooms are in your spellbook.\nGoal: irrigate a nearby plot.";
        }
        else if (Stage >= 5)
        {
            CurrentGoal = tankUpgraded
                ? "Farm lessons complete! Use Moon Herbs and Bloom in the main workshop. Y opens shared resources."
                : resources.GetAmount(FarmResource.TankKit) > 0
                    ? "Grab the tank upgrade carrier and touch your sprinkler to increase capacity to 200."
                    : PurchaseGoal(tankPurchase, "tank upgrade");
            controls = "GRIP: grab the upgrade carrier\nTouch the sprinkler to upgrade\nY: shared resources";
        }
        else
        {
            string crop = Stage <= 2 ? "Moon Herb" : "Bloom";
            if (lessonCrop == null || !lessonCrop.IsPlanted)
                CurrentGoal = "Grab the " + crop + " seed bag and touch an empty tilled plot. Till a new plot if needed.";
            else if (Stage == 1)
                CurrentGoal = "Water your planted Moon Herb: grab the watering can and tilt it over the plot. This unlocks mining.";
            else if (lessonCrop.Crop.IsMature)
                CurrentGoal = crop + " is mature! Point at the plant and press GRIP to harvest.";
            else if (lessonCrop.Moisture <= 0)
                CurrentGoal = "Grab the watering can and tilt it over your " + crop + " plot.";
            else CurrentGoal = "Keep the soil moist and wait for " + crop + " to mature. Growth: " + Mathf.FloorToInt(lessonCrop.Crop.Growth) + "%";
            if (Stage == 2) controls = "While it grows: GRIP the pickaxe,\nswing into rocks and collect stones.\nHarvest Moon Herb to unlock Bloom.";
        }
        goalText.text = "NEXT STEP\n" + Wrap(CurrentGoal, 43);
        lessonText.text = Wrap(controls, 43);
    }

    private string PurchaseGoal(KitPurchase purchase, string label)
    {
        if (!purchase) return "Buy a " + label + " kit at the farm shop.";
        int stones = Mathf.Max(0, purchase.StoneCost - resources.GetAmount(FarmResource.Stones));
        int coins = Mathf.Max(0, purchase.CoinCost - resources.GetAmount(FarmResource.Coins));
        return stones > 0 || coins > 0
            ? "For the " + label + ": " + (stones > 0 ? "mine " + stones + " more stones. " : "")
                + (coins > 0 ? "Trade Moon Herbs for " + coins + " more coins." : "")
            : "Materials ready. GRIP the " + label + " purchase button at the farm shop.";
    }

    public static string Wrap(string text, int width)
    {
        var result = new System.Text.StringBuilder();
        foreach (string line in text.Split('\n'))
        {
            int count = 0;
            foreach (string word in line.Split(' '))
            {
                if (count > 0 && count + word.Length + 1 > width) { result.Append('\n'); count = 0; }
                if (count > 0) { result.Append(' '); count++; }
                result.Append(word); count += word.Length;
            }
            result.Append('\n');
        }
        return result.ToString().TrimEnd();
    }
}
