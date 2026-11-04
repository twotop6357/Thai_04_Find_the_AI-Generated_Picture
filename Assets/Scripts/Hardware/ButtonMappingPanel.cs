// 사용 중단: 버튼을 Input Manager의 NextButton으로 옮기면서 F8 재등록 UI를 쓰지 않는다. 삭제 대상.
#if false
using UnityEngine;

namespace Hardware
{
    /// <summary>Operator-only single-button registration UI, available in both Editor and Player.</summary>
    [RequireComponent(typeof(SingleButtonInput))]
    public sealed class ButtonMappingPanel : MonoBehaviour
    {
        private GUIStyle title;
        private GUIStyle label;
        private GUIStyle detail;
        private GUIStyle buttonStyle;

        private void OnGUI()
        {
            var input = SingleButtonInput.Instance;
            if (input == null || !SingleButtonInput.IsSettingsOpen) return;
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
                label = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true };
                detail = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
                buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 19 };
                title.normal.textColor = label.normal.textColor = detail.normal.textColor = Color.white;
            }

            var previousMatrix = GUI.matrix;
            var previousColor = GUI.color;
            var previousDepth = GUI.depth;
            try
            {
                GUI.depth = -10000;
                float scale = Mathf.Max(0.25f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
                GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
                var screen = new Rect(0, 0, Screen.width / scale, Screen.height / scale);
                GUI.color = new Color(0, 0, 0, 0.85f);
                GUI.DrawTexture(screen, Texture2D.whiteTexture);
                GUI.color = Color.white;

                var panel = new Rect((screen.width - 700) / 2, (screen.height - 420) / 2, 700, 420);
                GUI.color = new Color(0.08f, 0.09f, 0.12f, 1);
                GUI.DrawTexture(panel, Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Box(panel, GUIContent.none);
                GUI.BeginGroup(panel);
                GUI.Label(new Rect(28, 20, 640, 40), "Button Settings", title);
                GUI.Label(new Rect(28, 70, 644, 60), "Button 1: " + input.BindingDisplayName, label);
                GUI.Label(new Rect(28, 136, 644, 30), input.IsBindingConnected
                    ? "Device connected" : "Device disconnected. The saved mapping is kept.", detail);
                GUI.Label(new Rect(28, 179, 644, 68), input.Status, detail);

                if (input.IsRebinding)
                {
                    if (GUI.Button(new Rect(28, 264, 644, 48), "Cancel registration (Esc)", buttonStyle)) input.CancelRebinding();
                }
                else
                {
                    if (GUI.Button(new Rect(28, 264, 314, 48), "Register button", buttonStyle)) input.StartRebinding();
                    if (GUI.Button(new Rect(358, 264, 314, 48), "Reset to keyboard 1", buttonStyle)) input.ResetBinding();
                }
                if (GUI.Button(new Rect(28, 328, 644, 48), "Close (F8)", buttonStyle)) input.CloseSettings();
                GUI.Label(new Rect(28, 385, 644, 28), "One button for start, answer submission and next screen.", detail);
                GUI.EndGroup();
            }
            finally
            {
                GUI.matrix = previousMatrix;
                GUI.color = previousColor;
                GUI.depth = previousDepth;
            }
        }
    }
}
#endif
