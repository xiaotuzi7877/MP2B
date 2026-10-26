using UnityEngine;

namespace ArcaneWorkshop
{
    public sealed class GatherableNode : MonoBehaviour
    {
        [SerializeField] private NodeKind kind;
        [SerializeField] private int remainingMaterial = 1;
        [SerializeField] private float vitality = 100f;

        private Vector3 basePosition;
        private Vector3 baseScale;
        private TextMesh label;
        private float labelTimer;

        public NodeKind Kind { get { return kind; } }

        private void Awake()
        {
            basePosition = transform.position;
            baseScale = transform.localScale;
            label = GetComponentInChildren<TextMesh>(true);
            RefreshLabel();
        }

        private void Update()
        {
            float phase = basePosition.x * 1.31f + basePosition.z * 0.77f;
            transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * 1.8f + phase) * 0.08f);
            transform.Rotate(Vector3.up, 18f * Time.deltaTime, Space.World);

            if (kind == NodeKind.MoonHerb)
            {
                // Wild herbs slowly wilt; an unharvested herb shrinks and finally withers away.
                vitality = Mathf.Max(0f, vitality - Time.deltaTime * 0.6f);
                transform.localScale = baseScale * Mathf.Lerp(0.45f, 1f, vitality / 100f);
                labelTimer -= Time.deltaTime;
                if (labelTimer <= 0f)
                {
                    labelTimer = 0.5f;
                    RefreshLabel();
                }
                if (vitality <= 0f)
                {
                    Destroy(gameObject);
                }
            }
        }

        public void Configure(NodeKind newKind)
        {
            kind = newKind;
            remainingMaterial = newKind == NodeKind.CrystalCluster ? 3 : 1;
            vitality = 100f;
        }

        public void Interact(ArcaneGameManager manager)
        {
            if (manager == null)
            {
                return;
            }

            if (kind == NodeKind.CrystalCluster)
            {
                vitality = 0f;
                ArcaneAudio.Play(SoundId.CrystalShatter, transform.position);
                manager.ShatterCrystal(transform.position);
                manager.SetFeedback("The crystal cluster shattered into harvestable shards.");
                manager.ShowTutorial("shatter", "Shatter Affordance: a Crystal Cluster is replaced by smaller Crystal Shards that can be harvested.", transform.position);
                Destroy(gameObject);
                return;
            }

            remainingMaterial--;
            vitality = Mathf.Max(0f, vitality - 100f);

            if (kind == NodeKind.MoonHerb)
            {
                manager.AddHerbs(1);
                ArcaneAudio.Play(SoundId.HerbPluck, transform.position);
                manager.SetFeedback("Gathered 1 Moon Herb.");
            }
            else
            {
                manager.AddCrystals(1);
                ArcaneAudio.Play(SoundId.ShardPick, transform.position);
                manager.SetFeedback("Gathered 1 Crystal Shard.");
            }

            manager.ShowTutorial("wild-harvest", "Harvesting: click wild materials to reduce their Remaining Material stat and increase a player Resource.", transform.position);
            RefreshLabel();

            if (remainingMaterial <= 0 || vitality <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void RefreshLabel()
        {
            if (label == null)
            {
                return;
            }

            string title = kind == NodeKind.MoonHerb ? "MOON HERB" : kind == NodeKind.CrystalCluster ? "CRYSTAL CLUSTER" : "CRYSTAL SHARD";
            label.text = title + "\nMaterial " + remainingMaterial + (kind == NodeKind.MoonHerb ? "\nVitality " + Mathf.RoundToInt(vitality) : "");
        }
    }
}
