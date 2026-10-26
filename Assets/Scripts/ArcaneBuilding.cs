using UnityEngine;

namespace ArcaneWorkshop
{
    public sealed class ArcaneBuilding : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private BuildingKind kind;

        [Header("Shared Stats")]
        [SerializeField] private float integrity = 100f;
        [SerializeField] private float cooldown;

        [Header("Planter Stats")]
        [SerializeField] private float growth;
        [SerializeField] private float moisture = 35f;
        [SerializeField] private float potency = 10f;

        [Header("Machine Stats")]
        [SerializeField] private float heat = 20f;
        [SerializeField] private float brewProgress;
        [SerializeField] private float mana = 25f;
        [SerializeField] private float purity = 60f;
        [SerializeField] private float charge = 30f;
        [SerializeField] private float resonance = 50f;

        // Extra Resonance from sparks and Mana Stills; it fades so the base hum stays visible.
        private float resonanceBoost;

        private ArcaneGameManager manager;
        private TextMesh statLabel;
        private float labelTimer;
        private bool brewing;
        private int activeRecipe = -1;

        public BuildingKind Kind { get { return kind; } }
        public GridCell Cell { get; private set; }
        public float Mana { get { return mana; } }

        // Neighbors only draw Mana above this reserve. A planter plus an amplifier drain faster than the
        // still refills, so without it the still sat at 0 and could never be collected.
        private const float ManaReserve = 30f;
        private float ShareableMana { get { return Mathf.Max(0f, mana - ManaReserve); } }
        public float Charge { get { return charge; } }
        public float Integrity { get { return integrity; } }
        public float Growth { get { return growth; } }
        public float Moisture { get { return moisture; } }
        public float Heat { get { return heat; } }
        public float BrewProgress { get { return brewProgress; } }
        public bool IsBrewing { get { return brewing; } }
        public float Purity { get { return purity; } }
        public float Resonance { get { return resonance; } }

        private void Awake()
        {
            statLabel = GetComponentInChildren<TextMesh>(true);
        }

        private void Update()
        {
            if (manager == null)
            {
                return;
            }

            TickStats(Time.deltaTime);
            labelTimer -= Time.deltaTime;
            if (labelTimer <= 0f)
            {
                labelTimer = 0.2f;
                RefreshLabel();
            }
        }

        public void ConfigureForKind(BuildingKind newKind)
        {
            kind = newKind;
            integrity = 100f;
            cooldown = 0f;
            growth = 0f;
            moisture = 35f;
            potency = 10f;
            heat = 20f;
            brewProgress = 0f;
            mana = 25f;
            purity = 60f;
            charge = 30f;
            resonance = 50f;
            resonanceBoost = 0f;
        }

        public void SetLabelVisible(bool visible)
        {
            if (statLabel != null)
            {
                statLabel.gameObject.SetActive(visible);
            }
        }

        public void Initialize(ArcaneGameManager newManager, GridCell newCell)
        {
            manager = newManager;
            Cell = newCell;
            RefreshLabel();
        }

        public void Interact()
        {
            if (manager == null)
            {
                return;
            }

            switch (kind)
            {
                case BuildingKind.RuneFoundation:
                    Repair();
                    break;

                case BuildingKind.MoonPlanter:
                    TendPlanter();
                    break;

                case BuildingKind.ManaStill:
                    CollectFromStill();
                    break;

                case BuildingKind.StarCauldron:
                    StartBrew();
                    break;

                case BuildingKind.CrystalAmplifier:
                    ChargeAmplifier();
                    break;
            }

            RefreshLabel();
        }

