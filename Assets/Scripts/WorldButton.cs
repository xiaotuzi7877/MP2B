using System;
using UnityEngine;

namespace ArcaneWorkshop
{
    // A pressable rune tablet for world-space menus. Works with the desktop mouse ray and the
    // VR controller ray alike, because both go through ArcaneGameManager's pointer handling.
    public sealed class WorldButton : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private Action onClick;
        private TextMesh label;
        private Renderer background;
        private MaterialPropertyBlock block;
        private bool selected;
        private float hoverUntil;

        public static WorldButton Create(Transform parent, string text, Vector3 localPosition, Vector2 size, float characterSize, Material material, Action onClick)
        {
            GameObject tablet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tablet.name = "Button " + text.Split('\n')[0];
            tablet.transform.SetParent(parent, false);
            tablet.transform.localPosition = localPosition;
            tablet.transform.localScale = new Vector3(size.x, size.y, 0.04f);
            tablet.GetComponent<Renderer>().sharedMaterial = material;
            tablet.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            GameObject textObject = new GameObject("Label");
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = localPosition + new Vector3(-size.x * 0.46f, 0f, -0.03f);
            TextMesh mesh = textObject.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleLeft;
            mesh.fontSize = 48;
            mesh.characterSize = characterSize;
            mesh.color = new Color(0.9f, 0.96f, 1f);
            WorldText.Style(mesh);

            WorldButton button = tablet.AddComponent<WorldButton>();
            button.onClick = onClick;
            button.label = mesh;
            button.background = tablet.GetComponent<Renderer>();
            return button;
        }

        private void Awake()
        {
            block = new MaterialPropertyBlock();
        }

        public void SetText(string text)
        {
            if (label.text != text)
            {
                label.text = text;
            }
        }

        public void SetSelected(bool value)
        {
            selected = value;
        }

        public void Hover()
        {
            hoverUntil = Time.unscaledTime + 0.1f;
        }

        public void Press()
        {
            if (onClick != null)
            {
                onClick();
            }
        }

        private void LateUpdate()
        {
            bool hovered = Time.unscaledTime < hoverUntil;
            Color color = selected ? new Color(0.25f, 0.55f, 0.85f) : new Color(0.12f, 0.1f, 0.22f);
            if (hovered)
            {
                color = Color.Lerp(color, new Color(0.55f, 0.9f, 1f), 0.45f);
            }
            background.GetPropertyBlock(block);
            block.SetColor(ColorId, color);
            background.SetPropertyBlock(block);
        }
    }
}
