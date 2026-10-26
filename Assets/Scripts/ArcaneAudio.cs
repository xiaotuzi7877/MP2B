using System;
using UnityEngine;

namespace ArcaneWorkshop
{
    public enum SoundId
    {
        UiSelect,
        UiBack,
        UiError,
        TutorialPage,
        BuildFoundation,
        BuildMachine,
        HerbPluck,
        CrystalShatter,
        ShardPick,
        WaterDrop,
        PlanterHarvest,
        StillCollect,
        BrewStart,
        IngredientPlop,
        BrewComplete,
        SparkLaunch,
        SparkHit,
        StarWhoosh,
        StarImpact,
        BuildingCrumble,
        Repair,
        PotionSend,
        CoinSale,
        GoalComplete,
        Ending
    }

    public enum SoundCategory
    {
        Interface,
        Interaction,
        Impact,
        Music,
        Ambient
    }

    [Serializable]
    public struct SoundEntry
    {
        public SoundId id;
        public SoundCategory category;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume;
        public float pitchJitter;
        public float cooldown;
    }

    // Central sound player. Every sound is scaled by its own volume, its category volume and the
    // master volume, so loudness is balanced in one place instead of at each call site.
    public sealed class ArcaneAudio : MonoBehaviour
    {
        private const int VoiceCount = 12;

        [Header("Mix (0-1)")]
        [SerializeField] private float masterVolume = 0.8f;
        [SerializeField] private float interfaceVolume = 0.35f;
        [SerializeField] private float interactionVolume = 0.55f;
        [SerializeField] private float impactVolume = 0.6f;
        [SerializeField] private float musicVolume = 0.45f;
        [SerializeField] private float ambientVolume = 0.08f;

        [Header("Library")]
        [SerializeField] private SoundEntry[] sounds;

        private static ArcaneAudio instance;
        private AudioSource[] voices;
        private AudioSource ambient;
        private float[] lastPlayed;
        private int nextVoice;

        public void SetLibrary(SoundEntry[] library)
        {
            sounds = library;
        }

        private void Awake()
        {
            instance = this;
            voices = new AudioSource[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                AudioSource voice = gameObject.AddComponent<AudioSource>();
                voice.playOnAwake = false;
                voices[i] = voice;
            }

            lastPlayed = new float[Enum.GetValues(typeof(SoundId)).Length];
            for (int i = 0; i < lastPlayed.Length; i++)
            {
                lastPlayed[i] = -10f;
            }

            StartAmbient();
        }

        public static void Play(SoundId id, Vector3 position)
        {
            if (instance != null)
            {
                instance.PlayInternal(id, position, true);
            }
        }

        // Non-positional (menu clicks, jingles).
        public static void Play2D(SoundId id)
        {
            if (instance != null)
            {
                instance.PlayInternal(id, Vector3.zero, false);
            }
        }

        private void PlayInternal(SoundId id, Vector3 position, bool spatial)
        {
            if (sounds == null)
            {
                return;
            }

            for (int i = 0; i < sounds.Length; i++)
            {
                SoundEntry entry = sounds[i];
                if (entry.id != id || entry.clips == null || entry.clips.Length == 0)
                {
                    continue;
                }

                if (Time.unscaledTime - lastPlayed[(int)id] < entry.cooldown)
                {
                    return;
                }
                lastPlayed[(int)id] = Time.unscaledTime;

                AudioSource voice = voices[nextVoice];
                nextVoice = (nextVoice + 1) % voices.Length;
                voice.transform.position = position;
                voice.clip = entry.clips[UnityEngine.Random.Range(0, entry.clips.Length)];
                voice.volume = entry.volume * CategoryVolume(entry.category) * masterVolume;
                voice.pitch = 1f + UnityEngine.Random.Range(-entry.pitchJitter, entry.pitchJitter);

                // World sounds are partly spatial so they can be located without becoming inaudible
                // across the (large, 5x-scaled in VR) workshop.
                voice.spatialBlend = spatial ? 0.6f : 0f;
                voice.rolloffMode = AudioRolloffMode.Linear;
                voice.minDistance = 4f;
                voice.maxDistance = 45f;
                if (spatial)
                {
                    PlayAt(voice, position);
                }
                else
                {
                    voice.transform.localPosition = Vector3.zero;
                    voice.Play();
                }
                return;
            }
        }

        private void PlayAt(AudioSource voice, Vector3 position)
        {
            // AudioSources live on this object; spatial playback uses a temporary child placed in the world.
            GameObject emitter = new GameObject("Sound " + voice.clip.name);
            emitter.transform.position = position;
            AudioSource source = emitter.AddComponent<AudioSource>();
            source.clip = voice.clip;
            source.volume = voice.volume;
            source.pitch = voice.pitch;
            source.spatialBlend = voice.spatialBlend;
            source.rolloffMode = voice.rolloffMode;
            source.minDistance = voice.minDistance;
            source.maxDistance = voice.maxDistance;
            source.dopplerLevel = 0f;
            source.Play();
            Destroy(emitter, voice.clip.length / Mathf.Max(0.1f, source.pitch) + 0.1f);
        }

        private float CategoryVolume(SoundCategory category)
        {
            switch (category)
            {
                case SoundCategory.Interface: return interfaceVolume;
                case SoundCategory.Interaction: return interactionVolume;
                case SoundCategory.Impact: return impactVolume;
                case SoundCategory.Music: return musicVolume;
                default: return ambientVolume;
            }
        }

        // A soft, slowly breathing night drone synthesized at startup: no asset needed and very quiet.
        private void StartAmbient()
        {
            const int sampleRate = 22050;
            const float seconds = 12f;
            int length = Mathf.RoundToInt(sampleRate * seconds);
            float[] data = new float[length];
            float[] frequencies = { 110f, 164.81f, 220f, 277.18f };
            System.Random noise = new System.Random(7);
            float wind = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float sample = 0f;
                for (int f = 0; f < frequencies.Length; f++)
                {
                    // Whole cycles over the loop length keep the loop seamless.
                    float cycles = Mathf.Round(frequencies[f] * seconds);
                    float swell = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * (f + 1) * t / seconds);
                    sample += Mathf.Sin(2f * Mathf.PI * cycles * t / seconds) * swell / frequencies.Length;
                }
                wind = wind * 0.995f + ((float)noise.NextDouble() * 2f - 1f) * 0.005f;
                data[i] = sample * 0.5f + wind * 1.5f;
            }

            AudioClip clip = AudioClip.Create("Night Workshop Drone", length, 1, sampleRate, false);
            clip.SetData(data, 0);
            ambient = gameObject.AddComponent<AudioSource>();
            ambient.clip = clip;
            ambient.loop = true;
            ambient.spatialBlend = 0f;
            ambient.volume = ambientVolume * masterVolume;
            ambient.Play();
        }
    }
}