        // Returns the building that gave up a stat this tick, or null if the pair does not interact.
        public ArcaneBuilding ApplyNeighborInteraction(ArcaneBuilding other, float deltaTime)
        {
            if (other == null)
            {
                return null;
            }

            if (MatchesPair(kind, other.kind, BuildingKind.ManaStill, BuildingKind.MoonPlanter))
            {
                ArcaneBuilding still = kind == BuildingKind.ManaStill ? this : other;
                ArcaneBuilding planter = kind == BuildingKind.MoonPlanter ? this : other;
                float transfer = Mathf.Min(still.ShareableMana, 1.8f * deltaTime);
                still.mana -= transfer;
                planter.moisture = Mathf.Min(100f, planter.moisture + transfer * 1.4f);
                planter.growth = Mathf.Min(100f, planter.growth + transfer * 0.18f);
                return transfer > 0f ? still : null;
            }
            else if (MatchesPair(kind, other.kind, BuildingKind.CrystalAmplifier, BuildingKind.StarCauldron))
            {
                ArcaneBuilding amplifier = kind == BuildingKind.CrystalAmplifier ? this : other;
                ArcaneBuilding cauldron = kind == BuildingKind.StarCauldron ? this : other;
                float transfer = Mathf.Min(amplifier.charge, 1.2f * deltaTime);
                amplifier.charge -= transfer;
                cauldron.heat = Mathf.Min(100f, cauldron.heat + transfer * 1.5f);
                cauldron.brewProgress = Mathf.Min(100f, cauldron.brewProgress + (cauldron.brewing ? transfer * 0.2f : 0f));
                return transfer > 0f ? amplifier : null;
            }
            else if (MatchesPair(kind, other.kind, BuildingKind.ManaStill, BuildingKind.CrystalAmplifier))
            {
                ArcaneBuilding still = kind == BuildingKind.ManaStill ? this : other;
                ArcaneBuilding amplifier = kind == BuildingKind.CrystalAmplifier ? this : other;
                float transfer = Mathf.Min(still.ShareableMana, 0.9f * deltaTime);
                still.mana -= transfer;
                amplifier.charge = Mathf.Min(100f, amplifier.charge + transfer);
                amplifier.resonanceBoost = Mathf.Min(50f, amplifier.resonanceBoost + transfer * 0.35f);
                return transfer > 0f ? still : null;
            }

            return null;
        }

        public void Repair()
        {
            if (integrity >= 100f)
            {
                manager.SetFeedback(DisplayLabel() + " is already at full Integrity.");
                return;
            }

            integrity = Mathf.Min(100f, integrity + 25f);
            ArcaneAudio.Play(SoundId.Repair, transform.position);
            manager.SetFeedback("Re-inscribed the runes: " + DisplayLabel() + " Integrity +25.");
            manager.ShowTutorial("repair", "Repair: Shift + right-click any building (or right-click a bare Rune Foundation) to restore its Integrity stat.", transform.position);
            RefreshLabel();
        }

        private void Damage(float amount)
        {
            integrity = Mathf.Max(0f, integrity - amount);
            RefreshLabel();
            if (integrity <= 0f)
            {
                manager.DestroyBuilding(this);
            }
            else
            {
                manager.SetFeedback("A falling star struck the " + DisplayLabel() + "! Integrity " + Mathf.RoundToInt(integrity) + ".");
                manager.ShowTutorial("falling-star", "Falling stars randomly strike buildings and lower their Integrity. At 0 Integrity the building crumbles - Shift + right-click to repair it.", transform.position);
            }
        }

#if UNITY_EDITOR
        public void DebugSetGrowth(float value) { growth = value; }
        public void DebugDamage(float amount) { Damage(amount); }
#endif

        private string DisplayLabel()
        {
            switch (kind)
            {
                case BuildingKind.RuneFoundation: return "Rune Foundation";
                case BuildingKind.MoonPlanter: return "Moon Planter";
                case BuildingKind.ManaStill: return "Mana Still";
                case BuildingKind.StarCauldron: return "Star Cauldron";
                default: return "Crystal Amplifier";
            }
        }

