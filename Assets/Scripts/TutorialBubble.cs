using UnityEngine;

namespace ArcaneWorkshop
{
    // A floating scroll that appears beside the object a new mechanic belongs to.
    // Point at it and press (click / trigger) to dismiss it early.
    public sealed class TutorialBubble : MonoBehaviour
    {
        private TextMesh text;
        private Transform background;
        private BoxCollider hitBox;

        public static TutorialBubble Create(Material backgroundMaterial)
        {
            GameObject root = new GameObject("Tutorial Bubble");
            TutorialBubble bubble = root.AddComponent<TutorialBubble>();

            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Quad);
            panel.name = "Scroll";
            Destroy(panel.GetComponent<Collider>());
            panel.transform.SetParent(root.transform, false);
            panel.transform.localPosition = new Vector3(0f, 0f, 0.02f);
            panel.GetComponent<Renderer>().sharedMaterial = backgroundMaterial;
            bubble.background = panel.transform;

            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(root.transform, false);
            TextMesh mesh = textObject.AddComponent<TextMesh>();
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Left;
            mesh.fontSize = 48;
            mesh.characterSize = 0.032f;
            mesh.color = new Color(1f, 0.95f, 0.8f);
            WorldText.Style(mesh);
            bubble.text = mesh;

            bubble.hitBox = root.AddComponent<BoxCollider>();
            // Show() sets the scroll's own scale, so only billboard it.
            root.AddComponent<WorldLabelBillboard>().ScaleWithDistance = false;
            root.SetActive(false);
            return bubble;
        }

        public void Show(string header, string message, Vector3 anchor, float scale)
        {
            text.text = "<b>" + header + "</b>\n" + ArcaneGameManager.Wrap(message, 40) + "\n<i>(point + click to dismiss)</i>";
            // High enough to clear the building's own stat label.
            transform.position = anchor + Vector3.up * 3.1f * scale;
            transform.localScale = Vector3.one * scale;
            gameObject.SetActive(true);

            Bounds bounds = text.GetComponent<MeshRenderer>().localBounds;
            Vector3 size = new Vector3(bounds.size.x + 0.3f, bounds.size.y + 0.25f, 1f);
            background.localScale = size;
            background.localPosition = new Vector3(bounds.center.x, bounds.center.y, 0.02f);
            hitBox.size = new Vector3(size.x, size.y, 0.1f);
            hitBox.center = background.localPosition;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
