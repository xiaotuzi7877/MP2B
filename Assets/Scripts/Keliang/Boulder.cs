using UnityEngine;

public class Boulder : MonoBehaviour
{
    [SerializeField] private GameObject stonePrefab;
    [SerializeField] private GameObject firstCrack;
    [SerializeField] private GameObject secondCrack;
    [SerializeField] private TextMesh hardnessText;
    [SerializeField] private float hardness = 3f;
    private FarmResources resources;
    public float Hardness => hardness;
    public void Initialize(FarmResources inventory) => resources = inventory;

    public bool Hit(float damage)
    {
        if (hardness <= 0f) return false;
        hardness = Mathf.Max(0f, hardness - damage);
        hardnessText.text = $"Hardness: {hardness:0.#} / 3";
        firstCrack.SetActive(hardness <= 2f);
        secondCrack.SetActive(hardness <= 1f);
        if (hardness > 0f) return true;
        for (int i = 0; i < 3; i++)
        {
            var stone = Instantiate(stonePrefab, transform.parent);
            stone.name = "stone" + (i + 1);
            float angle = i * Mathf.PI * 2f / 3f;
            stone.transform.position = transform.position + new Vector3(Mathf.Cos(angle) * .4f, 0f, Mathf.Sin(angle) * .4f);
            stone.GetComponent<StonePickup>().Initialize(resources);
        }
        gameObject.SetActive(false);
        Destroy(gameObject);
        return true;
    }
}
