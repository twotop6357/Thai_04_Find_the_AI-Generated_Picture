using UnityEngine;

namespace Core
{
    /// <summary>화면 전환이 실제로 시작될 때 재생할 효과음. ScreenTransition이 전환을 획득한 뒤에만 요청한다.</summary>
    public enum TransitionSound
    {
        None,
        Button,
        ReturnHome,
    }

    /// <summary>
    /// 전환 효과음을 인스펙터에서 할당하는 전역 매니저. GameLifetimeScope 프리팹의 자식이라 씬이 바뀌어도 유지되며,
    /// 씬 언로드 중에도 효과음이 끊기지 않는다. 클립을 비워 두면 해당 효과음만 재생되지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TransitionSoundManager : MonoBehaviour
    {
        [Header("버튼 입력으로 화면이 전환될 때")]
        [SerializeField] private AudioClip buttonClip;
        [SerializeField, Range(0f, 1f)] private float buttonVolume = 1f;

        [Header("최초 화면으로 돌아갈 때")]
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
