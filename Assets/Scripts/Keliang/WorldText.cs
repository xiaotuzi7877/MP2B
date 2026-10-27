using UnityEngine;

[RequireComponent(typeof(TextMesh))]
public class WorldText : MonoBehaviour
{
    [SerializeField] private Shader textShader;
    private TextMesh textMesh;
    private Material textMaterial;

    private void Awake()
    {
        textMesh = GetComponent<TextMesh>();
        textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textMaterial = new Material(textMesh.font.material) { shader = textShader };
        GetComponent<MeshRenderer>().sharedMaterial = textMaterial;
    }

    private void OnEnable() => Font.textureRebuilt += RefreshTexture;
    private void OnDisable() => Font.textureRebuilt -= RefreshTexture;
    private void OnDestroy() => Destroy(textMaterial);

    private void RefreshTexture(Font font)
    {
        if (font == textMesh.font)
            textMaterial.mainTexture = font.material.mainTexture;
    }
}
