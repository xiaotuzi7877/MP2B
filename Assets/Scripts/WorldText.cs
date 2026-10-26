using UnityEngine;

namespace ArcaneWorkshop
{
    // Gives runtime-created TextMesh labels the depth-tested World Text material, so in-world text
    // is hidden by walls and panels like any other object.
    public static class WorldText
    {
        public static Font Font;
        public static Material Material;

        public static void Style(TextMesh mesh)
        {
            if (mesh == null || Font == null || Material == null)
            {
                return;
            }
            mesh.font = Font;
            mesh.GetComponent<MeshRenderer>().sharedMaterial = Material;
        }
    }
}
