using System;
using System.Collections.Generic;
using Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hardware
{
    /// <summary>Operator window opened with F10. Select a card picture, then place the physical card on a reader to register its value.</summary>
    [RequireComponent(typeof(RfidReaderService))]
    public sealed class RfidMappingWindow : MonoBehaviour, IContentBlocker
    {
        // Figma card number order, matching the physical cards so they are easy to find.
        private static readonly string[] CardOrder = { "HUMAN_1", "AI_1", "HUMAN_2", "AI_2", "HUMAN_3", "AI_3" };
        private static readonly RfidReaderSlot[] Slots = (RfidReaderSlot[])Enum.GetValues(typeof(RfidReaderSlot));
        private static readonly Color AiColor = new Color(1f, 0.55f, 0.5f);
        private static readonly Color HumanColor = new Color(0.5f, 0.75f, 1f);
        private static readonly Color SelectedColor = new Color(1f, 0.85f, 0.2f);

        public static bool IsOpen { get; private set; }

        private RfidReaderService service;
        private RfidCardImageCatalog catalog;
        private string selectedCard;
        private string status = "Select a card picture, then place that card on any reader.";
        private string lastFrameText = "No value received yet.";
        private List<RfidClientInfo> clients = new List<RfidClientInfo>();
        private Vector2 valueScroll;
        private Vector2 clientScroll;
        private bool previousCursorVisible;
        private CursorLockMode previousCursorLock;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle smallStyle;
        private GUIStyle buttonStyle;
        private GUIStyle smallButtonStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => IsOpen = false;

        private void Awake() => service = GetComponent<RfidReaderService>();

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.f10Key.wasPressedThisFrame)
            {
                if (IsOpen) Close();
                else Open();
            }
            else if (IsOpen && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (selectedCard != null) selectedCard = null;
                else Close();
            }
        }

        public bool RefreshBlocking() => IsOpen;

        private void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            // Opening the window also picks up direct edits to the file.
            service.ReloadSettings();
            if (catalog == null) catalog = new RfidCardImageCatalog();
            previousCursorVisible = Cursor.visible;
            previousCursorLock = Cursor.lockState;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            service.FrameReceived += HandleFrame;
            ContentGate.Register(this);
            status = service.CanSave
                ? "Select a card picture, then place that card on any reader."
                : RfidSettings.FileName + " could not be read. Changes will not be saved until it is fixed.";
        }

        private void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            selectedCard = null;
            service.FrameReceived -= HandleFrame;
            ContentGate.Unregister(this);
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
        }

        private void OnDisable() => Close();

        private void HandleFrame(RfidFrame frame)
        {
            string card = service.GetCardId(frame.Hex);
            lastFrameText = $"{frame.RemoteIp}:{frame.LocalPort}  {(card ?? "Unmapped")}\n{frame.Hex}";
            if (selectedCard == null) return;

            switch (service.AddCardValue(selectedCard, frame.Hex))
            {
                case RfidReaderService.AddValueResult.Added:
                    status = $"Saved a value to {selectedCard}. Keep the card on the reader if its value changes.";
                    break;
                case RfidReaderService.AddValueResult.MappedToOtherCard:
                    status = $"This value already belongs to {card}. Remove it there first to move it.";
                    break;
                case RfidReaderService.AddValueResult.SaveFailed:
                    status = $"Could not save to {RfidSettings.FileName}. Check the file and its permissions.";
                    break;
            }
        }

        private void OnGUI()
        {
            if (!IsOpen) return;
            EnsureStyles();
            if (Event.current.type == EventType.Layout) clients = service.Clients;

            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            try
            {
                GUI.depth = -10000;
                float scale = Mathf.Max(0.25f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
                GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
                var screen = new Rect(0f, 0f, Screen.width / scale, Screen.height / scale);
                DrawFill(screen, new Color(0.06f, 0.07f, 0.1f, 0.97f));

                var panel = new Rect((screen.width - 1280f) * 0.5f, (screen.height - 720f) * 0.5f, 1280f, 720f);
                GUI.BeginGroup(panel);
                DrawHeader();
                DrawCards(new Rect(20f, 60f, 720f, 420f));
                DrawValues(new Rect(760f, 60f, 500f, 420f));
                DrawReaders(new Rect(20f, 490f, 720f, 170f));
                DrawClients(new Rect(760f, 490f, 500f, 170f));
                GUI.Label(new Rect(20f, 668f, 1240f, 44f), status, labelStyle);
                GUI.EndGroup();
            }
            finally
            {
                GUI.matrix = previousMatrix;
                GUI.color = previousColor;
                GUI.depth = previousDepth;
            }
        }

        private void DrawHeader()
        {
            GUI.Label(new Rect(20f, 12f, 500f, 40f), "RFID Card Mapping", titleStyle);
            RfidSettings settings = service.Settings;
            string server = service.IsListening
                ? $"TCP server listening on port {settings.port}"
                : $"TCP server NOT listening on port {settings.port}";
            GUI.Label(new Rect(420f, 20f, 500f, 28f), $"{server}  |  Release {settings.releaseSeconds:0.##}s", labelStyle);
            if (GUI.Button(new Rect(1120f, 14f, 140f, 34f), "Close (F10)", buttonStyle)) Close();
        }

        private void DrawCards(Rect area)
        {
            const float gap = 12f;
            float width = (area.width - gap * 2f) / 3f;
            float height = (area.height - gap) / 2f;
            for (int i = 0; i < CardOrder.Length; i++)
            {
                var tile = new Rect(area.x + (i % 3) * (width + gap), area.y + (i / 3) * (height + gap), width, height);
                DrawCardTile(tile, CardOrder[i]);
            }
        }

        private void DrawCardTile(Rect tile, string cardId)
        {
            bool selected = cardId == selectedCard;
            bool ai = RfidCardCatalog.GetCardType(cardId) == RfidCardType.Ai;
            DrawFill(tile, selected ? SelectedColor : (ai ? AiColor : HumanColor));
            var inner = new Rect(tile.x + 3f, tile.y + 3f, tile.width - 6f, tile.height - 6f);
            DrawFill(inner, new Color(0.13f, 0.14f, 0.18f, 1f));

            DrawSprite(new Rect(inner.x + 6f, inner.y + 6f, inner.width - 12f, inner.height - 56f), catalog.GetImage(cardId));

            int count = service.Settings.GetCard(cardId)?.hexValues.Count ?? 0;
            string location = RfidInput.TryFindSlot(cardId, out RfidReaderSlot slot) ? $"  |  On {slot}" : "";
            string countText = count == 0 ? "Not mapped" : count == 1 ? "1 value" : count + " values";
            GUI.Label(new Rect(inner.x + 8f, inner.yMax - 48f, inner.width - 16f, 24f), cardId, labelStyle);
            GUI.Label(new Rect(inner.x + 8f, inner.yMax - 24f, inner.width - 16f, 22f), countText + location, smallStyle);

            if (GUI.Button(tile, GUIContent.none, GUIStyle.none))
            {
                selectedCard = selected ? null : cardId;
                valueScroll = Vector2.zero;
                status = selectedCard == null
                    ? "Select a card picture, then place that card on any reader."
                    : $"Waiting for {selectedCard}. Place that card on any reader.";
            }
        }

        private void DrawValues(Rect area)
        {
            DrawFill(area, new Color(0.13f, 0.14f, 0.18f, 1f));
            var content = new Rect(area.x + 12f, area.y + 10f, area.width - 24f, area.height - 20f);
            GUI.Label(new Rect(content.x, content.y, content.width, 28f),
                selectedCard == null ? "Last received value" : "Values for " + selectedCard, labelStyle);

            GUI.Label(new Rect(content.x, content.y + 30f, content.width, 90f), lastFrameText, smallStyle);

            if (selectedCard == null) return;
            RfidCardMapping card = service.Settings.GetCard(selectedCard);
            if (card == null) return;

            var list = new Rect(content.x, content.y + 125f, content.width, content.height - 125f);
            const float rowButton = 76f;
            float textWidth = list.width - rowButton - 28f;
            float total = 0f;
            foreach (string hex in card.hexValues) total += RowHeight(hex, textWidth);

            valueScroll = GUI.BeginScrollView(list, valueScroll, new Rect(0f, 0f, list.width - 20f, total));
            float y = 0f;
            string removed = null;
            foreach (string hex in card.hexValues)
            {
                float height = RowHeight(hex, textWidth);
                // Wrap long values so they are shown in full.
                GUI.Label(new Rect(0f, y, textWidth, height), hex, smallStyle);
                if (GUI.Button(new Rect(textWidth + 4f, y + 2f, rowButton, 26f), "Remove", smallButtonStyle)) removed = hex;
                y += height;
            }
            GUI.EndScrollView();

            if (removed != null && !service.RemoveCardValue(selectedCard, removed))
                status = $"Could not save to {RfidSettings.FileName}. The value was kept.";
        }

        private float RowHeight(string hex, float width) => Mathf.Max(32f, smallStyle.CalcHeight(new GUIContent(hex), width) + 6f);

        private void DrawReaders(Rect area)
        {
            const float gap = 6f;
            float width = (area.width - gap * (Slots.Length - 1)) / Slots.Length;
            for (int i = 0; i < Slots.Length; i++)
            {
                var cell = new Rect(area.x + i * (width + gap), area.y, width, area.height);
                RfidReaderSlot slot = Slots[i];
                bool connected = clients.Exists(client => service.Matches(slot, client.RemoteIp, client.LocalPort));
                DrawFill(cell, connected ? new Color(0.14f, 0.3f, 0.2f, 1f) : new Color(0.2f, 0.16f, 0.16f, 1f));
                GUI.Label(new Rect(cell.x + 5f, cell.y + 4f, cell.width - 10f, 22f), slot.ToString(), labelStyle);
                GUI.Label(new Rect(cell.x + 5f, cell.y + 26f, cell.width - 10f, 18f), connected ? "Connected" : "No reader", smallStyle);

                string current = RfidInput.GetCard(slot);
                if (current == null) continue;
                DrawSprite(new Rect(cell.x + 5f, cell.y + 46f, cell.width - 10f, cell.height - 72f), catalog.GetImage(current));
                bool known = RfidCardCatalog.GetCardType(current) != RfidCardType.Unknown;
                GUI.Label(new Rect(cell.x + 5f, cell.yMax - 24f, cell.width - 10f, 20f), known ? current : "Unmapped", smallStyle);
            }
        }

        private void DrawClients(Rect area)
        {
            DrawFill(area, new Color(0.13f, 0.14f, 0.18f, 1f));
            GUI.Label(new Rect(area.x + 10f, area.y + 4f, area.width - 20f, 24f), "Connected readers: assign each to a slot", labelStyle);
            var list = new Rect(area.x + 8f, area.y + 30f, area.width - 16f, area.height - 36f);
            const float rowHeight = 50f;
            clientScroll = GUI.BeginScrollView(list, clientScroll, new Rect(0f, 0f, list.width - 20f, clients.Count * rowHeight));
            for (int i = 0; i < clients.Count; i++)
            {
                RfidClientInfo client = clients[i];
                float y = i * rowHeight;
                GUI.Label(new Rect(0f, y, list.width - 20f, 20f), $"{client.RemoteIp}:{client.RemotePort} (port {client.LocalPort})", smallStyle);
                float buttonWidth = (list.width - 20f) / Slots.Length;
                for (int s = 0; s < Slots.Length; s++)
                {
                    RfidReaderSlot slot = Slots[s];
                    bool assigned = service.Matches(slot, client.RemoteIp, client.LocalPort);
                    Color previous = GUI.backgroundColor;
                    if (assigned) GUI.backgroundColor = SelectedColor;
                    if (GUI.Button(new Rect(s * buttonWidth, y + 20f, buttonWidth - 2f, 24f), slot.ToString(), smallButtonStyle) && !assigned)
                        status = service.AssignReader(slot, client)
                            ? $"{client.RemoteIp} is now the {slot} reader."
                            : $"Could not save to {RfidSettings.FileName}.";
                    GUI.backgroundColor = previous;
                }
            }
            GUI.EndScrollView();
        }

        private static void DrawSprite(Rect area, Sprite sprite)
        {
            if (sprite == null) return;
            Rect source = sprite.textureRect;
            float fit = Mathf.Min(area.width / source.width, area.height / source.height);
            var size = new Vector2(source.width * fit, source.height * fit);
            var target = new Rect(area.center - size * 0.5f, size);
            Texture2D texture = sprite.texture;
            var coordinates = new Rect(source.x / texture.width, source.y / texture.height,
                source.width / texture.width, source.height / texture.height);
            GUI.DrawTextureWithTexCoords(target, texture, coordinates);
        }

        private static void DrawFill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 16 };
            smallButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 12, padding = new RectOffset(1, 1, 1, 1) };
            titleStyle.normal.textColor = labelStyle.normal.textColor = smallStyle.normal.textColor = Color.white;
        }
    }
}
