using System.Collections.Generic;
using UnityEngine;

namespace REPOPingMod.PingSystem
{
    public static class PingAudio
    {
        private static readonly Dictionary<PingType, AudioClip> _clips = new();
        private static AudioSource _audioSource;

        public static void Initialize(GameObject parent)
        {
            _audioSource = parent.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f; // 2D audio

            _clips[PingType.GoHere] = GenerateTone(440f, 0.15f, ToneShape.Sine);
            _clips[PingType.Danger] = GenerateDoubleTone(880f, 0.1f, 0.05f);
            _clips[PingType.Enemy] = GenerateTone(220f, 0.25f, ToneShape.Square);
            _clips[PingType.Loot] = GenerateRisingTone(523f, 784f, 0.2f);
        }

        public static void Play(PingType type)
        {
            if (!Plugin.PingConfig.SoundEnabled.Value) return;
            if (!_clips.TryGetValue(type, out var clip)) return;

            _audioSource.volume = Plugin.PingConfig.SoundVolume.Value;
            _audioSource.PlayOneShot(clip);
        }

        private enum ToneShape { Sine, Square }

        private static AudioClip GenerateTone(float frequency, float duration, ToneShape shape)
        {
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - ((float)i / sampleCount);
                float wave = shape == ToneShape.Sine
                    ? Mathf.Sin(2f * Mathf.PI * frequency * t)
                    : (Mathf.Sin(2f * Mathf.PI * frequency * t) >= 0f ? 1f : -1f);
                samples[i] = wave * envelope * 0.5f;
            }

            var clip = AudioClip.Create($"ping_{shape}_{frequency}", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip GenerateDoubleTone(float frequency, float toneDuration, float gapDuration)
        {
            int sampleRate = 44100;
            int toneSamples = (int)(sampleRate * toneDuration);
            int gapSamples = (int)(sampleRate * gapDuration);
            int totalSamples = toneSamples * 2 + gapSamples;
            float[] samples = new float[totalSamples];

            for (int i = 0; i < toneSamples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - ((float)i / toneSamples);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.5f;
            }

            int secondStart = toneSamples + gapSamples;
            for (int i = 0; i < toneSamples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - ((float)i / toneSamples);
                samples[secondStart + i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.5f;
            }

            var clip = AudioClip.Create("ping_double", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip GenerateRisingTone(float startFreq, float endFreq, float duration)
        {
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float progress = (float)i / sampleCount;
                float freq = Mathf.Lerp(startFreq, endFreq, progress);
                float envelope = 1f - (progress * 0.5f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.5f;
            }

            var clip = AudioClip.Create("ping_rising", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
