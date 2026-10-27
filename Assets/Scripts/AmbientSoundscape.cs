using UnityEngine;

namespace ArcaneWorkshop
{
    /// <summary>
    /// Night-time bed for the floating island, synthesised at startup so it needs no audio asset:
    /// soft filtered wind, a low drone chord, and sparse glassy chimes. The clip loops seamlessly
    /// (every component repeats exactly within the clip length).
    /// </summary>
    public sealed class AmbientSoundscape : MonoBehaviour
    {
        private const int SampleRate = 22050;
        private const float LengthSeconds = 12f;

        [SerializeField] private float volume = 0.16f;

        private AudioSource source;

        private void Start()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.clip = BuildClip();
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = volume;
            source.priority = 200;
            source.Play();
        }

        private static AudioClip BuildClip()
        {
            int count = Mathf.RoundToInt(SampleRate * LengthSeconds);
            float[] data = new float[count];
            System.Random random = new System.Random(417);

            // Wind: low-passed noise with a slow swell. One extra second is generated past the end and blended
            // into the start, so the sample after the last one is exactly the natural continuation (seamless loop).
            int fade = SampleRate;
            float[] raw = new float[count + fade];
            float lowA = 0f;
            float lowB = 0f;
            for (int i = 0; i < raw.Length; i++)
            {
                float white = (float)(random.NextDouble() * 2.0 - 1.0);
                lowA += (white - lowA) * 0.02f;
                lowB += (lowA - lowB) * 0.05f;
                raw[i] = lowB;
            }
            float[] wind = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i < fade ? i / (float)fade : 1f;
                wind[i] = i < fade ? raw[i] * t + raw[count + i] * (1f - t) : raw[i];
            }

            float[] drone = { 110f, 164.75f, 220f, 329.5f }; // whole cycles per loop: no click at the seam
            float[] droneGain = { 0.05f, 0.035f, 0.025f, 0.012f };
            float[] chimes = { 880f, 1174.66f, 1318.51f, 1760f };
            float[] chimeTimes = { 1.3f, 4.1f, 6.8f, 9.6f };

            for (int i = 0; i < count; i++)
            {
                float time = i / (float)SampleRate;
                float swell = 0.65f + 0.35f * Mathf.Sin(2f * Mathf.PI * time / LengthSeconds);
                float sample = wind[i] * 2.2f * swell;

                float tremolo = 0.8f + 0.2f * Mathf.Sin(2f * Mathf.PI * time * (2f / LengthSeconds));
                for (int d = 0; d < drone.Length; d++)
                {
                    sample += Mathf.Sin(2f * Mathf.PI * drone[d] * time) * droneGain[d] * tremolo;
                }

                for (int c = 0; c < chimes.Length; c++)
                {
                    float since = time - chimeTimes[c];
                    if (since < 0f)
                    {
                        since += LengthSeconds;
                    }
                    if (since < 2.5f)
                    {
                        float envelope = Mathf.Exp(-since * 2.2f) * Mathf.Clamp01(since * 200f);
                        sample += (Mathf.Sin(2f * Mathf.PI * chimes[c] * time) +
                                   0.4f * Mathf.Sin(2f * Mathf.PI * chimes[c] * 2.01f * time)) * 0.045f * envelope;
                    }
                }
                data[i] = Mathf.Clamp(sample, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("Island Night Ambience", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
