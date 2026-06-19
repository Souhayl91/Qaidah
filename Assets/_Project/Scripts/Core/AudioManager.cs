using UnityEngine;

namespace SimpleQaidah.Core
{
    public enum SFXType
    {
        Correct,
        Wrong,
        Tap,
        Star,
        Complete
    }

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource letterSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("SFX Clips")]
        [SerializeField] private AudioClip correctClip;
        [SerializeField] private AudioClip wrongClip;
        [SerializeField] private AudioClip tapClip;
        [SerializeField] private AudioClip starClip;
        [SerializeField] private AudioClip completeClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureAudioSources();
        }

        private void EnsureAudioSources()
        {
            if (letterSource == null)
            {
                letterSource = gameObject.AddComponent<AudioSource>();
                letterSource.playOnAwake = false;
            }
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
        }

        public void PlayLetterAudio(AudioClip clip)
        {
            if (clip == null) return;
            letterSource.Stop();
            letterSource.clip = clip;
            letterSource.Play();
        }

        public void PlaySFX(SFXType type)
        {
            AudioClip clip = type switch
            {
                SFXType.Correct => correctClip,
                SFXType.Wrong => wrongClip,
                SFXType.Tap => tapClip,
                SFXType.Star => starClip,
                SFXType.Complete => completeClip,
                _ => null
            };
            if (clip != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }

        public void StopAll()
        {
            letterSource.Stop();
            sfxSource.Stop();
        }

        public bool IsPlayingLetter => letterSource.isPlaying;
    }
}
