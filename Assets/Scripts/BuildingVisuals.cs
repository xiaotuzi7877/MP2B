using UnityEngine;

namespace ArcaneWorkshop
{
    // Maps a building's Stats onto its model so changes are readable without the text label:
    // foliage grows, soil darkens when wet, vessels glow with Mana, the brew rises and heats,
    // and the amplifier's rune ring and blast wave speed up with Charge and Resonance.
    [RequireComponent(typeof(ArcaneBuilding))]
    public sealed class BuildingVisuals : MonoBehaviour
    {
        [SerializeField] private Transform growthTarget;
        [SerializeField] private Transform bloom;
        [SerializeField] private Renderer tintTarget;
        [SerializeField] private Renderer glowTarget;
        [SerializeField] private Transform levelTarget;
        [SerializeField] private Light statLight;
        [SerializeField] private Transform spinTarget;
        [SerializeField] private Renderer blastRing;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private ArcaneBuilding building;
        private MaterialPropertyBlock block;
        private Vector3 growthScale;
        private Vector3 levelPosition;
        private Color baseTint;
        private Color baseGlow;
        private Color blastColor;

        public void Bind(Transform growth, Transform bloomObject, Renderer tint, Renderer glow, Transform level, Light light, Transform spin, Renderer blast)
        {
            growthTarget = growth;
            bloom = bloomObject;
            tintTarget = tint;
            glowTarget = glow;
            levelTarget = level;
            statLight = light;
            spinTarget = spin;
            blastRing = blast;
        }

        private void Awake()
        {
            building = GetComponent<ArcaneBuilding>();
            block = new MaterialPropertyBlock();
            if (growthTarget != null) growthScale = growthTarget.localScale;
            if (levelTarget != null) levelPosition = levelTarget.localPosition;
            if (tintTarget != null) baseTint = tintTarget.sharedMaterial.GetColor(ColorId);
            if (glowTarget != null && glowTarget.sharedMaterial.HasProperty(EmissionId)) baseGlow = glowTarget.sharedMaterial.GetColor(EmissionId);
            if (blastRing != null) blastColor = blastRing.sharedMaterial.GetColor(ColorId);
        }

        private void Update()
        {
            switch (building.Kind)
            {
                case BuildingKind.RuneFoundation:
                    SetGlow(glowTarget, Mathf.Lerp(0.35f, 1.2f, Mathf.InverseLerp(70f, 100f, building.Integrity)));
                    break;

                case BuildingKind.MoonPlanter:
                    float growth01 = building.Growth / 100f;
                    if (growthTarget != null)
                    {
                        float sway = 1f + Mathf.Sin(Time.time * 2.2f) * 0.03f;
                        growthTarget.localScale = growthScale * Mathf.Lerp(0.5f, 1.15f, growth01) * sway;
                    }
                    if (bloom != null)
                    {
                        bloom.gameObject.SetActive(growth01 >= 1f);
                    }
                    if (tintTarget != null)
                    {
                        // Dry soil is pale and dusty; wet soil is dark and rich.
                        Color dry = new Color(0.55f, 0.42f, 0.30f);
                        tintTarget.GetPropertyBlock(block);
                        block.SetColor(ColorId, Color.Lerp(dry, baseTint, building.Moisture / 100f));
                        tintTarget.SetPropertyBlock(block);
                    }
                    break;

                case BuildingKind.ManaStill:
                    float mana01 = building.Mana / 100f;
                    SetGlow(glowTarget, Mathf.Lerp(0.15f, 2.2f, mana01));
                    if (statLight != null) statLight.intensity = Mathf.Lerp(0.1f, 1.6f, mana01);
                    break;

                case BuildingKind.StarCauldron:
                    float heat01 = building.Heat / 100f;
                    if (levelTarget != null)
                    {
                        float rise = building.IsBrewing ? building.BrewProgress / 100f * 0.08f : 0f;
                        float bubble = Mathf.Sin(Time.time * (3f + heat01 * 9f)) * 0.006f * (0.3f + heat01);
                        levelTarget.localPosition = levelPosition + Vector3.up * (rise + bubble);
                    }
                    if (statLight != null)
                    {
                        statLight.intensity = Mathf.Lerp(0.3f, 2.6f, heat01) * (0.9f + Mathf.PerlinNoise(Time.time * 6f, 0f) * 0.2f);
                        statLight.color = Color.Lerp(new Color(0.4f, 0.8f, 1f), new Color(1f, 0.45f, 0.15f), heat01);
                    }
                    break;

                case BuildingKind.CrystalAmplifier:
                    float charge01 = building.Charge / 100f;
                    if (spinTarget != null)
                    {
                        spinTarget.Rotate(Vector3.up, Mathf.Lerp(15f, 160f, building.Resonance / 100f) * Time.deltaTime, Space.World);
                    }
                    SetGlow(glowTarget, Mathf.Lerp(0.2f, 1.8f, charge01));
                    if (blastRing != null)
                    {
                        blastRing.GetPropertyBlock(block);
                        Color c = blastColor;
                        c.a *= Mathf.Lerp(0.05f, 1f, charge01);
                        block.SetColor(ColorId, c);
                        blastRing.SetPropertyBlock(block);
                    }
                    if (statLight != null) statLight.intensity = Mathf.Lerp(0.1f, 1.4f, charge01);
                    break;
            }
        }

        private void SetGlow(Renderer target, float strength)
        {
            if (target == null)
            {
                return;
            }
            target.GetPropertyBlock(block);
            block.SetColor(EmissionId, baseGlow * strength);
            target.SetPropertyBlock(block);
        }
    }
}
