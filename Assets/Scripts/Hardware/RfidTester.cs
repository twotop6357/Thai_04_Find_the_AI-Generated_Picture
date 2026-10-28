using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hardware
{
    /// <summary>
    /// 실물 RFID 리더 없이 카드 올림/내림을 시뮬레이션하는 전역 테스터.
    /// 에디터·Development 빌드에서 첫 씬 로드 직후 자동 생성되어 DontDestroyOnLoad로 씬 전환 후에도 유지되며,
    /// 씬 배치나 프리팹 없이 IMGUI로 그려짐. 입력은 RfidInput으로 전달되므로 실제 리더와 동일한 경로를 탐.
    /// <para>
    /// 조작: F9 표시/숨김, 카드를 리더 칸으로 드래그해 올림, 리더 위 카드를 바깥으로 드래그하거나 우클릭해 내림.
    /// 이미 카드가 있는 리더에 올리면 기존 카드는 내려가 카드 목록으로 돌아감.
    /// </para>
    /// </summary>
    public class RfidTester : MonoBehaviour
    {
        private const float ReferenceHeight = 1080f;
        private const float PanelWidth = 760f;
        private const float PanelHeight = 470f;
        private const float CardWidth = 100f;
        private const float CardHeight = 56f;

        private static RfidTester _instance;

        private readonly Dictionary<RfidReaderSlot, Rect> _readerRects = new Dictionary<RfidReaderSlot, Rect>();
        private readonly Dictionary<string, Rect> _trayRects = new Dictionary<string, Rect>();
        private readonly List<string> _allCardIds = new List<string>();

        private bool _isVisible = true;
        private string _draggingCardId;
        private Vector2 _dragOffset;
        private Vector2 _panelPosition = new Vector2(20f, 20f);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateInstance()
        {
            if (_instance != null) return;

            GameObject go = new GameObject(nameof(RfidTester));
            DontDestroyOnLoad(go);
            go.AddComponent<RfidTester>();
        }
#endif

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _allCardIds.AddRange(RfidCardCatalog.AiCardIds);
            _allCardIds.AddRange(RfidCardCatalog.HumanCardIds);
            BuildLayout();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f9Key.wasPressedThisFrame)
            {
                _isVisible = !_isVisible;
                _draggingCardId = null;
            }
        }

        /// <summary>
        /// 패널 좌상단 기준 상대 좌표로 카드 목록과 리더 칸 위치를 계산함. 실제 설치 배치(중앙 1칸, 하단 좌측 AI 3칸, 하단 우측 인간 3칸)를 따름.
        /// </summary>
        private void BuildLayout()
        {
            const float gap = 10f;

            for (int i = 0; i < _allCardIds.Count; i++)
            {
                _trayRects[_allCardIds[i]] = new Rect(20f + i * (CardWidth + 20f), 50f, CardWidth, CardHeight);
            }

            float slotWidth = CardWidth + 16f;
            float slotHeight = CardHeight + 40f;

            _readerRects[RfidReaderSlot.Center] = new Rect((PanelWidth - slotWidth * 1.4f) * 0.5f, 140f, slotWidth * 1.4f, slotHeight);

            float bottomY = 330f;
            RfidReaderSlot[] aiSlots = { RfidReaderSlot.Ai1, RfidReaderSlot.Ai2, RfidReaderSlot.Ai3 };
            RfidReaderSlot[] humanSlots = { RfidReaderSlot.Human1, RfidReaderSlot.Human2, RfidReaderSlot.Human3 };
            for (int i = 0; i < 3; i++)
            {
                _readerRects[aiSlots[i]] = new Rect(20f + i * (slotWidth + gap), bottomY, slotWidth, slotHeight);
                float humanX = PanelWidth - 20f - (3 - i) * slotWidth - (2 - i) * gap;
                _readerRects[humanSlots[i]] = new Rect(humanX, bottomY, slotWidth, slotHeight);
            }
        }

        private void OnGUI()
        {
            if (!_isVisible || SingleButtonInput.IsSettingsOpen) return;

            float scale = Screen.height / ReferenceHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUI.depth = -1000;

            Rect panelRect = new Rect(_panelPosition, new Vector2(PanelWidth, PanelHeight));
            GUI.Box(panelRect, "RFID Tester (F9 숨김)  |  드래그: 올림/내림, 우클릭: 내림");

            GUI.BeginGroup(panelRect);
            if (GUI.Button(new Rect(PanelWidth - 110f, 22f, 100f, 22f), "모두 내리기"))
            {
                RfidInput.ClearAll();
            }

            GUI.Label(new Rect(20f, 28f, 300f, 20f), "카드 목록");
            GUI.Label(new Rect(20f, 308f, 300f, 20f), "AI 분류");
            GUI.Label(new Rect(PanelWidth - 360f, 308f, 340f, 20f), "인간 분류");

            HandleMouse(Event.current);
            DrawReaders();
            DrawTray();
            DrawDraggingCard(Event.current);
            GUI.EndGroup();
        }

        private void DrawReaders()
        {
            foreach (KeyValuePair<RfidReaderSlot, Rect> pair in _readerRects)
            {
                GUI.Box(pair.Value, pair.Key.ToString());

                string cardId = RfidInput.GetCard(pair.Key);
                if (cardId != null && cardId != _draggingCardId)
                {
                    DrawCard(GetCardRectInReader(pair.Value), cardId);
                }
            }
        }

        private void DrawTray()
        {
            foreach (KeyValuePair<string, Rect> pair in _trayRects)
            {
                bool isOnReader = RfidInput.TryFindSlot(pair.Key, out _);
                if (isOnReader || pair.Key == _draggingCardId)
                {
                    GUI.Box(pair.Value, string.Empty);
                    continue;
                }

                DrawCard(pair.Value, pair.Key);
            }
        }

        private void DrawDraggingCard(Event e)
        {
            if (_draggingCardId == null) return;

            DrawCard(new Rect(e.mousePosition - _dragOffset, new Vector2(CardWidth, CardHeight)), _draggingCardId);
        }

        private static void DrawCard(Rect rect, string cardId)
        {
            Color previousColor = GUI.backgroundColor;
            GUI.backgroundColor = RfidCardCatalog.GetCardType(cardId) == RfidCardType.Ai
                ? new Color(1f, 0.55f, 0.55f)
                : new Color(0.55f, 0.75f, 1f);
            GUI.Button(rect, cardId);
            GUI.backgroundColor = previousColor;
        }

        private static Rect GetCardRectInReader(Rect readerRect)
        {
            return new Rect(readerRect.center.x - CardWidth * 0.5f, readerRect.y + 28f, CardWidth, CardHeight);
        }

        /// <summary>
        /// 그룹 내부 좌표계의 마우스 이벤트로 드래그 시작/종료와 우클릭 내림을 처리함.
        /// 카드를 그리는 GUI.Button이 MouseDown을 먼저 소비하면 드래그가 시작되지 않으므로,
        /// OnGUI에서 그리기보다 먼저 호출해 카드 위 이벤트를 선점(Use)함.
        /// </summary>
        private void HandleMouse(Event e)
        {
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0 && TryPickCard(e.mousePosition, out string cardId, out Rect cardRect))
                    {
                        _draggingCardId = cardId;
                        _dragOffset = e.mousePosition - cardRect.position;
                        e.Use();
                    }
                    else if (e.button == 1 && TryFindReaderAt(e.mousePosition, out RfidReaderSlot slot))
                    {
                        RfidInput.NotifyRemoved(slot);
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (_draggingCardId != null) e.Use();
                    break;

                case EventType.MouseUp:
                    if (_draggingCardId == null || e.button != 0) break;
                    DropCard(_draggingCardId, e.mousePosition);
                    _draggingCardId = null;
                    e.Use();
                    break;
            }
        }

        private void DropCard(string cardId, Vector2 position)
        {
            if (TryFindReaderAt(position, out RfidReaderSlot targetSlot))
            {
                RfidInput.NotifyPlaced(targetSlot, cardId);
            }
            else if (RfidInput.TryFindSlot(cardId, out RfidReaderSlot previousSlot))
            {
                RfidInput.NotifyRemoved(previousSlot);
            }
        }

        private bool TryPickCard(Vector2 position, out string cardId, out Rect cardRect)
        {
            foreach (KeyValuePair<RfidReaderSlot, Rect> pair in _readerRects)
            {
                string placedCardId = RfidInput.GetCard(pair.Key);
                Rect rect = GetCardRectInReader(pair.Value);
                if (placedCardId != null && rect.Contains(position))
                {
                    cardId = placedCardId;
                    cardRect = rect;
                    return true;
                }
            }

            foreach (KeyValuePair<string, Rect> pair in _trayRects)
            {
                if (!RfidInput.TryFindSlot(pair.Key, out _) && pair.Value.Contains(position))
                {
                    cardId = pair.Key;
                    cardRect = pair.Value;
                    return true;
                }
            }

            cardId = null;
            cardRect = default;
            return false;
        }

        private bool TryFindReaderAt(Vector2 position, out RfidReaderSlot slot)
        {
            foreach (KeyValuePair<RfidReaderSlot, Rect> pair in _readerRects)
            {
                if (pair.Value.Contains(position))
                {
                    slot = pair.Key;
                    return true;
                }
            }

            slot = default;
            return false;
        }
    }
}
