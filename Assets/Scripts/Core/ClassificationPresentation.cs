using DG.Tweening;
using Hardware;
using UnityEngine;
using UnityEngine.UI;

namespace Core
{
    /// <summary>Frozen reader photos, fixed-size stamps, then score and instruction.</summary>
    public sealed class ClassificationPresentation : MonoBehaviour
    {
        [SerializeField] private Image scoreImage;
        [SerializeField] private Sprite[] scoreSprites = new Sprite[7];
        [SerializeField] private Image[] feedbackImages = new Image[6];
        [SerializeField] private Image[] resultPhotos = new Image[6];
        [SerializeField] private Sprite correctSprite;
        [SerializeField] private Sprite incorrectSprite;
        [SerializeField] private CanvasGroup instruction;
        [SerializeField] private Vector2 englishScorePosition = new Vector2(107, -213);
        [SerializeField] private Vector2 thaiScorePosition = new Vector2(68, -217);
        private readonly Vector2[] restingPositions = new Vector2[6];
        private readonly Quaternion[] restingRotations = new Quaternion[6];
        private bool captured;
        private bool ready;
        private Sequence sequence;
        private int scoreCount;
        public bool IsAnimating => !ready;

        public void SetScoreSprites(Sprite[] sprites)
        {
            if (sprites == null || sprites.Length != 7) return;
            scoreSprites = sprites;
            if (scoreImage != null)
            {
                scoreImage.sprite = scoreSprites[scoreCount];
                scoreImage.preserveAspect = true;
                scoreImage.SetNativeSize();
                scoreImage.rectTransform.anchoredPosition =
                    ClassificationEntryController.SelectedLanguage == ClassificationLanguage.Thai
                        ? thaiScorePosition : englishScorePosition;
            }
        }

        public void Prepare(int count, string[] cards)
        {
            Stop();
            scoreCount = Mathf.Clamp(count, 0, 6);
            scoreImage.sprite = scoreSprites[scoreCount];
            scoreImage.SetNativeSize();
            var catalog = new RfidCardImageCatalog();
            for (int i = 0; i < feedbackImages.Length; i++)
            {
                bool correct = RfidCardCatalog.GetCardType(cards[i]) == (i < 3 ? RfidCardType.Ai : RfidCardType.Human);
                feedbackImages[i].sprite = correct ? correctSprite : incorrectSprite;
                resultPhotos[i].sprite = catalog.GetImage(cards[i]);
                resultPhotos[i].enabled = resultPhotos[i].sprite != null;
            }
            HideForEntry();
        }

        private void CaptureRestingTransforms()
        {
            if (captured) return;
            for (int i = 0; i < feedbackImages.Length; i++)
            {
                restingPositions[i] = feedbackImages[i].rectTransform.anchoredPosition;
                restingRotations[i] = feedbackImages[i].rectTransform.localRotation;
            }
            captured = true;
        }

        public void HideForEntry()
        {
            Stop();
            SetAlpha(scoreImage, 0);
            foreach (var image in feedbackImages) SetAlpha(image, 0);
            instruction.alpha = 0;
            instruction.gameObject.SetActive(false);
        }

        public void Play()
        {
            HideForEntry();
            sequence = DOTween.Sequence().SetUpdate(true).SetRecyclable(false)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            for (int i = 0; i < feedbackImages.Length; i++)
                sequence.Insert(i * 0.2f, Stamp(i));
            // Every stamp settles before the score; input stays blocked through the instruction fade.
            sequence.Append(scoreImage.DOFade(1, 0.25f).SetEase(Ease.InOutSine));
            sequence.AppendCallback(() => instruction.gameObject.SetActive(true));
            sequence.Append(instruction.DOFade(1, 0.25f).SetEase(Ease.InOutSine));
            sequence.OnComplete(() => ready = true);
        }

        private Sequence Stamp(int index)
        {
            var image = feedbackImages[index];
            var rect = image.rectTransform;
            Vector2 rest = restingPositions[index];
            Quaternion rotation = restingRotations[index];
            var stamp = DOTween.Sequence();
            stamp.AppendCallback(() =>
            {
                rect.anchoredPosition = rest + new Vector2(0, 12);
                rect.localRotation = rotation * Quaternion.Euler(0, 0, -4);
            });
            stamp.Append(image.DOFade(1, 0.055f).SetEase(Ease.InQuad));
            stamp.Join(rect.DOAnchorPos(rest, 0.055f).SetEase(Ease.InCubic));
            stamp.Join(rect.DOLocalRotateQuaternion(rotation, 0.055f).SetEase(Ease.InCubic));
            stamp.Append(rect.DOAnchorPos(rest + new Vector2(2, -3), 0.035f));
            stamp.Join(rect.DOLocalRotateQuaternion(rotation * Quaternion.Euler(0, 0, 1.5f), 0.035f));
            stamp.Append(rect.DOAnchorPos(rest + new Vector2(-1, 1), 0.045f));
            stamp.Join(rect.DOLocalRotateQuaternion(rotation * Quaternion.Euler(0, 0, -0.7f), 0.045f));
            stamp.Append(rect.DOAnchorPos(rest, 0.085f).SetEase(Ease.OutQuad));
            stamp.Join(rect.DOLocalRotateQuaternion(rotation, 0.085f).SetEase(Ease.OutQuad));
            return stamp;
        }

        private static void SetAlpha(Image image, float alpha)
        {
            var color = image.color; color.a = alpha; image.color = color;
        }

        public void Stop()
        {
            ready = false;
            sequence?.Kill(); sequence = null;
            CaptureRestingTransforms();
            for (int i = 0; i < feedbackImages.Length; i++)
            {
                feedbackImages[i].rectTransform.anchoredPosition = restingPositions[i];
                feedbackImages[i].rectTransform.localRotation = restingRotations[i];
            }
        }

        private void OnDisable() { Stop(); }
    }
}
