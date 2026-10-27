using UnityEngine;

namespace ArcaneWorkshop
{
    public sealed class TeammateDeliveryReceiver : MonoBehaviour
    {
        [SerializeField] private WorkshopResourceKind acceptedResource;
        [SerializeField] private string expectedSource;
        [SerializeField] private Transform payloadAnchor;
        [SerializeField] private TextMesh statusLabel;
        [SerializeField] private ArcaneGameManager manager;

        public WorkshopResourceKind AcceptedResource { get { return acceptedResource; } }
        public string ExpectedSource { get { return expectedSource; } }
        public Transform PayloadAnchor { get { return payloadAnchor; } }

        private void Start()
        {
            ResolveManager();
            RefreshLabel();
        }

        private void Update()
        {
            RefreshLabel();
        }

        public void Configure(
            WorkshopResourceKind resource,
            string source,
            Transform modelAnchor,
            TextMesh label,
            ArcaneGameManager gameManager = null)
        {
            acceptedResource = resource;
            expectedSource = source;
            payloadAnchor = modelAnchor;
            statusLabel = label;
            manager = gameManager;
            RefreshLabel();
        }

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

        public string InteractionHint()
        {
            ResolveManager();
            int stored = manager != null ? manager.GetResource(acceptedResource) : 0;
            return expectedSource + " delivery receiver online: accepts " +
                   WorkshopProgression.ResourceDisplayName(acceptedResource) + ". Shared inventory: " + stored + ".";
        }

        public void Interact(ArcaneGameManager gameManager)
        {
            if (gameManager != null)
            {
                manager = gameManager;
                manager.SetFeedback(InteractionHint());
            }
            ArcaneAudio.Play2D(SoundId.UiSelect);
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
            statusLabel.text = expectedSource.ToUpperInvariant() + " RECEIVER\n" +
                WorkshopProgression.ResourceDisplayName(acceptedResource).ToUpperInvariant() + "  •  STORED " + stored;
        }
    }
}
