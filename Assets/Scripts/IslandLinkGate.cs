using UnityEngine;

namespace ArcaneWorkshop
{
    public enum IslandLinkState
    {
        Sealed,
        Awakened,
        Linked
    }

    /// <summary>
    /// Ring portal on Michael Island's rim where a teammate island's bridge docks.
    /// Sealed: a solid barrier you can see — the veil shimmers, motes swirl and the ring spins, so the blocked way
    /// reads as a force field. Awakened (progression): the field drops and the ring rests in its accent colour,
    /// so an open gate is a clear, effect-free passage; the bridge trade route opens. Linked: called by the
    /// integration scene once the teammate bridge exists; the safety barrier drops too.
    /// </summary>
    public sealed class IslandLinkGate : MonoBehaviour
    {
        [SerializeField] private string owner = "Lucas";
        [SerializeField] private string islandTitle = "Verdant Conservatory";
        [SerializeField] private int awakenAfterGoals = 6;
        [SerializeField] private WorkshopResourceKind awakenAfterDelivery = WorkshopResourceKind.Gold;
        [SerializeField] private bool requiresDelivery;
        [SerializeField] private Color accent = Color.green;
        [SerializeField] private Renderer[] glowRenderers;
        [SerializeField] private GameObject veil;
        [SerializeField] private Transform spinningRing;
        [SerializeField] private ParticleSystem swirl;
        [SerializeField] private Light gateLight;
        [SerializeField] private TextMesh label;
        [SerializeField] private Collider safetyBarrier;
        [SerializeField] private AudioSource hum;
        [SerializeField] private ArcaneGameManager manager;
        [SerializeField] private bool bridgeDocked;
        [SerializeField] private Collider sealBarrier;
        [SerializeField] private string awakenTutorial;
        [SerializeField] private Behaviour[] gatedSurfaces;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private IslandLinkState state = IslandLinkState.Sealed;
        private MaterialPropertyBlock block;
        private float checkTimer;
        private bool stateApplied;

        public string Owner { get { return owner; } }
        public IslandLinkState State { get { return state; } }
        public bool IsOpen { get { return state != IslandLinkState.Sealed; } }
        public bool BridgeDocked { get { return bridgeDocked; } }
        public Collider SealBarrier { get { return sealBarrier; } }

        /// <summary>
        /// A real teammate bridge docks at this gate. The optional solid seal blocks the walk until the gate awakens;
        /// the clickable barrier itself is a trigger.
        /// </summary>
        public void DockBridge(Collider seal, string tutorial, Behaviour[] surfaces = null)
        {
            bridgeDocked = true;
            sealBarrier = seal;
            awakenTutorial = tutorial;
            gatedSurfaces = surfaces;
            ApplyState(true);
        }

        public void Configure(ArcaneGameManager gameManager, string ownerName, string title, Color accentColor,
            int goalsToAwaken, bool needsDelivery, WorkshopResourceKind deliveryKind,
            Renderer[] glows, GameObject veilObject, Transform ring, ParticleSystem particles,
            Light lightSource, TextMesh textLabel, Collider barrier, AudioSource humSource)
        {
            manager = gameManager;
            owner = ownerName;
            islandTitle = title;
            accent = accentColor;
            awakenAfterGoals = goalsToAwaken;
            requiresDelivery = needsDelivery;
            awakenAfterDelivery = deliveryKind;
            glowRenderers = glows;
            veil = veilObject;
            spinningRing = ring;
            swirl = particles;
            gateLight = lightSource;
            label = textLabel;
            safetyBarrier = barrier;
            hum = humSource;
            ApplyState(true);
        }

        private void Start()
        {
            ResolveManager();
            ApplyState(true);
        }

        private void Update()
        {
            // The force-field effects play only while the gate is sealed.
            if (spinningRing != null && state == IslandLinkState.Sealed)
            {
                spinningRing.Rotate(Vector3.forward, 24f * Time.deltaTime, Space.Self);
            }
            if (gateLight != null && state == IslandLinkState.Sealed)
            {
                gateLight.intensity = 2.2f + Mathf.Sin(Time.time * 2.4f) * 0.35f;
            }

            checkTimer -= Time.deltaTime;
            if (checkTimer > 0f || state != IslandLinkState.Sealed)
            {
                return;
            }
            checkTimer = 0.5f;
            ResolveManager();
            if (manager != null && ConditionMet())
            {
                Awaken();
            }
        }

        public string RequirementText()
        {
            return requiresDelivery
                ? "receive your first " + WorkshopProgression.ResourceDisplayName(awakenAfterDelivery)
                : "complete Restoration Goal " + awakenAfterGoals;
        }

