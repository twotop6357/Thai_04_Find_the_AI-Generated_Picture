using UnityEngine;

namespace Core
{
    /// <summary>Sound played when a screen transition actually starts. ScreenTransition requests it only after acquiring the transition.</summary>
    public enum TransitionSound
    {
        None,
        Button,
        ReturnHome,
    }

    /// <summary>
    /// Global manager for assigning transition sounds in the Inspector. As a child of the GameLifetimeScope prefab it persists across scenes,
    /// so sounds are not cut off by a scene unload. Leaving a clip empty silences only that sound.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TransitionSoundManager : MonoBehaviour
    {
        [Header("When a button press changes the screen")]
        [SerializeField] private AudioClip buttonClip;
        [SerializeField, Range(0f, 1f)] private float buttonVolume = 1f;

        [Header("When returning to the first screen")]
        [SerializeField] private AudioClip returnHomeClip;
        [SerializeField, Range(0f, 1f)] private float returnHomeVolume = 1f;

        private static TransitionSoundManager _instance;
        private AudioSource source;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _instance = null;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            if (buttonClip == null) Debug.LogWarning("[TransitionSoundManager] Button clip is not assigned.", this);
            if (returnHomeClip == null) Debug.LogWarning("[TransitionSoundManager] Return home clip is not assigned.", this);
        }

        public static void Play(TransitionSound sound)
        {
            if (_instance == null || _instance.source == null) return;
            switch (sound)
            {
                case TransitionSound.Button:
                    _instance.PlayClip(_instance.buttonClip, _instance.buttonVolume);
                    break;
                case TransitionSound.ReturnHome:
                    _instance.PlayClip(_instance.returnHomeClip, _instance.returnHomeVolume);
                    break;
            }
        }

        private void PlayClip(AudioClip clip, float volume)
        {
            if (clip != null) source.PlayOneShot(clip, volume);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