        private void TickStats(float deltaTime)
        {
            cooldown = Mathf.Max(0f, cooldown - deltaTime);

            switch (kind)
            {
                case BuildingKind.RuneFoundation:
                    // Runes slowly fade toward 70 on their own; stars can push them lower.
                    if (integrity > 70f)
                    {
                        integrity = Mathf.Max(70f, integrity - deltaTime * 0.015f);
                    }
                    break;

                case BuildingKind.MoonPlanter:
                    moisture = Mathf.Max(0f, moisture - deltaTime * 0.65f);
                    if (moisture > 5f)
                    {
                        growth = Mathf.Min(100f, growth + deltaTime * (0.65f + potency * 0.01f));
                        potency = Mathf.Min(100f, potency + deltaTime * 0.08f);
                    }
                    break;

                case BuildingKind.ManaStill:
                    mana = Mathf.Min(100f, mana + deltaTime * 2.1f);
                    purity = Mathf.Min(100f, purity + deltaTime * 0.12f);
                    break;

                case BuildingKind.StarCauldron:
                    heat = Mathf.MoveTowards(heat, 25f, deltaTime * 0.5f);
                    if (brewing)
                    {
                        brewProgress += deltaTime * (3.2f + heat * 0.055f);
                        if (brewProgress >= 100f)
                        {
                            brewing = false;
                            brewProgress = 0f;
                            manager.CompleteBrew(activeRecipe, transform.position);
                            activeRecipe = -1;
                        }
                    }
                    break;

                case BuildingKind.CrystalAmplifier:
                    charge = Mathf.Min(100f, charge + deltaTime * 0.45f);
                    resonanceBoost = Mathf.Max(0f, resonanceBoost - deltaTime * 1.5f);
                    resonance = Mathf.Clamp(50f + Mathf.Sin(Time.time * 1.8f) * 20f + resonanceBoost, 0f, 100f);
                    break;
            }
        }

        private void TendPlanter()
        {
            if (growth >= 100f)
            {
                manager.AddHerbs(3);
                manager.AddEssence(1);
                ArcaneAudio.Play(SoundId.PlanterHarvest, transform.position);
                growth = 0f;
                moisture = Mathf.Max(10f, moisture - 15f);
                potency = 10f;
                manager.SetFeedback("Harvested 3 Moon Herbs and 1 Essence.");
                manager.ShowTutorial("harvest-planter", "A mature planter can be harvested. This destroys its stored crop state and increases player resources.", transform.position);
                return;
            }

            if (manager.TrySpendWater(1))
            {
                manager.SpawnWaterDrops(this);
                manager.SetFeedback("Watering the Moon Planter: each droplet that lands raises Moisture and Growth.");
                manager.ShowTutorial("caretaking", "Caretaking: right-click a Moon Planter to spend Water. The falling droplets raise its Moisture and Growth when they hit the soil.", transform.position);
            }
            else
            {
                ArcaneAudio.Play2D(SoundId.UiError);
                manager.SetFeedback("You need 1 Water to tend this planter.");
            }
        }

        private void CollectFromStill()
        {
            if (mana < 20f)
            {
                ArcaneAudio.Play2D(SoundId.UiError);
                manager.SetFeedback("The Mana Still needs at least 20 Mana.");
                return;
            }

            mana -= 20f;
            ArcaneAudio.Play(SoundId.StillCollect, transform.position);
            manager.AddWater(2);
            manager.AddEssence(1);
            manager.SetFeedback("Condensed 2 Water and 1 Essence from the Mana Still.");
            manager.ShowTutorial("still-collect", "Harvest the Mana Still: once Mana reaches 20, right-click to condense it into 2 Water and 1 Essence.", transform.position);
        }

        private void StartBrew()
        {
            if (brewing)
            {
                manager.SetFeedback("The cauldron is already brewing: " + Mathf.FloorToInt(brewProgress) + "%.");
                return;
            }

            int recipeIndex = manager.SelectedRecipe;
            if (!manager.TryConsumeRecipe(recipeIndex))
            {
                ArcaneAudio.Play2D(SoundId.UiError);
                manager.SetFeedback("Not enough resources for the selected recipe.");
                return;
            }

            activeRecipe = recipeIndex;
            brewing = true;
            brewProgress = 0f;
            heat = Mathf.Max(heat, 35f);
            ArcaneAudio.Play(SoundId.BrewStart, transform.position);
            manager.SpawnIngredientMotes(this);
            manager.SetFeedback("Brew started. Heat and neighboring amplifiers increase the rate.");
            manager.ShowTutorial("crafting", "Crafting: choose a recipe, then right-click the Star Cauldron. Ingredient motes visibly collide with it.", transform.position);
        }