        public string InteractionHint()
        {
            switch (state)
            {
                case IslandLinkState.Linked:
                    return owner + "'s " + islandTitle + " is linked. Walk across the bridge.";
                case IslandLinkState.Awakened when bridgeDocked:
                    return owner + "'s bridge gate is awake. Walk across the bridge to the " + islandTitle + ".";
                case IslandLinkState.Awakened:
                    return owner + "'s bridge gate is awake. Trade crates now cross to the receiver beside it. " +
                        "The island itself docks here when the team scenes are merged.";
                default:
                    return owner + "'s bridge gate is sealed. To awaken it, " + RequirementText() + ".";
            }
        }

        public void Interact(ArcaneGameManager gameManager)
        {
            if (gameManager != null)
            {
                manager = gameManager;
            }
            if (manager != null)
            {
                manager.SetFeedback(InteractionHint());
            }
            ArcaneAudio.Play(state == IslandLinkState.Sealed ? SoundId.UiError : SoundId.UiSelect, transform.position);
        }

        /// <summary>Integration hook for MP2C: call once the teammate island and bridge exist in the scene.</summary>
        public void Link()
        {
            state = IslandLinkState.Linked;
            ApplyState(true);
        }

        private bool ConditionMet()
        {
            if (requiresDelivery)
            {
                return manager.TeammateDeliveries(awakenAfterDelivery) > 0;
            }
            return manager.CompletedGoalCount >= awakenAfterGoals;
        }

        private void Awaken()
        {
            state = IslandLinkState.Awakened;
            ApplyState(true);
            ArcaneAudio.Play(SoundId.GoalComplete, transform.position);
            CelebrationSpark.Burst(transform.position + Vector3.up * 2f, 14, manager.MoteMaterial);
            manager.SetFeedback("The bridge gate to " + owner + "'s " + islandTitle + " has awakened! " +
                "Order supplies at its receiver.");
            manager.ShowTutorial("gate-" + owner, !string.IsNullOrEmpty(awakenTutorial) ? awakenTutorial : "NEW - " + owner + "'s bridge gate: a border crossing to " + owner + "'s " + islandTitle +
                ". The receiver beside it is the customs dock: pay the listed ingredients and a crate crosses the gate to you.",
                transform.position + Vector3.up * 1.5f);
        }

        private void ResolveManager()
        {
            if (manager == null)
            {
                manager = FindAnyObjectByType<ArcaneGameManager>();
            }
        }

        private void ApplyState(bool force)
        {
            if (!force && stateApplied)
            {
                return;
            }
            stateApplied = true;
            bool open = state != IslandLinkState.Sealed;
            bool field = !open;
            Color color = accent;

            if (block == null)
            {
                block = new MaterialPropertyBlock();
            }
            if (glowRenderers != null)
            {
                for (int i = 0; i < glowRenderers.Length; i++)
                {
                    if (glowRenderers[i] == null)
                    {
                        continue;
                    }
                    glowRenderers[i].GetPropertyBlock(block);
                    block.SetColor(ColorId, color);
                    block.SetColor(EmissionId, field ? accent * 1.6f : accent * 0.35f);
                    glowRenderers[i].SetPropertyBlock(block);
                }
            }
            if (veil != null)
            {
                veil.SetActive(field);
            }
            if (swirl != null)
            {
                if (field && !swirl.isPlaying)
                {
                    swirl.Play();
                }
                else if (!field)
                {
                    swirl.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
            if (gateLight != null)
            {
                gateLight.color = color;
                gateLight.intensity = field ? 2.2f : 0.6f;
            }
            if (hum != null)
            {
                if (field && !hum.isPlaying && Application.isPlaying)
                {
                    hum.Play();
                }
                else if (!field)
                {
                    hum.Stop();
                }
            }
            if (safetyBarrier != null)
            {
                safetyBarrier.enabled = state != IslandLinkState.Linked;
            }
            if (sealBarrier != null)
            {
                foreach (Collider part in sealBarrier.GetComponentsInChildren<Collider>(true))
                {
                    part.enabled = state == IslandLinkState.Sealed;
                }
            }
            // Teleport surfaces beyond the gate (its bridge deck) open with the gate, so the seal cannot be skipped.
            if (gatedSurfaces != null)
            {
                foreach (Behaviour surface in gatedSurfaces)
                {
                    if (surface != null)
                    {
                        surface.enabled = open;
                    }
                }
            }
            if (label != null)
            {
                string status = state == IslandLinkState.Linked ? "LINKED"
                    : state == IslandLinkState.Awakened ? (bridgeDocked ? "AWAKENED  •  BRIDGE OPEN" : "AWAKENED  •  TRADE ROUTE OPEN")
                    : "SEALED  •  " + RequirementText().ToUpperInvariant();
                label.text = owner.ToUpperInvariant() + "'S " + islandTitle.ToUpperInvariant() + "\n" + status;
                label.color = open ? Color.Lerp(accent, Color.white, 0.35f) : new Color(0.7f, 0.76f, 0.9f);
            }
        }
    }
}
