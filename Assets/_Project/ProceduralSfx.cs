using System;
using UnityEngine;

namespace HeroManagement
{
    // Simple synthesized sounds, so the game has audio before you have real sound files
    public static class ProceduralSfx
    {
        private const int Rate = 22050;

        private static AudioClip Make(string name, float seconds, Func<float, float> sample)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(seconds * Rate));
            var data = new float[count];
            for (int i = 0; i < count; i++) data[i] = Mathf.Clamp(sample(i / (float)Rate), -1f, 1f);

            var clip = AudioClip.Create(name, count, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Tone(float t, float freq, float decay)
        {
            return Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-decay * t);
        }

        public static AudioClip Click()
        {
            return Make("sfx_click", 0.07f, t => 0.35f * Tone(t, 900f, 45f));
        }

        public static AudioClip Coin()
        {
            return Make("sfx_coin", 0.35f, t => t < 0.09f
                ? 0.3f * Tone(t, 1320f, 18f)
                : 0.3f * Tone(t - 0.09f, 1760f, 12f));
        }

        // A hammer on metal: a low thump, a high ring and a burst of noise
        public static AudioClip Hammer()
        {
            return Make("sfx_hammer", 0.4f, t =>
            {
                float noise = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-90f * t) * 0.4f;
                return 0.45f * Tone(t, 240f, 22f) + 0.25f * Tone(t, 1800f, 55f) + noise;
            });
        }

        // Short rising jingle
        public static AudioClip Success()
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            return Make("sfx_success", 0.7f, t =>
            {
                int index = Mathf.FloorToInt(t / 0.12f);
                if (index >= notes.Length) return 0.3f * Tone(t - 0.48f, notes[notes.Length - 1], 6f);
                return 0.3f * Tone(t - index * 0.12f, notes[index], 8f);
            });
        }

        // Short falling jingle
        public static AudioClip Failure()
        {
            float[] notes = { 440f, 349.23f, 293.66f };
            return Make("sfx_failure", 0.7f, t =>
            {
                int index = Mathf.Min(notes.Length - 1, Mathf.FloorToInt(t / 0.18f));
                return 0.3f * Tone(t - index * 0.18f, notes[index], 5f);
            });
        }

        // Crackling fireplace: soft filtered noise with random pops
        public static AudioClip FireLoop()
        {
            float lowPass = 0f;
            float pop = 0f;

            return Make("sfx_fire", 2f, t =>
            {
                float noise = UnityEngine.Random.value * 2f - 1f;
                lowPass += (noise - lowPass) * 0.12f;
                if (UnityEngine.Random.value < 0.0009f) pop = 1f;
                pop *= 0.985f;
                return lowPass * 0.25f + noise * pop * 0.35f;
            });
        }

        // A murmur of voices: slow-moving filtered noise
        public static AudioClip ChatterLoop()
        {
            float lowPass = 0f;

            return Make("sfx_chatter", 3f, t =>
            {
                float noise = UnityEngine.Random.value * 2f - 1f;
                lowPass += (noise - lowPass) * 0.08f;
                float envelope = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 3.3f * t) * Mathf.Sin(2f * Mathf.PI * 1.1f * t + 1f);
                return lowPass * envelope * 0.5f;
            });
        }
    }
}
