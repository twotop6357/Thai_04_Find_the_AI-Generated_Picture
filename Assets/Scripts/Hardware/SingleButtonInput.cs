using System;
using UnityEngine;

namespace Hardware
{
    /// <summary>One persistent gameplay button shared by every scene, read from the Input Manager's NextButton axis.</summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class SingleButtonInput : MonoBehaviour
    {
        public const string ButtonName = "NextButton";

        public static SingleButtonInput Instance { get; private set; }
        public static bool IsSuppressed => Instance == null || !Instance.isActiveAndEnabled ||
            !Instance.focused || Instance.waitForRelease;
        public static bool IsPressed => Instance != null && Instance.held;

        private bool held;
        private bool waitForRelease = true;
        private bool focused = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var root = new GameObject(nameof(SingleButtonInput));
            DontDestroyOnLoad(root);
            root.AddComponent<SingleButtonInput>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            try { Input.GetButton(ButtonName); }
            catch (ArgumentException)
            {
                // GetButton throws every frame for an unknown axis; report once and leave input suppressed.
                Debug.LogError($"[SingleButtonInput] Input Manager axis '{ButtonName}' is missing. Button input is disabled.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (Instance == this) SuppressUntilRelease();
        }

        private void Update()
        {
            if (!focused) { SuppressUntilRelease(); return; }
            bool pressed = Input.GetButton(ButtonName);
            if (waitForRelease)
            {
                // A button already held when input resumes must be released before it counts.
                if (!pressed) waitForRelease = false;
                return;
            }
            held = pressed;
        }

        private void SuppressUntilRelease()
        {
            held = false;
            waitForRelease = true;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            focused = hasFocus;
            SuppressUntilRelease();
        }

        private void OnDisable()
        {
            if (Instance == this) SuppressUntilRelease();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