        private void ChargeAmplifier()
        {
            if (!manager.TrySpendCrystals(1))
            {
                ArcaneAudio.Play2D(SoundId.UiError);
                manager.SetFeedback("You need 1 Crystal to charge the amplifier.");
                return;
            }

            manager.AddEssence(1);
            ArcaneAudio.Play(SoundId.SparkLaunch, transform.position);
            manager.SpawnCrystalSpark(this);
            manager.SetFeedback("A crystal spark flies into the amplifier (+1 Essence).");
        }

        private void RefreshLabel()
        {
            if (statLabel == null)
            {
                return;
            }

            string damage = kind != BuildingKind.RuneFoundation && integrity < 99.5f ? "\nIntegrity " + Mathf.RoundToInt(integrity) : "";

            switch (kind)
            {
                case BuildingKind.RuneFoundation:
                    statLabel.text = "RUNE FLOOR\nIntegrity " + Mathf.RoundToInt(integrity);
                    break;
                case BuildingKind.MoonPlanter:
                    statLabel.text = "MOON PLANTER\nGrowth " + Mathf.RoundToInt(growth) + "%\nMoisture " + Mathf.RoundToInt(moisture) + "\nPotency " + Mathf.RoundToInt(potency) + damage;
                    break;
                case BuildingKind.ManaStill:
                    statLabel.text = "MANA STILL\nMana " + Mathf.RoundToInt(mana) + "\nPurity " + Mathf.RoundToInt(purity) + damage;
                    break;
                case BuildingKind.StarCauldron:
                    statLabel.text = "STAR CAULDRON\nHeat " + Mathf.RoundToInt(heat) + "\nBrew " + Mathf.RoundToInt(brewProgress) + "%" + damage;
                    break;
                case BuildingKind.CrystalAmplifier:
                    statLabel.text = "AMPLIFIER\nCharge " + Mathf.RoundToInt(charge) + "\nResonance " + Mathf.RoundToInt(resonance) + damage;
                    break;
            }
        }

        private static bool MatchesPair(BuildingKind first, BuildingKind second, BuildingKind a, BuildingKind b)
        {
            return (first == a && second == b) || (first == b && second == a);
        }

        // Collision interactions: each projectile type changes this building's Stats on impact
        // and is destroyed. Projectiles only react with the building they were aimed at.
        private void OnTriggerEnter(Collider other)
        {
            ArcaneProjectile projectile = other.GetComponent<ArcaneProjectile>();
            if (projectile == null || projectile.Target != this || manager == null)
            {
                return;
            }

            switch (projectile.Kind)
            {
                case ProjectileKind.Ingredient:
                    ArcaneAudio.Play(SoundId.IngredientPlop, transform.position);
                    brewProgress = Mathf.Min(100f, brewProgress + 4f);
                    heat = Mathf.Min(100f, heat + 2f);
                    break;

                case ProjectileKind.WaterDrop:
                    ArcaneAudio.Play(SoundId.WaterDrop, transform.position);
                    moisture = Mathf.Min(100f, moisture + 12f);
                    growth = Mathf.Min(100f, growth + 3f);
                    break;

                case ProjectileKind.CrystalSpark:
                    ArcaneAudio.Play(SoundId.SparkHit, transform.position);
                    charge = Mathf.Min(100f, charge + 40f);
                    resonanceBoost = Mathf.Min(50f, resonanceBoost + 25f);
                    manager.ShowTutorial("amp-charge", "Caretaking: right-click a Crystal Amplifier to spend 1 Crystal. The spark's impact adds Charge and Resonance.", transform.position);
                    break;

                case ProjectileKind.FallingStar:
                    ArcaneAudio.Play(SoundId.StarImpact, transform.position);
                    projectile.Consume();
                    Damage(34f);
                    return;

                default:
                    return;
            }

            projectile.Consume();
            RefreshLabel();
        }
    }
}
