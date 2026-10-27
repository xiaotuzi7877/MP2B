using UnityEngine;

namespace ArcaneWorkshop
{
    /// <summary>
    /// World-space answer to a pointer press. In the headset the feedback line otherwise lives only on the
    /// spellbook page, so a refused press (missing ingredients, locked ritual) looked like nothing happened.
    /// The toast appears on the line of sight toward what was pressed, faces the player, keeps a readable angular size,
    /// and fades after a few seconds.
    /// </summary>
    public sealed class FeedbackToast : MonoBehaviour
    {
        private const float HoldSeconds = 4f;
        private const float FadeSeconds = 0.6f;
        private const float AngularSize = 0.045f;

        private TextMesh text;
        private Transform backing;
        private Material backingMaterial;
        private float shownAt = -100f;
        private readonly Color textColor = new Color(1f, 0.95f, 0.8f);

        public static FeedbackToast Create(Material backingTemplate)
        {
            GameObject root = new GameObject("Pointer Feedback Toast");
            FeedbackToast toast = root.AddComponent<FeedbackToast>();

            GameObject label = new GameObject("Toast Text");
            label.transform.SetParent(root.transform, false);
            toast.text = label.AddComponent<TextMesh>();
            toast.text.anchor = TextAnchor.MiddleCenter;
            toast.text.alignment = TextAlignment.Center;
            toast.text.fontSize = 64;
            toast.text.characterSize = 0.1f;
            WorldText.Style(toast.text);

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Toast Backing";
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(root.transform, false);
            quad.transform.localPosition = new Vector3(0f, 0f, 0.02f);
            toast.backing = quad.transform;
            toast.backingMaterial = new Material(backingTemplate != null ? backingTemplate : new Material(Shader.Find("Sprites/Default")));
            quad.GetComponent<Renderer>().sharedMaterial = toast.backingMaterial;
            quad.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            root.SetActive(false);
            return toast;
        }

        public void Show(string message, Vector3 worldPoint)
        {
            if (text == null || string.IsNullOrEmpty(message))
            {
                return;
            }
            gameObject.SetActive(true);
            text.text = ArcaneGameManager.Wrap(message, 38);
            Vector3 position = worldPoint + Vector3.up * 0.45f;
            if (Camera.main != null)
            {
                // Sit on the line of sight between the player and the target, at most 1.5 m away,
                // so the message is never buried inside (or hidden behind) the object it describes.
                Vector3 eye = Camera.main.transform.position;
                Vector3 toTarget = worldPoint - eye;
                position = eye + toTarget.normalized * Mathf.Min(1.5f, toTarget.magnitude * 0.7f) + Vector3.up * 0.25f;
            }
            transform.position = position;
            shownAt = Time.unscaledTime;
            FitBacking();
            LateUpdate();
        }

        private void FitBacking()
        {
            string[] lines = text.text.Split('\n');
            int longest = 0;
            foreach (string line in lines)
            {
                longest = Mathf.Max(longest, line.Length);
            }
            // TextMesh: one line is roughly fontSize * characterSize / 10 world units tall before scaling.
            float lineHeight = text.fontSize * text.characterSize * 0.1f;
            backing.localScale = new Vector3(longest * lineHeight * 0.52f + lineHeight, lines.Length * lineHeight * 1.05f + lineHeight * 0.6f, 1f);
        }

        private void LateUpdate()
        {
            Camera view = Camera.main;
            float age = Time.unscaledTime - shownAt;
            if (age > HoldSeconds + FadeSeconds)
            {
                gameObject.SetActive(false);
                return;
            }
            if (view != null)
            {
                Vector3 toEye = view.transform.position - transform.position;
                float distance = Mathf.Max(0.5f, toEye.magnitude);
                transform.rotation = Quaternion.LookRotation(-toEye.normalized, Vector3.up);
                transform.localScale = Vector3.one * (distance * AngularSize);
            }
            float alpha = age < HoldSeconds ? 1f : 1f - (age - HoldSeconds) / FadeSeconds;
            text.color = new Color(textColor.r, textColor.g, textColor.b, alpha);
            if (backingMaterial != null)
            {
                backingMaterial.color = new Color(0.04f, 0.03f, 0.1f, 0.82f * alpha);
            }
        }

        private void OnDestroy()
        {
            if (backingMaterial != null)
            {
                Destroy(backingMaterial);
            }
        }
    }
}
