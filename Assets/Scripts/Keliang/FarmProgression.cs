using System;
using UnityEngine;

public class FarmProgression : MonoBehaviour
{
    [SerializeField] private FarmResources resources;
    [SerializeField] private GameObject moonSeeds;
    [SerializeField] private GameObject bloomSeeds;
    [SerializeField] private GameObject sprinklerCarrier;
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

    public int Stage { get; private set; }
    public bool Revealing => pendingStage > Stage;
    public bool SprinklerUnlocked => Stage >= 3;
    public string CurrentGoal { get; private set; }
    public bool CanPlant(FarmCropKind kind) => Stage >= (kind == FarmCropKind.MoonHerb ? 1 : 2);

    private void Awake()
    {
        ApplyVisibility();
        RefreshGoal();
    }

    private void Update()
    {
        if (Revealing && Time.unscaledTime >= revealAt)
        {
            Stage = pendingStage;
            ApplyVisibility();
            ClearGuide();
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
        if ((Stage == 1 && kind == FarmCropKind.MoonHerb) || (Stage == 2 && kind == FarmCropKind.ConservatoryBloom)) lessonCrop = cell;
        ClearGuide();
    }

    public void OnHarvested(FarmCropKind kind)
    {
        if (Stage == 1 && kind == FarmCropKind.MoonHerb)
            Reveal(2, "BLOOM UNLOCKED", "Grow Bloom with the same tools. Its first harvest gives 2 crafting ingredients.");
        else if (Stage == 2 && kind == FarmCropKind.ConservatoryBloom)
            Reveal(3, "SPRINKLER UNLOCKED", "Buy a kit, place it on empty tilled soil, then fill it with water to irrigate nearby plots.");
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
        bloomSeeds.SetActive(Stage >= 2);
        sprinklerCarrier.SetActive(SprinklerUnlocked);
        sprinklerLabel.text = SprinklerUnlocked ? sprinklerPrice : "LOCKED\nHarvest Bloom\nGRIP for help";
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
        if (kit == FarmResource.SprinklerKit && !SprinklerUnlocked) return;
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
        string controls = "GRIP: grab / harvest\nTRIGGER while holding can: water\nY: spellbook resources";
        if (Revealing)
        {
            CurrentGoal = "A new farming tool is opening...";
            goalText.text = "DISCOVERY " + pendingStage + " / 3";
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
        else if (Stage >= 3)
        {
            CurrentGoal = "Take your Bloom to Justin's foundry and use it at the forge.";
            controls = "2 Blooms added to your spellbook.\nOptional: buy a sprinkler kit, place on empty tilled soil and fill with water.";
        }
        else
        {
            string crop = Stage == 1 ? "Moon Herb" : "Bloom";
            if (lessonCrop == null || !lessonCrop.IsPlanted)
                CurrentGoal = "Grab the " + crop + " seed bag and touch an empty tilled plot. Till a new plot if needed.";
            else if (lessonCrop.Crop.IsMature)
                CurrentGoal = crop + " is mature! Point at the plant and press GRIP to harvest.";
            else if (lessonCrop.Moisture <= 0)
                CurrentGoal = "Grab the watering can. Hold TRIGGER with the spout above your " + crop + " plot.";
            else CurrentGoal = "Keep the soil moist and wait for " + crop + " to mature. Growth: " + Mathf.FloorToInt(lessonCrop.Crop.Growth) + "%";
        }
        goalText.text = "NEXT STEP\n" + Wrap(CurrentGoal, 43);
        lessonText.text = Wrap(controls, 43);
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
