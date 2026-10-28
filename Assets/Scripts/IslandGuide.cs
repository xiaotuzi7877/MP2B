using System.Collections.Generic;
using UnityEngine;

namespace ArcaneWorkshop
{
    /// <summary>
    /// A teammate island that can explain itself on the spellbook's right page while the player stands on it
    /// (Lucas's farm, Justin's foundry), so the next step is always in the left hand instead of on a distant board.
    /// </summary>
    public interface IIslandGuide
    {
        bool Contains(Vector3 position);
        string GuideTitle { get; }
        string GuideText();
    }

    public static class IslandGuides
    {
        private static readonly List<IIslandGuide> Guides = new List<IIslandGuide>();

        public static void Register(IIslandGuide guide)
        {
            if (guide != null && !Guides.Contains(guide))
            {
                Guides.Add(guide);
            }
        }

        public static void Unregister(IIslandGuide guide)
        {
            Guides.Remove(guide);
        }

        public static IIslandGuide At(Vector3 position)
        {
            for (int i = 0; i < Guides.Count; i++)
            {
                if (Guides[i] != null && Guides[i].Contains(position))
                {
                    return Guides[i];
                }
            }
            return null;
        }
    }
}
