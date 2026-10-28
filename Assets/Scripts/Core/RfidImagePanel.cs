using System;
using DG.Tweening;
using Hardware;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Core
{
    /// <summary>Fixed-size reader photos fade independently; only a complete known six-reader board shows the instruction.</summary>
    [DisallowMultipleComponent]
    public sealed class RfidImagePanel : MonoBehaviour
    {
        private const float FadeDuration = 0.25f;
        [SerializeField] private Image previewImage;
        [SerializeField] private Image[] aiImages = new Image[3];
        [SerializeField] private Image[] humanImages = new Image[3];
        [SerializeField] private CanvasGroup buttonInstruction;

        private sealed class Photo
        {
            internal Image Image;
            internal Sprite Wanted;
            internal Sprite Shown;
            internal Tween Fade;
            internal bool Exiting;
            internal int Version;
        }

        // Six classification readers followed by the central preview.
        private readonly Photo[] photos = new Photo[7];
        private RfidCardImageCatalog catalog;
        private IDisposable subscription;
        private Tween instructionFade;
        private bool instructionWanted;
        private int instructionVersion;
        private bool initialized;

        public bool HasPendingCardAnimations
        {
            get
            {
                if (instructionFade != null && instructionFade.IsActive() && instructionFade.IsPlaying()) return true;
                for (int i = 0; i < photos.Length; i++)
                    if (photos[i] != null && photos[i].Fade != null && photos[i].Fade.IsActive() && photos[i].Fade.IsPlaying()) return true;
                return false;
            }
        }

        private void Awake()
        {
            if (previewImage == null || aiImages.Length != 3 || humanImages.Length != 3 || buttonInstruction == null)
            {
                Debug.LogError("[RfidImagePanel] A preview, three photos per category and ButtonInstruction are required.", this);
                enabled = false; return;
            }
            for (int i = 0; i < photos.Length; i++)
            {
                Image image = i == 6 ? previewImage : (i < 3 ? aiImages[i] : humanImages[i - 3]);
                if (image == null) { enabled = false; Debug.LogError("[RfidImagePanel] A reader photo is missing.", this); return; }
                photos[i] = new Photo { Image = image };
            }
            catalog = new RfidCardImageCatalog();
            buttonInstruction.blocksRaycasts = false;
            buttonInstruction.interactable = false;
            initialized = true;
        }

        private void OnEnable()
        {
            if (!initialized) return;
            ClearVisuals();
            ContentGate.BeginGameInput(this);
            subscription = ContentGate.OnCardChanged.Subscribe(_ => RefreshImages());
            RefreshImages();
        }

        private void Update()
        {
            // Also reconcile the final unblock event intentionally suppressed by ContentGate.
            if (initialized) RefreshImages();
        }

        private void RefreshImages()
        {
            if (!initialized || !isActiveAndEnabled) return;
            if (ContentGate.IsBlocked) { SetInstruction(false); return; }
            bool complete = true;
            for (int i = 0; i < 6; i++)
            {
                string id = ContentGate.GetGameCard((RfidReaderSlot)(i + 1));
                Sprite sprite = catalog.GetImage(id);
                SetPhoto(photos[i], sprite);
                if (RfidCardCatalog.GetCardType(id) == RfidCardType.Unknown || sprite == null) complete = false;
            }
            SetPhoto(photos[6], catalog.GetImage(ContentGate.GetGameCard(RfidReaderSlot.Center)));
            SetInstruction(complete);
        }

        private void SetPhoto(Photo photo, Sprite sprite)
        {
            if (photo.Wanted == sprite) return;
            photo.Wanted = sprite;
            if (photo.Shown != null)
            {
                if (photo.Shown == sprite) FadeIn(photo); // Re-detection reverses the fade without an alpha jump.
                else if (!photo.Exiting) FadeOut(photo);
            }
            else if (sprite != null) ShowNew(photo);
        }

        private void ShowNew(Photo photo)
        {
            if (photo.Wanted == null || !isActiveAndEnabled) return;
            photo.Shown = photo.Wanted;
            SetAlpha(photo.Image, 0);
            photo.Image.sprite = photo.Shown;
            photo.Image.preserveAspect = true;
            photo.Image.raycastTarget = false;
            photo.Image.enabled = true;
            photo.Image.gameObject.SetActive(true);
            FadeIn(photo);
        }

        private void FadeIn(Photo photo)
        {
            int version = CancelFade(photo);
            photo.Exiting = false;
            photo.Fade = photo.Image.DOFade(1, FadeDuration).SetEase(Ease.InOutSine).SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() => { if (photo.Version == version) photo.Fade = null; });
        }

        private void FadeOut(Photo photo)
        {
            int version = CancelFade(photo);
            photo.Exiting = true;
            photo.Fade = photo.Image.DOFade(0, FadeDuration).SetEase(Ease.InOutSine).SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() =>
                {
                    if (photo.Version != version || !isActiveAndEnabled) return;
                    photo.Fade = null; photo.Shown = null; photo.Exiting = false;
                    HidePhoto(photo);
                    // A different card waits for the previous photo to disappear before its fade-in.
                    if (photo.Wanted != null) ShowNew(photo);
                });
        }

        private static int CancelFade(Photo photo)
        {
            ++photo.Version;
            photo.Fade?.Kill(); photo.Fade = null;
            return photo.Version;
        }

        private void SetInstruction(bool visible)
        {
            if (instructionWanted == visible) return;
            instructionWanted = visible;
            int version = ++instructionVersion;
            instructionFade?.Kill(); instructionFade = null;
            if (visible && !buttonInstruction.gameObject.activeSelf)
            {
                buttonInstruction.alpha = 0;
                buttonInstruction.gameObject.SetActive(true);
            }
            instructionFade = buttonInstruction.DOFade(visible ? 1 : 0, FadeDuration)
                .SetEase(Ease.InOutSine).SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() =>
                {
                    if (version != instructionVersion) return;
                    instructionFade = null;
                    if (!instructionWanted) buttonInstruction.gameObject.SetActive(false);
                });
        }

        private static void SetAlpha(Image image, float alpha)
        {
            var color = image.color; color.a = alpha; image.color = color;
        }

        private static void HidePhoto(Photo photo)
        {
            SetAlpha(photo.Image, 0);
            photo.Image.enabled = false;
            photo.Image.sprite = null;
            photo.Image.gameObject.SetActive(false);
        }

        private void ClearVisuals()
        {
            ++instructionVersion; instructionWanted = false;
            instructionFade?.Kill(); instructionFade = null;
            buttonInstruction.alpha = 0; buttonInstruction.gameObject.SetActive(false);
            foreach (var photo in photos)
            {
                CancelFade(photo); photo.Wanted = photo.Shown = null; photo.Exiting = false;
                HidePhoto(photo);
            }
        }

        private void OnDisable()
        {
            ContentGate.EndGameInput(this);
            subscription?.Dispose(); subscription = null;
            if (initialized) ClearVisuals();
        }
    }
}
