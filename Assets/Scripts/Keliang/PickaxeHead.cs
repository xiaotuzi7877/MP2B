using UnityEngine;

public class PickaxeHead : MonoBehaviour
{
    [SerializeField] private PickaxeTool pickaxe;
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Boulder>(out var boulder))
            pickaxe.Strike(boulder);
    }
}
