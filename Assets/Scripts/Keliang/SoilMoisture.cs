using UnityEngine;

public class SoilMoisture : MonoBehaviour
{
    [SerializeField] private Renderer soilRenderer;
    [SerializeField] private float dryingRate = 2f;
    [SerializeField] private Color dryColor = new Color(0.24f, 0.105f, 0.045f);
    [SerializeField] private Color wetColor = new Color(0.075f, 0.035f, 0.015f);
    [SerializeField, Range(0f, 100f)] private float moisture;
    [SerializeField] private GameObject upgradedVisual;
    public bool IsUpgraded { get; private set; }

    public void Upgrade()
    {
        IsUpgraded = true;
        dryingRate *= .5f;
        upgradedVisual.SetActive(true);
    }

    private MaterialPropertyBlock properties;
    private static readonly int BaseColor = Shader.PropertyToID("_Color");
    public float Moisture => moisture;

    private void Awake()
    {
        properties = new MaterialPropertyBlock();
        RefreshColor();
    }

    private void Update()
    {
        if (moisture <= 0f)
            return;

        moisture = Mathf.Max(0f, moisture - dryingRate * Time.deltaTime);
        RefreshColor();
    }

    public void AddWater(float amount)
    {
        moisture = Mathf.Clamp(moisture + amount, 0f, 100f);
        RefreshColor();
    }

    private void RefreshColor()
    {
        properties.SetColor(BaseColor, Color.Lerp(dryColor, wetColor, moisture / 100f));
        soilRenderer.SetPropertyBlock(properties);
    }
}
