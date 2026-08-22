using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FamilyForceUnity.Combat
{
    public sealed class CombatFeedback : MonoBehaviour
    {
        private static CombatFeedback instance;
        private AudioSource source;
        private AudioClip lightClip;
        private AudioClip heavyClip;

        private void Awake()
        {
            instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.volume = 0.32f;
            lightClip = CreateClick("Light Hit", 130f, 0.055f);
            heavyClip = CreateClick("Heavy Hit", 82f, 0.09f);
        }

        public static void PlayHit(bool heavy)
        {
            if (instance == null) return;
            instance.source.PlayOneShot(heavy ? instance.heavyClip : instance.lightClip);
            instance.StartCoroutine(instance.Rumble(heavy));
        }

        private IEnumerator Rumble(bool heavy)
        {
            float strength = heavy ? 0.48f : 0.22f;
            foreach (Gamepad pad in Gamepad.all) pad.SetMotorSpeeds(strength, strength * 1.25f);
            yield return new WaitForSecondsRealtime(heavy ? 0.11f : 0.055f);
            foreach (Gamepad pad in Gamepad.all) pad.SetMotorSpeeds(0f, 0f);
        }

        private static AudioClip CreateClick(string label, float frequency, float duration)
        {
            const int sampleRate = 22050;
            int count = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float fade = 1f - i / (float)count;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * fade * 0.5f;
            }
            AudioClip clip = AudioClip.Create(label, count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
