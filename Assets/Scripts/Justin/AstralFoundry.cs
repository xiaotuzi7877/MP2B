using System.Collections.Generic;
using UnityEngine;

namespace ArcaneWorkshop
{
    /// <summary>
    /// Justin's Astral Foundry island: the star-smith's loop that turns falling stars into the Astral Core.
    /// 1. Star Catcher: focus it with Crystal; each focus charge catches one falling star as a Star Fragment.
    ///    An unfocused catcher lets the star crash into the island, which drains the Rune Well.
    /// 2. Rune Well: pour Essence in to raise Rune Power (it slowly leaks away).
    /// 3. Astral Forge: 2 Star Fragments + 50 Rune Power + 1 Conservatory Bloom (Lucas's binder) forge one Astral Core,
    ///    which then floats back across the bridge into Justin's receiver on Michael Island.
    /// Only named resources cross into the shared workshop (Crystal, Essence and Bloom in; Astral Core out).
    /// </summary>
    public sealed class AstralFoundry : MonoBehaviour
    {
        public const int FragmentsNeeded = 2;
        public const int MaxFragments = 3;
        public const int ChargesPerCrystal = 2;
        public const int MaxCharges = 4;
        public const float RunePerEssence = 50f;
        public const float RuneNeeded = 50f;
        private const float StarInterval = 8f;
        private const float StarFallSeconds = 2.4f;
        private const float CrashDrain = 15f;
        private const float RuneLeakPerSecond = 0.15f;
        private const float ForgeSeconds = 3.5f;
        private const float DeliverySeconds = 5.5f;

        [SerializeField] private ArcaneGameManager manager;
        [SerializeField] private IslandLinkGate gate;
        [SerializeField] private TeammateDeliveryReceiver receiver;
        [SerializeField] private FoundryStation catcher;
        [SerializeField] private FoundryStation well;
        [SerializeField] private FoundryStation forge;
        [SerializeField] private Transform catchPoint;
        [SerializeField] private Transform crashPoint;
        [SerializeField] private Transform[] fragmentSlots;
        [SerializeField] private Renderer[] focusLamps;
        [SerializeField] private Renderer[] runeChannel;
        [SerializeField] private Transform wellCrystal;
        [SerializeField] private Transform hammer;
        [SerializeField] private Light forgeLight;
        [SerializeField] private ParticleSystem forgeSparks;
        [SerializeField] private Transform coreVisual;
        [SerializeField] private Transform coreOutput;
        [SerializeField] private Transform[] deliveryPath;
        [SerializeField] private Transform bridgeNear;
        [SerializeField] private Transform bridgeFar;
        [SerializeField] private float islandRadius = 8f;
        [SerializeField] private Material starMaterial;
        [SerializeField] private Color accent = new Color(0.78f, 0.42f, 1f);

        private static readonly List<AstralFoundry> Active = new List<AstralFoundry>();
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private static readonly Color Dormant = new Color(0.2f, 0.18f, 0.28f);

        private int fragments;
        private int focusCharges;
        private float runePower;
        private float forgeHeat;
        private int coresForged;
        private bool forging;
        private float forgeTimer;
        private float nextHammerSound;
        private bool delivering;
        private float deliveryTimer;
        private Transform star;
        private Vector3 starStart;
        private float starTimer = 3f;
        private float starT;
        private float labelTimer;
        private bool playerWasHere;
        private MaterialPropertyBlock block;

        public int Fragments { get { return fragments; } }
        public int FocusCharges { get { return focusCharges; } }
        public float RunePower { get { return runePower; } }
        public bool Forging { get { return forging; } }
        public bool Delivering { get { return delivering; } }
        public int CoresForged { get { return coresForged; } }
        public bool IsOpen { get { return gate == null || gate.IsOpen; } }
        public float IslandRadius { get { return islandRadius; } }

