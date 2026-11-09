using UnityEngine;

namespace Core
{
    /// <summary>
    /// UI sounds. Transition sounds are requested by ScreenTransition only after it acquires the transition;
    /// LanguageChange and CardSelected are requested directly by the feature that accepted the input.
    /// </summary>
    public enum TransitionSound
    {
        None,
        Button,
        ReturnHome,
        LanguageChange,
        CardSelected,
    }

    /// <summary>
    /// Global manager for assigning UI sounds in the Inspector. As a child of the GameLifetimeScope prefab it persists across scenes,
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

        [Header("When a long press switches the language on Title")]
        [SerializeField] private AudioClip languageClip;
        [SerializeField, Range(0f, 1f)] private float languageVolume = 1f;

        [Header("When the game accepts a registered card on a reader")]
        [SerializeField] private AudioClip selectedClip;
        [SerializeField, Range(0f, 1f)] private float selectedVolume = 1f;

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
            if (languageClip == null) Debug.LogWarning("[TransitionSoundManager] Language clip is not assigned.", this);
            if (selectedClip == null) Debug.LogWarning("[TransitionSoundManager] Selected clip is not assigned.", this);
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
                case TransitionSound.LanguageChange:
                    _instance.PlayClip(_instance.languageClip, _instance.languageVolume);
                    break;
                case TransitionSound.CardSelected:
                    _instance.PlayClip(_instance.selectedClip, _instance.selectedVolume);
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
