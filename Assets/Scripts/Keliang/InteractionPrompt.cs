using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class InteractionPrompt : MonoBehaviour
{
    [SerializeField] private XRBaseInteractable interactable;
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private Text promptText;
    [SerializeField, TextArea] private string hoverMessage;
    [SerializeField, TextArea] private string heldMessage;
    [SerializeField] private float height = .3f;
    private Transform viewer;

    public void SetMessages(string message) { hoverMessage = message; heldMessage = message; }

    private void Start()
    {
        // Spawned tool/crop prefabs cannot serialize a scene camera reference.
        var camera = Camera.main;
        if (camera != null) viewer = camera.transform;
    }

    private void LateUpdate()
    {
        bool visible = viewer != null && (interactable.isHovered || interactable.isSelected);
        if (promptRoot.activeSelf != visible) promptRoot.SetActive(visible);
        if (!visible) return;

        string message = interactable.isSelected ? heldMessage : hoverMessage;
        if (promptText.text != message) promptText.text = message;
        var panel = promptRoot.transform;
        panel.position = transform.position + Vector3.up * height;
        panel.rotation = viewer.rotation;
        // Keep the panel readable on differently scaled or rotated tool prefabs.
        panel.localScale = Vector3.one;
        var scale = panel.lossyScale;
        panel.localScale = new Vector3(.001f / Mathf.Abs(scale.x), .001f / Mathf.Abs(scale.y), .001f / Mathf.Abs(scale.z));
    }

    private void OnDisable() => promptRoot.SetActive(false);
}
