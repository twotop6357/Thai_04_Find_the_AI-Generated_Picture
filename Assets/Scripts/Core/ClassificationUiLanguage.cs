using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Core
{
    /// <summary>Explicit sprite references keep both language variants available in Player builds.</summary>
    [DefaultExecutionOrder(-200)]
    public sealed class ClassificationUiLanguage : MonoBehaviour
    {
        private const float FadeSeconds = 0.5f;

        [Serializable]
        public sealed class ImagePair
        {
            public Image image;
            public Sprite thai;
            public Sprite english;
            public bool useNativeSize;
        }

        [SerializeField] private ImagePair[] images = Array.Empty<ImagePair>();
        [SerializeField] private ClassificationPresentation results;
        [SerializeField] private Sprite[] thaiScores = new Sprite[7];
        [SerializeField] private Sprite[] englishScores = new Sprite[7];

        /// <summary>True while one language fades out and the next fades in. Input waits for this.</summary>
        public static bool IsFading { get; private set; }

        private readonly List<Image> fadingImages = new List<Image>();
        private readonly List<float> restAlphas = new List<float>();
        private Sequence fade;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => IsFading = false;

        private void Awake() => Apply();
        private void OnEnable()
        {
            ClassificationEntryController.LanguageChanged += HandleLanguageChanged;
            Apply();
        }
        private void OnDisable()
        {
            ClassificationEntryController.LanguageChanged -= HandleLanguageChanged;
            StopFade();
        }

        public void Apply()
        {
            bool thai = ClassificationEntryController.SelectedLanguage == ClassificationLanguage.Thai;
            foreach (var pair in images)
                if (pair.image != null)
                {
                    pair.image.sprite = thai ? pair.thai : pair.english;
                    if (pair.useNativeSize) pair.image.SetNativeSize();
                }
            if (results != null) results.SetScoreSprites(thai ? thaiScores : englishScores);
        }

        private void HandleLanguageChanged(bool animated)
        {
            StopFade();
            if (animated) StartFade();
            else Apply();
        }

        private void StartFade()
        {
            foreach (var pair in images)
                if (pair.image != null && pair.image.gameObject.activeInHierarchy)
                {
                    fadingImages.Add(pair.image);
                    restAlphas.Add(pair.image.color.a);
                }
            if (fadingImages.Count == 0) { Apply(); return; }

            IsFading = true;
            // Hidden panels change at the midpoint too, while nothing of the old language is visible.
            fade = DOTween.Sequence().SetUpdate(true)
                .Append(DOVirtual.Float(1, 0, FadeSeconds, SetFadeAlpha).SetEase(Ease.InOutSine))
                .AppendCallback(Apply)
                .Append(DOVirtual.Float(0, 1, FadeSeconds, SetFadeAlpha).SetEase(Ease.InOutSine))
                .OnComplete(StopFade);
        }

        private void SetFadeAlpha(float amount)
        {
            for (int i = 0; i < fadingImages.Count; i++)
            {
                if (fadingImages[i] == null) continue;
                var color = fadingImages[i].color;
                color.a = restAlphas[i] * amount;
                fadingImages[i].color = color;
            }
        }

        private void StopFade()
        {
            var running = fade;
            fade = null;
            running?.Kill();
            SetFadeAlpha(1);
            fadingImages.Clear();
            restAlphas.Clear();
            IsFading = false;
        }
    }
}
