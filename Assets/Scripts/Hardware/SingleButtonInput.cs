using System;
using System.IO;
using Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Hardware
{
    /// <summary>One persistent gameplay binding shared by every scene. F8 opens the operator settings.</summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class SingleButtonInput : MonoBehaviour, IContentBlocker
    {
        public const string DefaultBindingPath = "<Keyboard>/1";
        private const string FileName = "thai04-button-binding-v1.json";

        [Serializable]
        private sealed class SavedBinding
        {
            public int version = 1;
            public string controlPath = DefaultBindingPath;
            public string displayName = "Keyboard / 1";
        }

        public static SingleButtonInput Instance { get; private set; }
        public static bool IsSettingsOpen => Instance != null && Instance.settingsOpen;
        public static bool IsSuppressed => Instance == null || !Instance.isActiveAndEnabled ||
            !Instance.focused || Instance.settingsOpen || Instance.waitForRelease || Instance.IsRebinding;
        public static bool IsPressed => Instance != null && Instance.held;
        public static bool WasPressedThisFrame => Instance != null && Instance.pressedFrame == Time.frameCount;
        public string StoragePath => Path.Combine(Application.persistentDataPath, FileName);
        public string BindingPath => saved.controlPath;
        public string BindingDisplayName => saved.displayName;
        public bool IsBindingConnected => button != null && button.controls.Count > 0;
        public bool IsRebinding => rebind != null;
        public string Status { get; private set; } = "Changes are saved automatically and kept after restart.";

        private InputAction button;
        private InputActionRebindingExtensions.RebindingOperation rebind;
        private SavedBinding saved = new SavedBinding();
        private bool settingsOpen;
        private bool held;
        private bool waitForRelease = true;
        private bool focused = true;
        private int pressedFrame = -1;
        private bool previousCursorVisible;
        private CursorLockMode previousCursorLock;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var root = new GameObject(nameof(SingleButtonInput));
            DontDestroyOnLoad(root);
            root.AddComponent<SingleButtonInput>();
            root.AddComponent<ButtonMappingPanel>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            button = new InputAction("Button1", InputActionType.Button, DefaultBindingPath, expectedControlType: "Button");
            LoadBinding();
        }

        private void OnEnable()
        {
            if (Instance != this) return;
            button.Enable();
            InputSystem.onDeviceChange += OnDeviceChange;
            SuppressUntilRelease();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f8Key.wasPressedThisFrame)
            {
                if (settingsOpen) CloseSettings();
                else OpenSettings();
            }
            else if (settingsOpen && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (IsRebinding) CancelRebinding();
                else CloseSettings();
            }

            if (!focused || settingsOpen || IsRebinding) { SuppressUntilRelease(); return; }
            if (waitForRelease)
            {
                // Inspect physical controls too: enabling an action must not accept an already held button.
                foreach (var control in button.controls)
                    if (control is ButtonControl key && key.isPressed) return;
                waitForRelease = false;
                return;
            }
            held = button.IsPressed();
            if (button.WasPressedThisFrame()) pressedFrame = Time.frameCount;
        }

        public void OpenSettings()
        {
            if (settingsOpen) return;
            settingsOpen = true;
            previousCursorVisible = Cursor.visible;
            previousCursorLock = Cursor.lockState;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SuppressUntilRelease();
            ContentGate.Register(this);
        }

        public void CloseSettings()
        {
            if (!settingsOpen) return;
            CancelRebinding();
            settingsOpen = false;
            ContentGate.Unregister(this);
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
            SuppressUntilRelease();
        }

        public bool RefreshBlocking() => settingsOpen;

        public void StartRebinding()
        {
            OpenSettings();
            if (IsRebinding) return;
            button.Disable();
            SuppressUntilRelease();
            Status = "Press the keyboard key or USB controller button to use. Esc cancels.";
            rebind = button.PerformInteractiveRebinding(0)
                // Esc is handled once in Update, rather than by both the operation and settings hotkeys.
                .WithCancelingThrough((string)null)
                .OnMatchWaitForAnother(0)
                .WithExpectedControlType<ButtonControl>()
                .WithControlsExcluding("<Pointer>")
                // AnyKey also actuates for Esc/F8; exclude it before event suppression and candidate scoring.
                .WithControlsExcluding("<Keyboard>/anyKey")
                .WithControlsExcluding("<Keyboard>/escape")
                .WithControlsExcluding("<Keyboard>/f8")
                .WithControlsExcluding("<Keyboard>/f9")
                .WithTimeout(20)
                .OnPotentialMatch(operation =>
                {
                    // Reject axis-derived synthetic buttons and devices unrelated to this installation.
                    for (int i = operation.candidates.Count - 1; i >= 0; i--)
                    {
                        var candidate = operation.candidates[i];
                        if (candidate.synthetic || !(candidate.device is Keyboard || candidate.device is Joystick || candidate.device is Gamepad))
                            operation.RemoveCandidate(candidate);
                    }
                    if (operation.candidates.Count > 0) operation.Complete();
                })
                .OnGeneratePath(control => "<" + control.device.layout + ">" + control.path.Substring(control.device.path.Length))
                .OnComplete(operation =>
                {
                    if (rebind != operation) return;
                    var control = operation.selectedControl;
                    if (control == null)
                    {
                        Status = "No button selected. The previous mapping is unchanged.";
                        FinishRebinding(operation);
                        return;
                    }
                    CommitBinding(new SavedBinding
                    {
                        controlPath = button.bindings[0].effectivePath,
                        displayName = control.device.displayName.Trim() + " / " + control.displayName
                    });
                    FinishRebinding(operation);
                })
                .OnCancel(operation =>
                {
                    if (rebind != operation) return;
                    Status = "Registration cancelled. The previous button is unchanged.";
                    FinishRebinding(operation);
                });
            rebind.Start();
        }

        public void CancelRebinding() => rebind?.Cancel();

        public void ResetBinding()
        {
            CancelRebinding();
            button.Disable();
            CommitBinding(new SavedBinding());
            button.Enable();
            SuppressUntilRelease();
        }

        private void FinishRebinding(InputActionRebindingExtensions.RebindingOperation operation)
        {
            rebind = null;
            operation.Dispose();
            button.Enable();
            SuppressUntilRelease();
        }

        private void CommitBinding(SavedBinding next)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(StoragePath));
                File.WriteAllText(StoragePath + ".tmp", JsonUtility.ToJson(next, true));
                if (File.Exists(StoragePath)) File.Replace(StoragePath + ".tmp", StoragePath, StoragePath + ".bak");
                else File.Move(StoragePath + ".tmp", StoragePath);
                saved = next;
                Status = "Saved. This button will be restored automatically on the next launch.";
                Debug.Log("[SingleButtonInput] Saved Button1 binding: " + saved.controlPath, this);
            }
            catch (Exception error)
            {
                Status = "Could not save the button. The previous mapping is still active. Try again.";
                Debug.LogError("[SingleButtonInput] Could not save Button1 binding: " + error.Message, this);
            }
            button.ApplyBindingOverride(0, saved.controlPath);
        }

        private void LoadBinding()
        {
            if (File.Exists(StoragePath) || File.Exists(StoragePath + ".bak"))
            {
                try { saved = ReadBinding(StoragePath); }
                catch (Exception error)
                {
                    try
                    {
                        saved = ReadBinding(StoragePath + ".bak");
                        Status = "Restored the saved backup button mapping.";
                        Debug.LogWarning("[SingleButtonInput] Restored Button1 binding from backup: " + error.Message, this);
                    }
                    catch (Exception backupError)
                    {
                        Status = "Could not read the saved mapping. Keyboard 1 is available; register your button again.";
                        Debug.LogWarning("[SingleButtonInput] Cannot read saved Button1 binding; using Keyboard 1. Files were preserved. " + backupError.Message, this);
                    }
                }
            }
            button.ApplyBindingOverride(0, saved.controlPath);
        }

        private static SavedBinding ReadBinding(string path)
        {
            var value = JsonUtility.FromJson<SavedBinding>(File.ReadAllText(path));
            if (value == null || value.version != 1 || string.IsNullOrEmpty(value.controlPath) ||
                !value.controlPath.StartsWith("<", StringComparison.Ordinal) || value.controlPath.IndexOf(">/", StringComparison.Ordinal) < 2 ||
                value.controlPath.EndsWith("/", StringComparison.Ordinal) || value.controlPath.Contains("*"))
                throw new InvalidDataException("Invalid Button1 binding data.");
            if (string.IsNullOrEmpty(value.displayName)) value.displayName = value.controlPath;
            return value;
        }

        private void SuppressUntilRelease()
        {
            held = false;
            pressedFrame = -1;
            waitForRelease = true;
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected ||
                change == InputDeviceChange.Reconnected || change == InputDeviceChange.Added || change == InputDeviceChange.Disabled)
                SuppressUntilRelease();
            // The saved layout/control path survives USB port changes and new runtime device IDs.
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            focused = hasFocus;
            if (!hasFocus) CancelRebinding();
            SuppressUntilRelease();
        }

        private void OnDisable()
        {
            if (Instance != this) return;
            CloseSettings();
            CancelRebinding();
            InputSystem.onDeviceChange -= OnDeviceChange;
            button?.Disable();
            SuppressUntilRelease();
        }

        private void OnDestroy()
        {
            button?.Dispose();
            if (Instance == this) Instance = null;
        }
    }
}
