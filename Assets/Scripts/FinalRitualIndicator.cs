using UnityEngine;

namespace ArcaneWorkshop
{
    // Floating Nexus Core above the ritual islet between the three islands. No walkable floor, no teleport
    // target, no inventory. It mirrors Final Ritual progress; once ready, triggering it (or Michael's
    // Convergence Altar) begins the ritual, and the Core answers by resonating.
    public sealed class FinalRitualIndicator : MonoBehaviour
    {
        private enum SignalState { Dormant, Ready, Resonating }

        [SerializeField] private ArcaneGameManager manager;
        [SerializeField] private Transform crystalRoot;
        [SerializeField] private TextMesh statusLabel;
        [SerializeField] private Renderer stateRenderer;
        [SerializeField] private Light stateLight;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly Color DormantColor = new Color(0.32f, 0.42f, 0.72f);
        private static readonly Color ReadyColor = new Color(0.95f, 0.72f, 0.2f);
        private static readonly Color ResonatingColor = new Color(1f, 0.93f, 0.7f);

        private MaterialPropertyBlock propertyBlock;
        private Vector3 baseLocalPosition;
        private SignalState shownState;
        private bool hasShownState;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            if (crystalRoot != null)
            {
                baseLocalPosition = crystalRoot.localPosition;
            }
        }

        private void Start()
        {
            ResolveManager();
            RefreshState(true);
        }

        private void Update()
        {
            SignalState state = CurrentState();
            if (crystalRoot != null)
            {
                float spin = state == SignalState.Dormant ? 28f : 75f;
                float bob = state == SignalState.Dormant ? 0.12f : 0.22f;
                crystalRoot.Rotate(Vector3.up, spin * Time.deltaTime, Space.Self);
                crystalRoot.localPosition = baseLocalPosition + Vector3.up * (Mathf.Sin(Time.time * 1.7f) * bob);
            }
            if (stateLight != null && state != SignalState.Dormant)
            {
                float baseIntensity = state == SignalState.Ready ? 2.2f : 3.2f;
                stateLight.intensity = baseIntensity + Mathf.Sin(Time.time * 3f) * 0.5f;
            }
            RefreshState(false);
        }

        public void Configure(ArcaneGameManager gameManager, Transform crystal, TextMesh label, Renderer renderer, Light light)
        {
            manager = gameManager;
            crystalRoot = crystal;
            statusLabel = label;
            stateRenderer = renderer;
            stateLight = light;
            baseLocalPosition = crystalRoot != null ? crystalRoot.localPosition : Vector3.zero;
            RefreshState(true);
        }

        public string InteractionHint()
        {
            ResolveManager();
            switch (CurrentState())
            {
                case SignalState.Resonating:
                    return "The Nexus Core resonates. Michael Island's Convergence Ritual is complete.";
                case SignalState.Ready:
                    return "Nexus Core aligned. Trigger the Core or the Convergence Altar to begin the Final Ritual (25 Gold + 2 Essence).";
                default:
                    return "Nexus Core dormant. " + manager.GoalSummary().Replace("\n", " ");
            }
        }

        public void Interact(ArcaneGameManager gameManager)
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

            SignalState state = CurrentState();
            if (state == SignalState.Ready)
            {
                // Players naturally aim at the Core itself, so it starts the ritual too (same cost and checks).
                manager.TryPerformFinalRitual();
                return;
            }
            manager.SetFeedback(InteractionHint());
            ArcaneAudio.Play2D(state == SignalState.Dormant ? SoundId.UiError : SoundId.UiSelect);
        }

        private void ResolveManager()
        {
            if (manager == null)
            {
                manager = FindAnyObjectByType<ArcaneGameManager>();
            }
        }

        private SignalState CurrentState()
        {
            if (manager == null)
            {
                return SignalState.Dormant;
            }
            if (manager.WorkshopRestored)
            {
                return SignalState.Resonating;
            }
            return manager.FinalRitualReady ? SignalState.Ready : SignalState.Dormant;
        }

        // TextMesh and property-block updates rebuild render data, so only push them on a state change.
        private void RefreshState(bool force)
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
            SignalState state = CurrentState();
            if (!force && hasShownState && state == shownState)
            {
                return;
            }
            shownState = state;
            hasShownState = true;

            Color color;
            string status;
            Color labelColor;
            switch (state)
            {
                case SignalState.Resonating:
                    color = ResonatingColor;
                    status = "RESONATING";
                    labelColor = new Color(1f, 0.95f, 0.75f);
                    break;
                case SignalState.Ready:
                    color = ReadyColor;
                    status = "READY";
                    labelColor = new Color(1f, 0.82f, 0.35f);
                    break;
                default:
                    color = DormantColor;
                    status = "DORMANT";
                    labelColor = new Color(0.65f, 0.76f, 1f);
                    break;
            }

            if (statusLabel != null)
            {
                statusLabel.text = "CENTRAL NEXUS CORE\nFINAL RITUAL SIGNAL  •  " + status;
                statusLabel.color = labelColor;
            }
            if (stateRenderer != null)
            {
                stateRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(ColorId, color);
                stateRenderer.SetPropertyBlock(propertyBlock);
            }
            if (stateLight != null)
            {
                stateLight.color = color;
                stateLight.intensity = state == SignalState.Dormant ? 0.65f : 2.2f;
            }
        }
    }
}
