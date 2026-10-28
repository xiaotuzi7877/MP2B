using UnityEngine;

namespace ArcaneWorkshop
{
    /// <summary>
    /// Bridgehead receiver for one teammate resource. Two ways to fill it:
    /// - <see cref="Receive"/>: the integration API the teammate island calls when its product crosses the bridge;
    /// - Interact (trade order): once the bridge gate is awake, pay the listed ingredients and a supply crate
    ///   flies in from the teammate's side of the gate, so the island is fully playable on its own.
    /// </summary>
    public sealed class TeammateDeliveryReceiver : MonoBehaviour
    {
        private const float ShipmentSeconds = 3.2f;

        [SerializeField] private WorkshopResourceKind acceptedResource;
        [SerializeField] private string expectedSource;
        [SerializeField] private Transform payloadAnchor;
        [SerializeField] private TextMesh statusLabel;
        [SerializeField] private ArcaneGameManager manager;
        [SerializeField] private IslandLinkGate gate;
        [SerializeField] private Transform shipmentOrigin;
        [SerializeField] private Transform shipmentCrate;
        [SerializeField] private Transform beacon;
        [SerializeField] private WorkshopResourceAmount[] orderCost;
        [SerializeField] private string orderRedirect;

        private bool shipping;
        private float shipTimer;
        private float labelTimer;

        public WorkshopResourceKind AcceptedResource { get { return acceptedResource; } }
        public string ExpectedSource { get { return expectedSource; } }
        public Transform PayloadAnchor { get { return payloadAnchor; } }
        public bool Shipping { get { return shipping; } }

        public void Configure(
            WorkshopResourceKind resource,
            string source,
            Transform modelAnchor,
            TextMesh label,
            ArcaneGameManager gameManager,
            IslandLinkGate linkGate,
            Transform origin,
            Transform crate,
            Transform beaconTransform,
            WorkshopResourceAmount[] cost)
        {
            acceptedResource = resource;
            expectedSource = source;
            payloadAnchor = modelAnchor;
            statusLabel = label;
            manager = gameManager;
            gate = linkGate;
            shipmentOrigin = origin;
            shipmentCrate = crate;
            beacon = beaconTransform;
            orderCost = cost;
            RefreshLabel();
        }

        private void Start()
        {
            ResolveManager();
            if (shipmentCrate != null)
            {
                shipmentCrate.gameObject.SetActive(false);
            }
            RefreshLabel();
        }

        private void Update()
        {
            if (beacon != null)
            {
                beacon.Rotate(Vector3.up, (shipping ? 140f : 35f) * Time.deltaTime, Space.World);
            }
            if (shipping)
            {
                AdvanceShipment();
            }
            labelTimer -= Time.deltaTime;
            if (labelTimer <= 0f)
            {
                labelTimer = 0.4f;
                RefreshLabel();
            }
        }

        /// <summary>Integration API: a teammate island delivers its product into Michael's shared inventory.</summary>
        public bool Receive(int amount, string sourceMember, GameObject deliveredModel = null)
        {
            ResolveManager();
            if (manager == null || amount <= 0)
            {
                return false;
            }
            if (!string.IsNullOrWhiteSpace(expectedSource) && !string.IsNullOrWhiteSpace(sourceMember) &&
                !sourceMember.Contains(expectedSource))
            {
                manager.SetFeedback("This receiver expects a delivery from " + expectedSource + ".");
                return false;
            }

            if (deliveredModel != null && payloadAnchor != null)
            {
                deliveredModel.transform.SetParent(payloadAnchor, false);
                deliveredModel.transform.localPosition = Vector3.zero;
                deliveredModel.transform.localRotation = Quaternion.identity;
            }

            bool accepted = manager.RegisterTeammateOutput(acceptedResource, amount, sourceMember);
            RefreshLabel();
            return accepted;
        }

        /// <summary>The teammate island makes this product itself: clicks explain where instead of placing an order.</summary>
        public void RedirectOrders(string message)
        {
            orderRedirect = message;
            RefreshLabel();
        }

        public string CostLabel()
        {
            if (orderCost == null || orderCost.Length == 0)
            {
                return "free";
            }
            string text = string.Empty;
            for (int i = 0; i < orderCost.Length; i++)
            {
                if (i > 0)
                {
                    text += " + ";
                }
                text += orderCost[i].amount + " " + WorkshopProgression.ResourceDisplayName(orderCost[i].kind);
            }
            return text;
        }

