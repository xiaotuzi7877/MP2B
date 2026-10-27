using UnityEngine;

namespace ArcaneWorkshop
{
    public enum TeamStationKind
    {
        ArcaneConservatory,
        AstralFoundry,
        ConvergenceNexus
    }

    /// <summary>
    /// Playable team-owned stations integrated through ArcaneGameManager's shared resource API.
    /// Each station keeps its local simulation private and exchanges only named resources.
    /// </summary>
    public sealed class TeamExpansionStation : MonoBehaviour
    {
        [SerializeField] private TeamStationKind kind;
        [SerializeField] private ArcaneGameManager manager;
        [SerializeField] private TextMesh statusLabel;
        [SerializeField] private Transform animatedPart;
        [SerializeField] private Renderer stateRenderer;
        [SerializeField] private Light accentLight;

        [Header("Conservatory Stats")]
        [SerializeField] private float growth;
        [SerializeField] private float moisture = 40f;
        [SerializeField] private float fertility = 55f;
        [SerializeField] private float pollination = 20f;
        [SerializeField] private float familiarBond = 25f;

        [Header("Foundry Stats")]
        [SerializeField] private int starFragments;
        [SerializeField] private float runePower;
        [SerializeField] private float forgeHeat = 20f;
        [SerializeField] private float shield = 25f;

        private float labelTimer;
        private float pulse;
        private MaterialPropertyBlock propertyBlock;
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public TeamStationKind Kind { get { return kind; } }
        public float Growth { get { return growth; } }
        public float Moisture { get { return moisture; } }
        public float Fertility { get { return fertility; } }
        public float Pollination { get { return pollination; } }
        public float FamiliarBond { get { return familiarBond; } }
        public int StarFragments { get { return starFragments; } }
        public float RunePower { get { return runePower; } }
        public float ForgeHeat { get { return forgeHeat; } }
        public float Shield { get { return shield; } }
        public bool CanInterceptFallingStar { get { return kind == TeamStationKind.AstralFoundry && shield >= 20f; } }

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        private void Start()
        {
            if (manager == null)
            {
                manager = FindAnyObjectByType<ArcaneGameManager>();
            }
            RefreshLabel();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            pulse += deltaTime;

            if (kind == TeamStationKind.ArcaneConservatory)
            {
                moisture = Mathf.Max(0f, moisture - deltaTime * 0.32f);
                fertility = Mathf.Max(10f, fertility - deltaTime * 0.035f);
                pollination = Mathf.MoveTowards(pollination, familiarBond * 0.72f, deltaTime * 0.22f);
                familiarBond = Mathf.Min(100f, familiarBond + deltaTime * 0.025f);
                if (moisture > 4f)
                {
                    float ecology = 0.24f + moisture * 0.004f + fertility * 0.003f + pollination * 0.003f;
                    growth = Mathf.Min(100f, growth + deltaTime * ecology);
                }
            }
            else if (kind == TeamStationKind.AstralFoundry)
            {
                forgeHeat = Mathf.MoveTowards(forgeHeat, 20f, deltaTime * 0.45f);
                shield = Mathf.Max(0f, shield - deltaTime * 0.018f);
                runePower = Mathf.Max(0f, runePower - deltaTime * 0.012f);
            }

            if (animatedPart != null)
            {
                float direction = kind == TeamStationKind.ArcaneConservatory ? 12f : kind == TeamStationKind.AstralFoundry ? -24f : 32f;
                animatedPart.Rotate(Vector3.up, direction * deltaTime, Space.Self);
                float bob = Mathf.Sin(pulse * 1.8f) * 0.04f;
                Vector3 localPosition = animatedPart.localPosition;
                localPosition.y = Mathf.Max(0.2f, localPosition.y + bob * deltaTime);
                animatedPart.localPosition = localPosition;
            }

            UpdateVisualState();
            labelTimer -= deltaTime;
            if (labelTimer <= 0f)
            {
                labelTimer = 0.2f;
                RefreshLabel();
            }
        }

        public void Configure(
            TeamStationKind stationKind,
            ArcaneGameManager gameManager,
            TextMesh label,
            Transform motion,
            Renderer state,
            Light light)
        {
            kind = stationKind;
            manager = gameManager;
            statusLabel = label;
            animatedPart = motion;
            stateRenderer = state;
            accentLight = light;
            RefreshLabel();
        }

