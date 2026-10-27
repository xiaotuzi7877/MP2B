using UnityEngine;

public class KitDisplay : MonoBehaviour
{
    [SerializeField] private FarmResources resources;
    [SerializeField] private TextMesh countText;
    private void OnEnable() { resources.Changed += Refresh; Refresh(); }
    private void OnDisable() => resources.Changed -= Refresh;
    private void Refresh()
    {
        countText.text = $"Build: {resources.GetAmount(FarmResource.SprinklerKit)}    Pick: {resources.GetAmount(FarmResource.PickaxeKit)}\n"
            + $"Tank: {resources.GetAmount(FarmResource.TankKit)}    Soil: {resources.GetAmount(FarmResource.SoilKit)}";
    }
}
