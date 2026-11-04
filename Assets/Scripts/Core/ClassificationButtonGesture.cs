using Hardware;
using UnityEngine;

namespace Core
{
    /// <summary>Release advances; one uninterrupted five-second hold returns home without submitting.</summary>
    public sealed class ClassificationButtonGesture : MonoBehaviour
    {
        public const double HomeHoldSeconds = 5;
        [SerializeField] private ClassificationResultPanel results;
        private bool pressed;
        private bool physicalHeld;
        private bool waitForRelease;
        private double pressedAt;

        private bool Held => physicalHeld || SingleButtonInput.IsPressed;
        private void OnEnable() { pressed = false; waitForRelease = Held; }

        private void LateUpdate()
        {
            bool held = Held;
            if (SingleButtonInput.IsSuppressed || ContentGate.IsBlocked || ScreenTransition.IsBusy ||
                (results != null && results.IsTransitioning))
            {
                pressed = false;
                waitForRelease = held;
                return;
            }
            if (waitForRelease)
            {
                if (!held) waitForRelease = false;
                return;
            }
            if (held && !pressed) { pressed = true; pressedAt = Time.realtimeSinceStartupAsDouble; }
            if (!pressed) return;
            if (Time.realtimeSinceStartupAsDouble - pressedAt >= HomeHoldSeconds)
            {
                pressed = false;
                waitForRelease = true;
                var manager = FindObjectOfType<GameManager>();
                if (manager != null) manager.ReturnHome();
                return;
            }
            if (!held)
            {
                pressed = false;
                if (results != null) results.PressButton1();
            }
        }

        public void SetPhysicalButtonPressed(bool held) => physicalHeld = held;
        private void OnApplicationFocus(bool focused) { pressed = false; waitForRelease = true; }
        private void OnDisable() { pressed = false; physicalHeld = false; }
    }
}