        public void Configure(ArcaneGameManager gameManager, IslandLinkGate linkGate, TeammateDeliveryReceiver coreReceiver,
            FoundryStation starCatcher, FoundryStation runeWell, FoundryStation astralForge,
            Transform catchAt, Transform crashAt, Transform[] slots, Renderer[] lamps, Renderer[] channel, Transform crystal,
            Transform forgeHammer, Light glow, ParticleSystem sparks, Transform core, Transform output, Transform[] path,
            Transform nearEnd, Transform farEnd, float radius, Material starGlow, Color accentColor)
        {
            manager = gameManager;
            gate = linkGate;
            receiver = coreReceiver;
            catcher = starCatcher;
            well = runeWell;
            forge = astralForge;
            catchPoint = catchAt;
            crashPoint = crashAt;
            fragmentSlots = slots;
            focusLamps = lamps;
            runeChannel = channel;
            wellCrystal = crystal;
            hammer = forgeHammer;
            forgeLight = glow;
            forgeSparks = sparks;
            coreVisual = core;
            coreOutput = output;
            deliveryPath = path;
            bridgeNear = nearEnd;
            bridgeFar = farEnd;
            islandRadius = radius;
            starMaterial = starGlow;
            accent = accentColor;
            RefreshLabels();
        }

        /// <summary>True on the foundry island or its bridge: the shared rig keeps its workshop pointer active here.</summary>
        public static bool InPointerZone(Vector3 position)
        {
            for (int i = 0; i < Active.Count; i++)
            {
                if (Active[i].Contains(position))
                {
                    return true;
                }
            }
            return false;
        }

