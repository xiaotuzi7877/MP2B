using UnityEngine;

public class CropHealth : MonoBehaviour
{
    [SerializeField] private Renderer[] plantRenderers;
    [SerializeField, Min(0f)] private float droughtGrace = 10f;
    [SerializeField, Min(0f)] private float droughtDamageRate = 5f;
    [SerializeField, Min(0f)] private float recoveryRate = 10f;
    [SerializeField] private Color wiltedColor = new Color(0.85f, 0.65f, 0.08f);
    [SerializeField, Range(0f, 100f)] private float health = 100f;

    private FarmCell cell;
    private SoilMoisture soil;
    private float dryTime;
    private Color[] healthyColors;
    private MaterialPropertyBlock properties;
    private static readonly int BaseColor = Shader.PropertyToID("_Color");

    public float Health => health;

    private void Awake()
    {
        properties = new MaterialPropertyBlock();
        healthyColors = new Color[plantRenderers.Length];
        for (int i = 0; i < plantRenderers.Length; i++)
            healthyColors[i] = plantRenderers[i].sharedMaterial.GetColor(BaseColor);
        RefreshColor();
    }

    public void Initialize(FarmCell plantedCell, SoilMoisture plantedSoil)
    {
        cell = plantedCell;
        soil = plantedSoil;
    }

    private void Update()
    {
        float previousHealth = health;
        if (soil.Moisture > 0f)
        {
            dryTime = 0f;
            health = Mathf.Min(100f, health + recoveryRate * Time.deltaTime);
        }
        else
        {
            float damageSeconds = Mathf.Max(0f, dryTime + Time.deltaTime - droughtGrace);
            dryTime = Mathf.Min(droughtGrace, dryTime + Time.deltaTime);
            health = Mathf.Max(0f, health - droughtDamageRate * damageSeconds);
        }

        if (health != previousHealth)
            RefreshColor();
        if (health <= 0f)
            cell.CropDied(this);
    }

    private void RefreshColor()
    {
        for (int i = 0; i < plantRenderers.Length; i++)
        {
            plantRenderers[i].GetPropertyBlock(properties);
            properties.SetColor(BaseColor, Color.Lerp(wiltedColor, healthyColors[i], health / 100f));
            plantRenderers[i].SetPropertyBlock(properties);
        }
    }
}