        public void Interact(bool modifier)
        {
            if (manager == null)
            {
                manager = FindAnyObjectByType<ArcaneGameManager>();
            }
            if (manager == null)
            {
                return;
            }

            switch (kind)
            {
                case TeamStationKind.ArcaneConservatory:
                    InteractWithConservatory(modifier);
                    break;
                case TeamStationKind.AstralFoundry:
                    InteractWithFoundry(modifier);
                    break;
                default:
                    InteractWithNexus(modifier);
                    break;
            }
            RefreshLabel();
        }

        public string InteractionHint()
        {
            switch (kind)
            {
                case TeamStationKind.ArcaneConservatory:
                    return "Use: tend or harvest. Shift + use: send the Familiar to pollinate.";
                case TeamStationKind.AstralFoundry:
                    return "Use: capture, charge, then forge. Shift + use: reinforce the Shield.";
                default:
                    return "Use: perform Convergence Alchemy or the unlocked Final Ritual.";
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            ArcaneProjectile projectile = other.GetComponent<ArcaneProjectile>();
            if (projectile == null || projectile.Kind != ProjectileKind.FallingStar || projectile.Target != this)
            {
                return;
            }

            projectile.Consume();
            if (shield >= 20f)
            {
                shield -= 20f;
                runePower = Mathf.Min(100f, runePower + 22f);
                forgeHeat = Mathf.Min(100f, forgeHeat + 12f);
                if (manager != null)
                {
                    manager.SetFeedback("The Astral Shield intercepted a falling star and converted its impact into Rune Power.");
                }
            }
            RefreshLabel();
        }

        private void InteractWithConservatory(bool modifier)
        {
            if (growth >= 100f)
            {
                manager.RegisterTeammateOutput(WorkshopResourceKind.ConservatoryBloom, 2, "Justin's Conservatory");
                growth = 0f;
                moisture = Mathf.Max(20f, moisture - 30f);
                fertility = Mathf.Max(25f, fertility - 18f);
                pollination = Mathf.Max(10f, pollination - 35f);
                familiarBond = Mathf.Min(100f, familiarBond + 8f);
                manager.SetFeedback("Conservatory harvest complete: +2 Conservatory Blooms — one for Foundry binding and one for Convergence.");
                return;
            }

            if (modifier)
            {
                if (!manager.TrySpendResource(WorkshopResourceKind.Essence, 1))
                {
                    manager.SetFeedback("The pollinating Familiar needs 1 Essence.");
                    return;
                }
                familiarBond = Mathf.Min(100f, familiarBond + 18f);
                pollination = Mathf.Min(100f, pollination + 38f);
                growth = Mathf.Min(100f, growth + 18f);
                manager.SetFeedback("The Familiar carried arcane pollen across the Conservatory: Pollination and Growth increased.");
                return;
            }

            if (!manager.TrySpendResource(WorkshopResourceKind.Water, 1))
            {
                manager.SetFeedback("The Conservatory needs 1 Water for caretaking.");
                return;
            }
            moisture = Mathf.Min(100f, moisture + 42f);
            fertility = Mathf.Min(100f, fertility + 12f);
            growth = Mathf.Min(100f, growth + 24f);
            manager.SetFeedback("Conservatory tended: Moisture, Fertility, and Growth increased. Harvest at 100% Growth.");
        }

        private void InteractWithFoundry(bool modifier)
        {
            if (modifier)
            {
                if (!manager.TrySpendResource(WorkshopResourceKind.Crystal, 1))
                {
                    manager.SetFeedback("The Astral Shield needs 1 Crystal reinforcement.");
                    return;
                }
                shield = Mathf.Min(100f, shield + 32f);
                runePower = Mathf.Min(100f, runePower + 12f);
                manager.SetFeedback("Astral Shield reinforced: falling-star energy is redirected into Rune Power.");
                return;
            }

            if (starFragments < 2)
            {
                if (!manager.TrySpendResource(WorkshopResourceKind.Crystal, 1))
                {
                    manager.SetFeedback("Capturing a Star Fragment requires 1 Crystal focus.");
                    return;
                }
                starFragments++;
                runePower = Mathf.Min(100f, runePower + 18f);
                manager.SetFeedback("Captured Star Fragment " + starFragments + "/2. Use the Foundry again to continue the rune path.");
                return;
            }

            if (runePower < 50f)
            {
                if (!manager.TrySpendResource(WorkshopResourceKind.Essence, 1))
                {
                    manager.SetFeedback("The Rune Path needs 1 Essence to reach forging power.");
                    return;
                }
                runePower = Mathf.Min(100f, runePower + 55f);
                forgeHeat = Mathf.Min(100f, forgeHeat + 38f);
                manager.SetFeedback("Rune Path charged. The Foundry now needs 1 Conservatory Bloom as organic binding material.");
                return;
            }

            WorkshopResourceAmount[] forgeCost =
            {
                new WorkshopResourceAmount(WorkshopResourceKind.ConservatoryBloom, 1),
                new WorkshopResourceAmount(WorkshopResourceKind.Crystal, 1)
            };
            if (!manager.TrySpendResources(forgeCost))
            {
                manager.SetFeedback("Forging an Astral Core requires 1 Conservatory Bloom and 1 Crystal.");
                return;
            }

            starFragments -= 2;
            runePower = Mathf.Max(0f, runePower - 50f);
            forgeHeat = Mathf.Min(100f, forgeHeat + 45f);
            shield = Mathf.Min(100f, shield + 20f);
            manager.RegisterTeammateOutput(WorkshopResourceKind.AstralCore, 1, "Lucas's Astral Foundry");
            manager.SetFeedback("Astral forging complete: +1 Astral Core delivered to the shared workshop.");
        }

        private void InteractWithNexus(bool modifier)
        {
            if (modifier)
            {
                manager.SetFeedback("Nexus status: " + manager.TeamIntegrationSummary());
                return;
            }

            if (manager.FinalRitualReady)
            {
                manager.TryPerformFinalRitual();
                return;
            }

            // On failure TryPerformConvergenceBrew has already set the exact reason (locked / missing ingredients).
            if (!manager.TryPerformConvergenceBrew(transform.position))
            {
                ArcaneAudio.Play(SoundId.UiError, transform.position);
            }
        }

        private void RefreshLabel()
        {
            if (statusLabel == null)
            {
                return;
            }

            switch (kind)
            {
                case TeamStationKind.ArcaneConservatory:
                    statusLabel.text = "JUSTIN — ARCANE CONSERVATORY\nGrowth " + Mathf.RoundToInt(growth) +
                        "   Moisture " + Mathf.RoundToInt(moisture) + "\nFertility " + Mathf.RoundToInt(fertility) +
                        "   Pollination " + Mathf.RoundToInt(pollination) + "\nFamiliar Bond " + Mathf.RoundToInt(familiarBond) +
                        "\nUSE: Tend / Harvest   SHIFT+USE: Familiar";
                    break;
                case TeamStationKind.AstralFoundry:
                    statusLabel.text = "LUCAS — ASTRAL FOUNDRY\nFragments " + starFragments + "/2   Rune Power " +
                        Mathf.RoundToInt(runePower) + "\nForge Heat " + Mathf.RoundToInt(forgeHeat) +
                        "   Shield " + Mathf.RoundToInt(shield) +
                        "\nUSE: Capture / Charge / Forge\nSHIFT+USE: Reinforce Shield";
                    break;
                default:
                    string summary = manager == null ? "Awaiting workshop link" : manager.TeamIntegrationSummary();
                    statusLabel.text = "MICHAEL — CONVERGENCE ALTAR\n" + summary +
                        "\nCLICK / TRIGGER: Converge  •  Final Ritual";
                    break;
            }
        }

        private void UpdateVisualState()
        {
            float normalized;
            Color color;
            if (kind == TeamStationKind.ArcaneConservatory)
            {
                normalized = growth / 100f;
                color = Color.Lerp(new Color(0.12f, 0.32f, 0.16f), new Color(0.25f, 1f, 0.42f), normalized);
            }
            else if (kind == TeamStationKind.AstralFoundry)
            {
                normalized = Mathf.Max(runePower, shield) / 100f;
                color = Color.Lerp(new Color(0.18f, 0.12f, 0.25f), new Color(0.9f, 0.35f, 1f), normalized);
            }
            else
            {
                normalized = manager != null && manager.FinalRitualReady ? 1f : 0.45f;
                color = Color.Lerp(new Color(0.12f, 0.45f, 0.75f), new Color(1f, 0.76f, 0.22f), normalized);
            }

            if (stateRenderer != null)
            {
                stateRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(ColorId, color);
                stateRenderer.SetPropertyBlock(propertyBlock);
            }
            if (accentLight != null)
            {
                accentLight.color = color;
                accentLight.intensity = 1.2f + normalized * 1.8f + Mathf.Sin(pulse * 2f) * 0.12f;
            }
        }
    }
}
