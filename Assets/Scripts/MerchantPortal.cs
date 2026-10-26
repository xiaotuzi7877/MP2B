using UnityEngine;

namespace ArcaneWorkshop
{
    // A traveling merchant's moon gate at the island edge. Potion bottles fly to it along a fixed
    // path; each delivery that collides with the gate pays Gold based on its Demand and Reputation.
    public sealed class MerchantPortal : MonoBehaviour
    {
        [SerializeField] private float demand = 70f;
        [SerializeField] private float reputation = 10f;
        [SerializeField] private Transform swirl;
        [SerializeField] private Vector3[] deliveryPath;

        private ArcaneGameManager manager;
        private TextMesh label;
        private float labelTimer;

        public Vector3[] DeliveryPath { get { return deliveryPath; } }

        public void Configure(Transform swirlTransform, Vector3[] path)
        {
            swirl = swirlTransform;
            deliveryPath = path;
        }

        public void Initialize(ArcaneGameManager newManager)
        {
            manager = newManager;
            label = GetComponentInChildren<TextMesh>(true);
            RefreshLabel();
        }

        private void Update()
        {
            // Demand recovers while the merchant waits; reputation slowly fades without deliveries.
            demand = Mathf.Min(100f, demand + Time.deltaTime * 1.1f);
            reputation = Mathf.Max(0f, reputation - Time.deltaTime * 0.03f);

            if (swirl != null)
            {
                swirl.Rotate(Vector3.up, Mathf.Lerp(20f, 140f, demand / 100f) * Time.deltaTime, Space.Self);
                float breathe = 1f + Mathf.Sin(Time.time * 2f) * 0.04f * (demand / 100f);
                swirl.localScale = new Vector3(breathe, breathe, breathe) * 0.85f;
            }

            labelTimer -= Time.deltaTime;
            if (labelTimer <= 0f)
            {
                labelTimer = 0.2f;
                RefreshLabel();
            }
        }

        public int PriceFor(int baseGold)
        {
            float demandFactor = Mathf.Lerp(0.5f, 1.25f, demand / 100f);
            float reputationFactor = 1f + reputation / 200f;
            return Mathf.Max(1, Mathf.RoundToInt(baseGold * demandFactor * reputationFactor));
        }

        private void OnTriggerEnter(Collider other)
        {
            ArcaneProjectile projectile = other.GetComponent<ArcaneProjectile>();
            if (projectile == null || projectile.Kind != ProjectileKind.Potion || projectile.Target != this)
            {
                return;
            }

            int payment = PriceFor(projectile.Payload);
            demand = Mathf.Max(0f, demand - 22f);
            reputation = Mathf.Min(100f, reputation + 12f);
            projectile.Consume();
            ArcaneAudio.Play(SoundId.CoinSale, transform.position);
            if (manager != null)
            {
                manager.ReceiveSale(payment, transform.position);
            }
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            if (label != null)
            {
                label.text = "MERCHANT GATE\nDemand " + Mathf.RoundToInt(demand) + "\nReputation " + Mathf.RoundToInt(reputation);
            }
        }
    }
}
