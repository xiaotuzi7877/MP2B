using UnityEngine;

namespace ArcaneWorkshop
{
    [RequireComponent(typeof(Renderer))]
    public sealed class ArcaneFlowAnimator : MonoBehaviour
    {
        [SerializeField] private Vector2 scrollSpeed = new Vector2(0.15f, 0.42f);
        [SerializeField] private Color pulseColor = new Color(0.25f, 0.95f, 1f, 1f);

        private Material materialInstance;
        private int textureProperty;
        private Vector2 offset;

        private void Awake()
        {
            Renderer targetRenderer = GetComponent<Renderer>();
            materialInstance = targetRenderer.material;
            textureProperty = materialInstance.HasProperty("_BaseMap") ? Shader.PropertyToID("_BaseMap") : Shader.PropertyToID("_MainTex");
        }

        private void Update()
        {
            if (materialInstance == null)
            {
                return;
            }

            offset += scrollSpeed * Time.deltaTime;
            materialInstance.SetTextureOffset(textureProperty, offset);

            float pulse = 0.72f + Mathf.Sin(Time.time * 3.4f) * 0.18f;
            Color animatedColor = Color.Lerp(pulseColor * 0.45f, pulseColor, pulse);
            if (materialInstance.HasProperty("_BaseColor"))
            {
                materialInstance.SetColor("_BaseColor", animatedColor);
            }
            else if (materialInstance.HasProperty("_Color"))
            {
                materialInstance.SetColor("_Color", animatedColor);
            }
        }
    }
}
