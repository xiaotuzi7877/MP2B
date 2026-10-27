using UnityEngine;

public class CropGrowth : MonoBehaviour
{
    [SerializeField] private GameObject smallStage;
    [SerializeField] private GameObject mediumStage;
    [SerializeField] private GameObject matureStage;
    [SerializeField] private GameObject fertilizerMarker;
    [SerializeField] private float growthRate = 100f / 30f;
    [SerializeField, Range(0f, 100f)] private float growth;
    [SerializeField, Range(0f, 100f)] private float fertilizerBoost = 25f;

    private SoilMoisture soil;
    private int visibleStage = -1;
    public float Growth => growth;
    public bool IsMature => growth >= 100f;
    public bool IsFertilized { get; private set; }

    public void Fertilize()
    {
        IsFertilized = true;
        fertilizerMarker.SetActive(true);
        growth = Mathf.Min(100f, growth + fertilizerBoost);
        RefreshStage();
    }

    public void Initialize(SoilMoisture plantedSoil)
    {
        soil = plantedSoil;
        RefreshStage();
    }

    private void Update()
    {
        if (IsMature || soil.Moisture <= 0f)
            return;

        growth = Mathf.Min(100f, growth + growthRate * Time.deltaTime);
        RefreshStage();
    }

    private void RefreshStage()
    {
        int stage = growth >= 100f ? 2 : growth >= 40f ? 1 : 0;
        if (stage == visibleStage)
            return;

        smallStage.SetActive(stage == 0);
        mediumStage.SetActive(stage == 1);
        matureStage.SetActive(stage == 2);
        visibleStage = stage;
    }
}
