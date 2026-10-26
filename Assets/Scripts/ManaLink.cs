using UnityEngine;

namespace ArcaneWorkshop
{
    // A glowing arc between two neighboring machines. The chevron texture scrolls from the
    // giving machine toward the receiving machine so the player can read the flow direction.
    [RequireComponent(typeof(LineRenderer))]
    public sealed class ManaLink : MonoBehaviour
    {
        private const int Segments = 12;
        private const float ArcHeight = 0.38f;
        private const float ScrollSpeed = 1.6f;

        private static Texture2D chevronTexture;

        private LineRenderer line;
        private Material materialInstance;
        private Color baseColor;
        private float offset;

        public static ManaLink Create(Transform parent, Material sourceMaterial)
        {
            GameObject linkObject = new GameObject("Mana Link");
            linkObject.transform.SetParent(parent, false);
            LineRenderer line = linkObject.AddComponent<LineRenderer>();
            line.positionCount = Segments + 1;
            line.useWorldSpace = true;
            line.textureMode = LineTextureMode.Tile;
            line.widthMultiplier = 0.16f;
            line.numCapVertices = 4;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = new Material(sourceMaterial);
            line.sharedMaterial.mainTexture = GetChevronTexture();
            return linkObject.AddComponent<ManaLink>();
        }

        private void Awake()
        {
            line = GetComponent<LineRenderer>();
        }

        public void Connect(Vector3 from, Vector3 to, Color color)
        {
            if (materialInstance == null)
            {
                materialInstance = line.sharedMaterial;
            }

            baseColor = color;
            for (int i = 0; i <= Segments; i++)
            {
                float t = i / (float)Segments;
                Vector3 point = Vector3.Lerp(from, to, t);
                point.y += Mathf.Sin(t * Mathf.PI) * ArcHeight;
                line.SetPosition(i, point);
            }

            float length = Vector3.Distance(from, to);
            materialInstance.mainTextureScale = new Vector2(Mathf.Max(1f, length * 3f), 1f);
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (materialInstance == null)
            {
                return;
            }

            // Solar-coaster style UV scroll: offset grows over time along the strip.
            offset -= ScrollSpeed * Time.deltaTime;
            materialInstance.mainTextureOffset = new Vector2(offset, 0f);

            float pulse = 0.75f + Mathf.Sin(Time.time * 5f) * 0.25f;
            Color color = baseColor * pulse;
            color.a = 1f;
            line.startColor = color;
            line.endColor = color;
        }

        private static Texture2D GetChevronTexture()
        {
            if (chevronTexture != null)
            {
                return chevronTexture;
            }

            const int width = 64;
            const int height = 16;
            chevronTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            chevronTexture.name = "Mana Link Chevrons";
            chevronTexture.wrapMode = TextureWrapMode.Repeat;
            chevronTexture.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < height; y++)
            {
                float v = Mathf.Abs(y / (float)(height - 1) - 0.5f) * 2f;
                for (int x = 0; x < width; x++)
                {
                    // Chevron pointing toward +U, i.e. toward the end of the line.
                    float chevron = Mathf.Repeat(x / (float)width + v * 0.35f, 1f);
                    float head = Mathf.SmoothStep(0f, 1f, chevron * 2.5f) * (1f - Mathf.SmoothStep(0.35f, 0.55f, chevron));
                    float edgeFade = 1f - v * v;
                    float alpha = Mathf.Clamp01(0.25f + head) * edgeFade;
                    chevronTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            chevronTexture.Apply();
            return chevronTexture;
        }
    }
}