        public string InteractionHint()
        {
            ResolveManager();
            string product = WorkshopProgression.ResourceDisplayName(acceptedResource);
            if (gate != null && !gate.IsOpen)
            {
                return expectedSource + "'s " + product + " receiver is waiting for its bridge gate. To awaken it, " +
                    gate.RequirementText() + ".";
            }
            if (shipping)
            {
                return "A " + product + " crate from " + expectedSource + "'s island is on its way.";
            }
            if (!string.IsNullOrEmpty(orderRedirect))
            {
                return orderRedirect;
            }
            return "Order 1 " + product + " from " + expectedSource + "'s island for " + CostLabel() + ".";
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
            if ((gate != null && !gate.IsOpen) || shipping || !string.IsNullOrEmpty(orderRedirect))
            {
                manager.SetFeedback(InteractionHint());
                ArcaneAudio.Play(shipping || (gate != null && gate.IsOpen) ? SoundId.UiSelect : SoundId.UiError, transform.position);
                return;
            }
            if (orderCost != null && orderCost.Length > 0 && !manager.TrySpendResources(orderCost))
            {
                manager.SetFeedback("Not enough ingredients. Ordering a " +
                    WorkshopProgression.ResourceDisplayName(acceptedResource) + " costs " + CostLabel() + ".");
                ArcaneAudio.Play(SoundId.UiError, transform.position);
                return;
            }

            shipping = true;
            shipTimer = 0f;
            if (shipmentCrate != null)
            {
                shipmentCrate.gameObject.SetActive(true);
                shipmentCrate.position = ShipmentStart();
            }
            ArcaneAudio.Play(SoundId.PotionSend, transform.position);
            manager.SetFeedback("Order sent across the bridge gate to " + expectedSource + ". The crate arrives in a moment.");
            RefreshLabel();
        }

        private Vector3 ShipmentStart()
        {
            return shipmentOrigin != null ? shipmentOrigin.position : transform.position + transform.forward * -6f + Vector3.up * 2f;
        }

        private void AdvanceShipment()
        {
            shipTimer += Time.deltaTime;
            float t = Mathf.Clamp01(shipTimer / ShipmentSeconds);
            if (shipmentCrate != null && payloadAnchor != null)
            {
                float eased = t * t * (3f - 2f * t);
                Vector3 position = Vector3.Lerp(ShipmentStart(), payloadAnchor.position + Vector3.up * 0.35f, eased);
                position.y += Mathf.Sin(t * Mathf.PI) * 1.4f;
                shipmentCrate.position = position;
                shipmentCrate.Rotate(Vector3.up, 160f * Time.deltaTime, Space.World);
            }
            if (t < 1f)
            {
                return;
            }

            shipping = false;
            if (shipmentCrate != null)
            {
                shipmentCrate.gameObject.SetActive(false);
            }
            Vector3 landing = payloadAnchor != null ? payloadAnchor.position : transform.position;
            CelebrationSpark.Burst(landing + Vector3.up * 0.4f, 10, manager != null ? manager.MoteMaterial : null);
            ArcaneAudio.Play(SoundId.CoinSale, landing);
            Receive(1, expectedSource + "'s island");
        }

        private void ResolveManager()
        {
            if (manager == null)
            {
                manager = FindAnyObjectByType<ArcaneGameManager>();
            }
        }

        private void RefreshLabel()
        {
            if (statusLabel == null)
            {
                return;
            }
            int stored = manager != null ? manager.GetResource(acceptedResource) : 0;
            string product = WorkshopProgression.ResourceDisplayName(acceptedResource).ToUpperInvariant();
            string line = gate != null && !gate.IsOpen ? "AWAITING BRIDGE GATE"
                : shipping ? "CRATE INBOUND..."
                : !string.IsNullOrEmpty(orderRedirect) ? "FORGED ACROSS THE BRIDGE"
                : "CLICK TO ORDER  •  " + CostLabel();
            statusLabel.text = expectedSource.ToUpperInvariant() + " SUPPLY RECEIVER\n" + product + "  •  STORED " + stored + "\n" + line;
        }
    }
}
