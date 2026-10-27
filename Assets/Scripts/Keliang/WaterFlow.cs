using UnityEngine;

public class WaterFlow : MonoBehaviour
{
    [SerializeField] private Renderer[] streams;
    [SerializeField] private float speed = 2f;
    private MaterialPropertyBlock properties;
    private static readonly int BaseMapST = Shader.PropertyToID("_MainTex_ST");
    private void Awake() => properties = new MaterialPropertyBlock();
    private void Update()
    {
        foreach (var stream in streams)
        {
            if (!stream.enabled) continue;
            stream.GetPropertyBlock(properties);
            properties.SetVector(BaseMapST, new Vector4(4f, 1f, -Time.time * speed, 0f));
            stream.SetPropertyBlock(properties);
        }
    }
}
