using UnityEngine;

namespace ArcaneWorkshop
{
    // VR comfort overlays on the head camera. The vignette darkens peripheral vision while the
    // stick moves the player, which is where artificial motion causes the most sickness; the blink
    // covers teleports and snap turns with a brief fade so the view never visibly jumps.
    public sealed class ComfortVignette : MonoBehaviour
    {
        private const float Distance = 0.3f;
        private const float QuadSize = 2.4f;
        // Clear out to roughly 30 degrees from the view center, fully dark past roughly 55 degrees.
        private const float ClearRadius = 0.072f;
        private const float DarkRadius = 0.18f;
        private const float MaxVignette = 0.9f;
        private const float BlinkFadeSeconds = 0.18f;

        private Material vignetteMaterial;
        private Material blinkMaterial;
        private Renderer vignetteRenderer;
        private Renderer blinkRenderer;
        private float vignette;
        private float vignetteTarget;
        private float blink;

        public static ComfortVignette Create(Transform head)
        {
            GameObject root = new GameObject("Comfort Vignette");
            root.transform.SetParent(head, false);
            ComfortVignette comfort = root.AddComponent<ComfortVignette>();

            Shader shader = Shader.Find("Sprites/Default");
            comfort.vignetteMaterial = new Material(shader) { mainTexture = CreateVignetteTexture(), renderQueue = 4000 };
            comfort.blinkMaterial = new Material(shader) { mainTexture = Texture2D.whiteTexture, renderQueue = 4001 };
            comfort.vignetteRenderer = CreateQuad("Tunnel", root.transform, comfort.vignetteMaterial);
            comfort.blinkRenderer = CreateQuad("Blink", root.transform, comfort.blinkMaterial);
            comfort.Apply();
            return comfort;
        }

        // Called every frame with how strongly the player is being moved artificially (0..1).
        public void SetMotion(float amount)
        {
            vignetteTarget = Mathf.Clamp01(amount) * MaxVignette;
        }

        public void Blink()
        {
            blink = 1f;
            Apply();
        }

        private void LateUpdate()
        {
            // Close the tunnel quickly when motion starts, open it a little slower when it stops.
            float rate = vignetteTarget > vignette ? 6f : 3f;
            vignette = Mathf.MoveTowards(vignette, vignetteTarget, rate * Time.deltaTime);
            blink = Mathf.MoveTowards(blink, 0f, Time.deltaTime / BlinkFadeSeconds);
            Apply();
        }

        private void Apply()
        {
            vignetteRenderer.enabled = vignette > 0.001f;
            vignetteMaterial.color = new Color(0f, 0f, 0f, vignette);
            blinkRenderer.enabled = blink > 0.001f;
            blinkMaterial.color = new Color(0f, 0f, 0f, blink);
        }

        private static Renderer CreateQuad(string name, Transform parent, Material material)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = Vector3.forward * Distance;
            quad.transform.localScale = new Vector3(QuadSize, QuadSize, 1f);
            Renderer renderer = quad.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        private static Texture2D CreateVignetteTexture()
        {
            const int size = 256;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size - 0.5f;
                    float dy = (y + 0.5f) / size - 0.5f;
                    float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(ClearRadius, DarkRadius, Mathf.Sqrt(dx * dx + dy * dy)));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }
    }
}
