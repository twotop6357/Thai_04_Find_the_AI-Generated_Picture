using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace Core
{
    /// <summary>One owned tutorial sequence. Shared artwork is borrowed; no textures are copied.</summary>
    public sealed class TutorialCardPresentation : MonoBehaviour
    {
        [SerializeField] private Image arrowMarkImage;
        [SerializeField] private Image cardGuideLine;
        [SerializeField] private Image card1;
        [SerializeField] private Image cardLayoutImage;
        [SerializeField] private RectTransform[] cardDecos = new RectTransform[2];
        [Header("Card body calibration (normalized sprite coordinates, bottom-left origin)")]
        [Tooltip("Opaque card body in the existing 112x169 sprite, excluding the soft shadow: pixels x2..102, y10..167.")]
        [SerializeField] private Rect cardBody = new Rect(2f / 112f, 10f / 169f, 100f / 112f, 157f / 169f);
        [SerializeField] private Rect targetBody = new Rect(0, 0, 1, 1);
        [Header("Lift, travel, and placement")]
        [SerializeField, Min(0.01f)] private float liftDuration = 0.18f;
        [SerializeField, Min(0.01f)] private float moveDuration = 0.9f;
        [SerializeField, Min(0.01f)] private float placeDuration = 0.18f;
        [SerializeField, Min(1)] private float liftScale = 1.08f;
        [SerializeField, Min(1)] private float hoverScale = 1.06f;
        [SerializeField, Min(0)] private float travelArcHeight = 32f;
        [SerializeField, Min(0.01f)] private float decoDuration = 0.2f;

        [Header("Playback and replay")]
        [SerializeField, Range(0.1f, 2f)] private float playbackSpeed = 0.75f;
        [SerializeField, Min(1)] private int playCount = 2;
        [FormerlySerializedAs("replayFadeDuration")]
        [Tooltip("Total real-time duration: half fades out, reset is hidden, then half fades in. No pause is inserted.")]
        [SerializeField, Min(0.01f)] private float replayResetDuration = 1f;
        [Tooltip("Minimum real-time hold from final card placement, including the decoration pop.")]
        [SerializeField, Min(0)] private float finalArrivalHold = 0.5f;
        public int CompletedCycles { get; private set; }
        private Sequence sequence;
        private CancellationTokenSource enableLifetime;
        private int version;
        private bool initialized;
        private CanvasGroup sourceOpacity;
        private float sourceAlpha;
        private Image movingCard;
        private RectTransform host;
        private Vector3[] decoScales;
        private Rect sourceBodyRect;
        private Vector3 startCenter, endCenter, startScale, endScale;
        private Quaternion startRotation, endRotation;
        public bool IsPlaying { get; private set; }
        public bool IsComplete { get; private set; }

        private void Awake()
        {
            if (arrowMarkImage == null || cardGuideLine == null || card1 == null || cardLayoutImage == null ||
                cardDecos.Length != 2 || cardDecos[0] == null || cardDecos[1] == null || card1.sprite == null)
            {
                Debug.LogError("[TutorialCardPresentation] Arrow, guide, source card, target layout and two decorations are required.", this);
                enabled = false; return;
            }
            host = (RectTransform)transform;
            sourceOpacity = card1.GetComponent<CanvasGroup>();
            if (sourceOpacity == null) sourceOpacity = card1.gameObject.AddComponent<CanvasGroup>();
            sourceAlpha = sourceOpacity.alpha;
            decoScales = new[] { cardDecos[0].localScale, cardDecos[1].localScale };
            initialized = true;
        }

        private void OnEnable()
        {
            if (!initialized) return;
            StopAndReset();
            enableLifetime = new CancellationTokenSource();
            PlayAfterEntryAsync(version, enableLifetime.Token).Forget();
        }

        private async UniTask PlayAfterEntryAsync(int expectedVersion, CancellationToken token)
        {
            try
            {
                await UniTask.WaitUntil(() => !ScreenTransition.IsBusy, cancellationToken: token);
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, token);
                if (expectedVersion == version && isActiveAndEnabled) Play();
            }
            catch (OperationCanceledException) { }
            catch (Exception exception) { Debug.LogError($"[TutorialCardPresentation] Tutorial entry failed: {exception}", this); }
        }

        public void Play()
        {
            if (!initialized || !isActiveAndEnabled) return;
            StopAndReset();
            PrepareCardGeometry();
            IsPlaying = true;
            sequence = DOTween.Sequence().SetUpdate(true).SetEase(Ease.Linear).SetRecyclable(false)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            float speed = Mathf.Clamp(playbackSpeed, 0.1f, 2f);
            sequence.timeScale = speed;
            int count = Mathf.Max(1, playCount);
            for (int i = 0; i < count; i++)
            {
                AppendCycle(sequence);
                if (i + 1 == count)
                    sequence.AppendInterval(Mathf.Max(0, finalArrivalHold * speed - decoDuration));
                sequence.AppendCallback(() => CompletedCycles++);
                if (i + 1 < count) AppendReplayReset(sequence, speed);
            }
            sequence.OnComplete(() => { IsPlaying = false; IsComplete = true; });
        }

        private void AppendCycle(Sequence sequence)
        {
            sequence.AppendCallback(ResetCues);
            AppendPulse(sequence, arrowMarkImage, false);
            AppendPulse(sequence, cardGuideLine, true);
            sequence.AppendCallback(() =>
            {
                sourceOpacity.alpha = 0;
                movingCard.gameObject.SetActive(true);
                SetCardPose(startCenter, startScale, startRotation);
            });
            sequence.Append(DOVirtual.Float(0, 1, liftDuration, t =>
                SetCardPose(startCenter, Vector3.LerpUnclamped(startScale, startScale * liftScale, t), startRotation))
                .SetEase(Ease.OutSine));
            sequence.Append(DOVirtual.Float(0, 1, moveDuration, t =>
            {
                var center = Vector3.LerpUnclamped(startCenter, endCenter, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * travelArcHeight);
                SetCardPose(center, Vector3.LerpUnclamped(startScale * liftScale, endScale * hoverScale, t),
                    Quaternion.SlerpUnclamped(startRotation, endRotation, t));
            }).SetEase(Ease.InOutSine));
            sequence.Append(DOVirtual.Float(0, 1, placeDuration, t =>
                SetCardPose(endCenter, Vector3.LerpUnclamped(endScale * hoverScale, endScale, t), endRotation))
                .SetEase(Ease.OutSine));
            sequence.AppendCallback(() =>
            {
                // The body is fitted before the decorations become visible, on the same tween update.
                SetCardPose(endCenter, endScale, endRotation);
                foreach (var deco in cardDecos) { deco.localScale = Vector3.zero; deco.gameObject.SetActive(true); }
            });
            sequence.Append(cardDecos[0].DOScale(decoScales[0], decoDuration).SetEase(Ease.OutBack, 2.2f));
            sequence.Join(cardDecos[1].DOScale(decoScales[1], decoDuration).SetEase(Ease.OutBack, 2.2f));
        }

        private void AppendReplayReset(Sequence timeline, float speed)
        {
            // Only the selected card fades. Boards, the other five cards and background stay opaque.
            float halfDuration = replayResetDuration * 0.5f * speed;
            timeline.Append(movingCard.DOFade(0, halfDuration).SetEase(Ease.InOutSine));
            timeline.AppendCallback(() =>
            {
                movingCard.gameObject.SetActive(false);
                sourceOpacity.alpha = 0;
                PrepareCardGeometry();
            });
            timeline.Append(DOVirtual.Float(0, sourceAlpha, halfDuration, alpha => sourceOpacity.alpha = alpha).SetEase(Ease.InOutSine));
        }

        private static void AppendPulse(Sequence timeline, Image image, bool leaveVisible)
        {
            timeline.AppendCallback(() => { SetAlpha(image, 0); image.gameObject.SetActive(true); });
            for (int i = 0; i < 2; i++)
            {
                timeline.Append(image.DOFade(1, 0.25f).SetEase(Ease.InOutSine));
                timeline.AppendInterval(0.2f);
                if (!(leaveVisible && i == 1)) timeline.Append(image.DOFade(0, 0.2f).SetEase(Ease.InOutSine));
            }
            if (!leaveVisible) timeline.AppendCallback(() => image.gameObject.SetActive(false));
        }

        private void PrepareCardGeometry()
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)card1.transform.parent);
            if (movingCard == null)
            {
                var go = new GameObject("Card1_MovingPresentation", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(host, false);
                movingCard = go.GetComponent<Image>();
                movingCard.raycastTarget = false;
            }
            var rect = movingCard.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = card1.rectTransform.pivot;
            rect.sizeDelta = card1.rectTransform.rect.size;
            movingCard.sprite = card1.sprite;
            movingCard.type = card1.type;
            movingCard.preserveAspect = card1.preserveAspect;
            movingCard.color = card1.color;
            movingCard.material = card1.material;
            movingCard.transform.SetAsLastSibling();
            movingCard.gameObject.SetActive(false);
            sourceBodyRect = BodyRect(DrawRect(movingCard), cardBody);
            var fromBody = BodyRect(DrawRect(card1), cardBody);
            var toBody = BodyRect(DrawRect(cardLayoutImage), targetBody);
            startCenter = host.InverseTransformPoint(card1.transform.TransformPoint(fromBody.center));
            endCenter = host.InverseTransformPoint(cardLayoutImage.transform.TransformPoint(toBody.center));
            startRotation = Quaternion.Inverse(host.rotation) * card1.transform.rotation;
            endRotation = Quaternion.Inverse(host.rotation) * cardLayoutImage.transform.rotation;
            startScale = new Vector3(
                host.InverseTransformVector(card1.transform.TransformVector(Vector3.right)).magnitude,
                host.InverseTransformVector(card1.transform.TransformVector(Vector3.up)).magnitude, 1);
            endScale = new Vector3(
                host.InverseTransformVector(cardLayoutImage.transform.TransformVector(Vector3.right * toBody.width)).magnitude / sourceBodyRect.width,
                host.InverseTransformVector(cardLayoutImage.transform.TransformVector(Vector3.up * toBody.height)).magnitude / sourceBodyRect.height, 1);
            SetCardPose(startCenter, startScale, startRotation);
        }

        private static Rect BodyRect(Rect draw, Rect body) => new Rect(draw.x + body.x * draw.width,
            draw.y + body.y * draw.height, body.width * draw.width, body.height * draw.height);

        private static Rect DrawRect(Image image)
        {
            var rect = image.GetPixelAdjustedRect();
            if (!image.preserveAspect || image.sprite == null) return rect;
            float ratio = image.sprite.rect.width / image.sprite.rect.height;
            if (ratio > rect.width / rect.height)
            {
                float previous = rect.height; rect.height = rect.width / ratio;
                rect.y += (previous - rect.height) * image.rectTransform.pivot.y;
            }
            else
            {
                float previous = rect.width; rect.width = rect.height * ratio;
                rect.x += (previous - rect.width) * image.rectTransform.pivot.x;
            }
            return rect;
        }

        private void SetCardPose(Vector3 bodyCenter, Vector3 scale, Quaternion rotation)
        {
            var rect = movingCard.rectTransform;
            rect.localScale = scale;
            rect.localRotation = rotation;
            rect.localPosition = bodyCenter - rotation * Vector3.Scale(sourceBodyRect.center, scale);
        }

        private static void SetAlpha(Image image, float alpha)
        {
            var color = image.color; color.a = alpha; image.color = color;
        }

        public void StopAndReset()
        {
            ++version; sequence?.Kill(); sequence = null;
            IsPlaying = IsComplete = false;
            CompletedCycles = 0;
            ResetVisuals();
        }

        private void ResetVisuals()
        {
            if (!initialized) return;
            sourceOpacity.alpha = sourceAlpha;
            if (movingCard != null) movingCard.gameObject.SetActive(false);
            ResetCues();
        }

        private void ResetCues()
        {
            SetAlpha(arrowMarkImage, 0); arrowMarkImage.gameObject.SetActive(false);
            SetAlpha(cardGuideLine, 0); cardGuideLine.gameObject.SetActive(false);
            for (int i = 0; i < cardDecos.Length; i++)
            {
                cardDecos[i].localScale = decoScales[i]; cardDecos[i].gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            enableLifetime?.Cancel(); enableLifetime?.Dispose(); enableLifetime = null;
            StopAndReset();
        }

        private void OnDestroy()
        {
            if (movingCard != null) Destroy(movingCard.gameObject);
        }
    }
}
