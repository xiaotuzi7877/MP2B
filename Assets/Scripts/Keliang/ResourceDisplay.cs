using UnityEngine;

public class ResourceDisplay : MonoBehaviour
{
    [SerializeField] private FarmResources resources;
    [SerializeField] private TextMesh countText;

    private void OnEnable()
    {
        resources.Changed += Refresh;
        Refresh();
    }

    private void OnDisable() => resources.Changed -= Refresh;
    private void Refresh()
    {
        countText.text = $"Moon Herbs: {resources.Crops}    Seeds: {resources.GetAmount(FarmResource.Seeds)}\n"
            + $"Coins: {resources.GetAmount(FarmResource.Coins)}    Fertilizer: {resources.GetAmount(FarmResource.Fertilizer)}\n"
            + $"Stones: {resources.GetAmount(FarmResource.Stones)}";
    }
}