        public bool Contains(Vector3 position)
        {
            Vector3 offset = position - transform.position;
            if (new Vector2(offset.x, offset.z).magnitude <= islandRadius + 1.5f)
            {
                return true;
            }
            if (bridgeNear == null || bridgeFar == null)
            {
                return false;
            }
            Vector2 p = new Vector2(position.x, position.z);
            Vector2 a = new Vector2(bridgeNear.position.x, bridgeNear.position.z);
            Vector2 b = new Vector2(bridgeFar.position.x, bridgeFar.position.z);
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 0.001f));
            return Vector2.Distance(p, a + ab * t) <= 2.4f;
        }

        private void OnEnable()
        {
            Active.Add(this);
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }

        private void Awake()
        {
            block = new MaterialPropertyBlock();
        }

        private void Start()
        {
            ResolveManager();
            if (coreVisual != null)
            {
                coreVisual.gameObject.SetActive(false);
            }
            if (receiver != null)
            {
                receiver.RedirectOrders("Astral Cores are forged on Justin's island. Cross the south-east bridge to the Astral Foundry.");
            }
            RefreshLabels();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            bool playerHere = PlayerHere();
            if (playerHere && !playerWasHere)
            {
                OnPlayerArrived();
            }
            playerWasHere = playerHere;

            if (IsOpen)
            {
                UpdateStarRain(dt, playerHere);
            }
            if (!forging)
            {
                runePower = Mathf.Max(0f, runePower - RuneLeakPerSecond * dt);
            }
            forgeHeat = Mathf.MoveTowards(forgeHeat, forging ? 1f : 0f, dt * (forging ? 0.6f : 0.25f));
            if (forging)
            {
                AdvanceForging(dt);
            }
            if (delivering)
            {
                AdvanceDelivery(dt);
            }

            UpdateVisuals(dt);
            labelTimer -= dt;
            if (labelTimer <= 0f)
            {
                labelTimer = 0.25f;
                RefreshLabels();
            }
        }

        // ------------------------------------------------------------------ player actions

        public void Use(FoundryStationKind kind, ArcaneGameManager gameManager)
        {
            if (gameManager != null)
            {
                manager = gameManager;
            }
            ResolveManager();
            if (manager == null)
            {
                return;
            }
            switch (kind)
            {
                case FoundryStationKind.StarCatcher:
                    FocusCatcher();
                    break;
                case FoundryStationKind.RuneWell:
                    ChargeWell();
                    break;
                default:
                    StartForging();
                    break;
            }
            RefreshLabels();
        }

        private void FocusCatcher()
        {
            Vector3 at = catcher != null ? catcher.transform.position : transform.position;
            if (focusCharges >= MaxCharges)
            {
                Fail("The Star Catcher is fully focused (" + focusCharges + " charges). Wait for the next falling star.", at);
                return;
            }
            if (!manager.TrySpendResource(WorkshopResourceKind.Crystal, 1))
            {
                Fail("Focusing the Star Catcher needs 1 Crystal. Shatter crystal clusters on Michael Island.", at);
                return;
            }
            focusCharges = Mathf.Min(MaxCharges, focusCharges + ChargesPerCrystal);
            starTimer = Mathf.Min(starTimer, 2f);
            ArcaneAudio.Play(SoundId.ShardPick, at);
            CelebrationSpark.Burst(at + Vector3.up * 2.2f, 6, manager.MoteMaterial);
            manager.SetFeedback("Star Catcher focused: it will catch the next " + focusCharges +
                " falling stars as Star Fragments. Watch the sky above it.");
        }

        private void ChargeWell()
        {
            Vector3 at = well != null ? well.transform.position : transform.position;
            if (runePower >= 100f - 0.5f)
            {
                Fail("The Rune Well is already full of Rune Power.", at);
                return;
            }
            if (!manager.TrySpendResource(WorkshopResourceKind.Essence, 1))
            {
                Fail("The Rune Well needs 1 Essence. Collect Essence from a Mana Still on Michael Island.", at);
                return;
            }
            runePower = Mathf.Min(100f, runePower + RunePerEssence);
            ArcaneAudio.Play(SoundId.WaterDrop, at);
            manager.SetFeedback("Essence poured into the Rune Well: Rune Power " + Mathf.RoundToInt(runePower) +
                ". The rune channel carries it to the Forge.");
        }

        private void StartForging()
        {
            Vector3 at = forge != null ? forge.transform.position : transform.position;
            if (forging || delivering)
            {
                Fail(forging ? "The Forge is already hammering an Astral Core." : "The last Astral Core is still crossing the bridge.", at);
                return;
            }
            string missing = ForgeMissing();
            if (missing != null)
            {
                Fail(missing, at);
                return;
            }
            if (!manager.TrySpendResource(WorkshopResourceKind.ConservatoryBloom, 1))
            {
                Fail(ForgeMissing() ?? "The Forge needs 1 Conservatory Bloom as binder.", at);
                return;
            }
            fragments -= FragmentsNeeded;
            runePower -= RuneNeeded;
            forging = true;
            forgeTimer = 0f;
            nextHammerSound = 0f;
            if (forgeSparks != null)
            {
                forgeSparks.Play();
            }
            ArcaneAudio.Play(SoundId.BrewStart, at);
            manager.SetFeedback("Forging: two Star Fragments, Rune Power and Lucas's Bloom are bound into an Astral Core...");
        }

        /// <summary>The first thing the Forge still needs, or null when it can forge.</summary>
        public string ForgeMissing()
        {
            if (fragments < FragmentsNeeded)
            {
                return "The Forge needs " + FragmentsNeeded + " Star Fragments (" + fragments + "/" + FragmentsNeeded +
                    "). Focus the Star Catcher with Crystal and let it catch falling stars.";
            }
            if (runePower < RuneNeeded)
            {
                return "The Forge needs " + Mathf.RoundToInt(RuneNeeded) + " Rune Power (now " + Mathf.RoundToInt(runePower) +
                    "). Pour Essence into the Rune Well.";
            }
            if (manager != null && manager.GetResource(WorkshopResourceKind.ConservatoryBloom) < 1)
            {
                return "The Forge needs 1 Conservatory Bloom as binder. Grow it on Lucas's farm or order it at his receiver.";
            }
            return null;
        }

        private void Fail(string message, Vector3 at)
        {
            manager.SetFeedback(message);
            ArcaneAudio.Play(SoundId.UiError, at);
        }

        // ------------------------------------------------------------------ simulation

        private void UpdateStarRain(float dt, bool playerHere)
        {
            if (star == null)
            {
                starTimer -= dt;
                if (starTimer <= 0f)
                {
                    LaunchStar();
                }
                return;
            }

            starT += dt / StarFallSeconds;
            Vector3 target = TargetForStar();
            float eased = starT * starT;
            star.position = Vector3.Lerp(starStart, target, eased);
            if (starT >= 1f)
            {
                LandStar(playerHere);
            }
        }

        private Vector3 TargetForStar()
        {
            if (focusCharges > 0 || crashPoint == null)
            {
                return catchPoint != null ? catchPoint.position : transform.position + Vector3.up;
            }
            return crashPoint.position;
        }

        private void LaunchStar()
        {
            starTimer = StarInterval;
            starT = 0f;
            Vector3 target = TargetForStar();
            Vector3 drift = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * 7f;
            starStart = target + Vector3.up * 16f + drift;
            GameObject starObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            starObject.name = "Foundry Falling Star";
            Destroy(starObject.GetComponent<Collider>());
            starObject.transform.position = starStart;
            starObject.transform.localScale = Vector3.one * 0.32f;
            Renderer renderer = starObject.GetComponent<Renderer>();
            if (starMaterial != null)
            {
                renderer.sharedMaterial = starMaterial;
            }
            TrailRenderer trail = starObject.AddComponent<TrailRenderer>();
            trail.time = 0.7f;
            trail.startWidth = 0.28f;
            trail.endWidth = 0f;
            trail.sharedMaterial = manager != null && manager.MoteMaterial != null ? manager.MoteMaterial : renderer.sharedMaterial;
            trail.startColor = new Color(1f, 0.88f, 0.55f);
            trail.endColor = new Color(accent.r, accent.g, accent.b, 0f);
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            star = starObject.transform;
            ArcaneAudio.Play(SoundId.StarWhoosh, target + Vector3.up * 4f);
        }

        private void LandStar(bool playerHere)
        {
            Vector3 at = star.position;
            Destroy(star.gameObject);
            star = null;
            if (focusCharges > 0)
            {
                focusCharges--;
                bool stored = fragments < MaxFragments;
                fragments = Mathf.Min(MaxFragments, fragments + 1);
                ArcaneAudio.Play(SoundId.SparkHit, at);
                CelebrationSpark.Burst(at, 8, manager != null ? manager.MoteMaterial : null);
                if (playerHere && manager != null)
                {
                    manager.SetFeedback(stored
                        ? "The Star Catcher caught a falling star: Star Fragment " + fragments + "/" + FragmentsNeeded + "."
                        : "The Star Catcher caught a star, but the fragment rack is full. Forge before focusing again.");
                }
            }
            else
            {
                runePower = Mathf.Max(0f, runePower - CrashDrain);
                ArcaneAudio.Play(SoundId.StarImpact, at);
                CelebrationSpark.Burst(at, 5, starMaterial);
                if (playerHere && manager != null)
                {
                    manager.SetFeedback("An unfocused star crashed into the foundry and drained " + Mathf.RoundToInt(CrashDrain) +
                        " Rune Power. Focus the Star Catcher with Crystal to catch the next one.");
                }
            }
            RefreshLabels();
        }

        /// <summary>Test hook: land the next falling star immediately.</summary>
        public void LandStarNow()
        {
            if (star == null)
            {
                LaunchStar();
            }
            starT = 1f;
            star.position = TargetForStar();
            LandStar(false);
        }

        private void AdvanceForging(float dt)
        {
            forgeTimer += dt;
            nextHammerSound -= dt;
            if (nextHammerSound <= 0f)
            {
                nextHammerSound = 0.7f;
                ArcaneAudio.Play(SoundId.SparkHit, forge != null ? forge.transform.position : transform.position);
            }
            if (forgeTimer < ForgeSeconds)
            {
                return;
            }

            forging = false;
            if (forgeSparks != null)
            {
                forgeSparks.Stop();
            }
            delivering = true;
            deliveryTimer = 0f;
            if (coreVisual != null)
            {
                coreVisual.gameObject.SetActive(true);
                coreVisual.position = coreOutput != null ? coreOutput.position : transform.position + Vector3.up;
            }
            Vector3 at = coreOutput != null ? coreOutput.position : transform.position;
            ArcaneAudio.Play(SoundId.BrewComplete, at);
            CelebrationSpark.Burst(at, 14, manager != null ? manager.MoteMaterial : null);
            if (manager != null)
            {
                manager.SetFeedback("Astral Core forged! It is floating back across the bridge to Justin's receiver on Michael Island.");
            }
        }

        /// <summary>Test hook: finish the running forge and delivery at once.</summary>
        public void CompleteForNow()
        {
            if (forging)
            {
                forgeTimer = ForgeSeconds;
                AdvanceForging(0f);
            }
            if (delivering)
            {
                deliveryTimer = DeliverySeconds;
                AdvanceDelivery(0f);
            }
        }

        private void AdvanceDelivery(float dt)
        {
            deliveryTimer += dt;
            float t = Mathf.Clamp01(deliveryTimer / DeliverySeconds);
            if (coreVisual != null && deliveryPath != null && deliveryPath.Length > 0)
            {
                coreVisual.position = PathPoint(t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.8f;
                coreVisual.Rotate(Vector3.up, 120f * dt, Space.World);
            }
            if (t < 1f)
            {
                return;
            }

            delivering = false;
            coresForged++;
            if (coreVisual != null)
            {
                coreVisual.gameObject.SetActive(false);
            }
            Vector3 landing = deliveryPath != null && deliveryPath.Length > 0 ? deliveryPath[deliveryPath.Length - 1].position : transform.position;
            CelebrationSpark.Burst(landing + Vector3.up * 0.4f, 10, manager != null ? manager.MoteMaterial : null);
            ArcaneAudio.Play(SoundId.CoinSale, landing);
            bool accepted = receiver != null && receiver.Receive(1, "Justin's Astral Foundry");
            if (!accepted && receiver == null && manager != null)
            {
                manager.RegisterTeammateOutput(WorkshopResourceKind.AstralCore, 1, "Justin's Astral Foundry");
            }
        }

        private Vector3 PathPoint(float t)
        {
            Vector3 start = coreOutput != null ? coreOutput.position : transform.position;
            int segments = deliveryPath.Length;
            float scaled = t * segments;
            int index = Mathf.Min(segments - 1, Mathf.FloorToInt(scaled));
            Vector3 from = index == 0 ? start : deliveryPath[index - 1].position;
            Vector3 to = deliveryPath[index].position;
            float local = scaled - index;
            return Vector3.Lerp(from, to, local * local * (3f - 2f * local));
        }

        // ------------------------------------------------------------------ presentation

        private void OnPlayerArrived()
        {
            ResolveManager();
            if (manager == null || !IsOpen)
            {
                return;
            }
            Vector3 at = catcher != null ? catcher.transform.position + Vector3.up * 2.6f : transform.position + Vector3.up * 2.5f;
            manager.ShowTutorial("foundry-arrive",
                "NEW - Justin's Astral Foundry. You are the star-smith: focus the Star Catcher with Crystal so it catches " +
                "falling stars, pour Essence into the Rune Well, then strike the Astral Forge. Lucas's Bloom binds the core.",
                at);
        }

        private bool PlayerHere()
        {
            Camera camera = Camera.main;
            return camera != null && Contains(camera.transform.position);
        }

        private void UpdateVisuals(float dt)
        {
            if (fragmentSlots != null)
            {
                for (int i = 0; i < fragmentSlots.Length; i++)
                {
                    if (fragmentSlots[i] == null)
                    {
                        continue;
                    }
                    bool show = i < fragments;
                    if (fragmentSlots[i].gameObject.activeSelf != show)
                    {
                        fragmentSlots[i].gameObject.SetActive(show);
                    }
                    fragmentSlots[i].Rotate(Vector3.up, 50f * dt, Space.World);
                }
            }
            if (focusLamps != null)
            {
                for (int i = 0; i < focusLamps.Length; i++)
                {
                    Tint(focusLamps[i], i < focusCharges, 1.8f);
                }
            }
            if (runeChannel != null)
            {
                float lit = runePower / 100f * runeChannel.Length;
                float wave = Time.time * 3f;
                for (int i = 0; i < runeChannel.Length; i++)
                {
                    bool on = i < lit;
                    Tint(runeChannel[i], on, on ? 1.2f + Mathf.Sin(wave - i * 0.8f) * 0.5f : 0f);
                }
            }
            if (wellCrystal != null)
            {
                wellCrystal.Rotate(Vector3.up, (15f + runePower * 1.4f) * dt, Space.World);
            }
            if (hammer != null)
            {
                float swing = forging ? Mathf.Abs(Mathf.Sin(forgeTimer * Mathf.PI / 0.7f)) : 0f;
                hammer.localRotation = Quaternion.Euler(Mathf.Lerp(-25f, 55f, swing), 0f, 0f); // raised back, strikes the anvil face
            }
            if (forgeLight != null)
            {
                forgeLight.intensity = 0.7f + forgeHeat * 2.6f + (forging ? Mathf.Sin(Time.time * 18f) * 0.3f : 0f);
                forgeLight.color = Color.Lerp(accent, new Color(1f, 0.62f, 0.25f), forgeHeat);
            }
        }

        private void Tint(Renderer target, bool on, float emission)
        {
            if (target == null)
            {
                return;
            }
            target.GetPropertyBlock(block);
            block.SetColor(ColorId, on ? accent : Dormant);
            block.SetColor(EmissionId, on ? accent * emission : Dormant * 0.1f);
            target.SetPropertyBlock(block);
        }

        private FoundryStationKind NextStep()
        {
            if (fragments < FragmentsNeeded)
            {
                return FoundryStationKind.StarCatcher;
            }
            return runePower < RuneNeeded ? FoundryStationKind.RuneWell : FoundryStationKind.AstralForge;
        }

        private void RefreshLabels()
        {
            FoundryStationKind next = NextStep();
            bool busy = forging || delivering;
            if (catcher != null && catcher.Label != null)
            {
                catcher.Label.text = Marker(next == FoundryStationKind.StarCatcher && !busy) + "STAR CATCHER\n" +
                    "FOCUS " + focusCharges + "/" + MaxCharges + "  •  FRAGMENTS " + fragments + "/" + FragmentsNeeded + "\n" +
                    (IsOpen ? "CLICK: FOCUS  •  1 CRYSTAL" : "SEALED");
            }
            if (well != null && well.Label != null)
            {
                well.Label.text = Marker(next == FoundryStationKind.RuneWell && !busy) + "RUNE WELL\n" +
                    "RUNE POWER " + Mathf.RoundToInt(runePower) + "/100\nCLICK: POUR  •  1 ESSENCE";
            }
            if (forge != null && forge.Label != null)
            {
                int blooms = manager != null ? manager.GetResource(WorkshopResourceKind.ConservatoryBloom) : 0;
                string line = forging ? "FORGING..." : delivering ? "CORE CROSSING THE BRIDGE..." : "CLICK: FORGE ASTRAL CORE";
                forge.Label.text = Marker(next == FoundryStationKind.AstralForge && !busy) + "ASTRAL FORGE\n" +
                    "FRAGMENTS " + fragments + "/" + FragmentsNeeded + "  •  RUNE " + Mathf.RoundToInt(runePower) + "/" +
                    Mathf.RoundToInt(RuneNeeded) + "  •  BLOOM " + blooms + "/1\n" + line + "\nCORES FORGED " + coresForged;
            }
        }

        private static string Marker(bool next)
        {
            return next ? ">> " : string.Empty;
        }

        private void ResolveManager()
        {
            if (manager == null)
            {
                manager = FindAnyObjectByType<ArcaneGameManager>();
            }
        }
    }
}
