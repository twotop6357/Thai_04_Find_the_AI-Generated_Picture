using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core
{
    /// <summary>A continuous horizontal split clipped by one fixed capsule mask.</summary>
    public sealed class ImageClassificationChart : MonoBehaviour
    {
        private const float MoveDuration = 0.5f;
        [SerializeField] private string cardId;
        [SerializeField] private Image picture;
        [SerializeField] private RectTransform aiBar;
        [SerializeField] private RectTransform humanBar;
        [SerializeField] private TMP_Text aiPercentage;
        [SerializeField] private TMP_Text humanPercentage;
        private Sequence movement;
        private float targetAi;
        private bool hasResponses;
        public bool IsAnimating => movement != null && movement.IsActive() && movement.IsPlaying();

        public void Refresh(ImageClassificationStatistics statistics)
        {
            Stop();
            picture.sprite = new RfidCardImageCatalog().GetImage(cardId);
            picture.preserveAspect = true;
            statistics.GetCounts(cardId, out long ai, out long human);
            long total = ai + human;
            hasResponses = total > 0;
            targetAi = total == 0 ? 0 : (float)((double)ai / total);
            SetProgress(0);
        }

        public void Play(float delay = 0)
        {
            Stop();
            SetProgress(0);
            movement = DOTween.Sequence().SetUpdate(true).SetRecyclable(false)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            if (delay > 0) movement.AppendInterval(delay);
            movement.Append(DOVirtual.Float(0, 1, MoveDuration, SetProgress).SetEase(Ease.InOutSine));
        }

        private void SetProgress(float progress)
        {
            // Start at an even split and move a single boundary: the full capsule never breaks apart.
            float split = Mathf.Lerp(0.5f, targetAi, progress);
            aiBar.gameObject.SetActive(hasResponses);
            humanBar.gameObject.SetActive(hasResponses);
            aiBar.anchorMin = Vector2.zero;
            aiBar.anchorMax = new Vector2(split, 1);
            aiBar.offsetMin = aiBar.offsetMax = Vector2.zero;
            // Human is an uninterrupted backing layer; AI overlays it up to the shared boundary.
            humanBar.anchorMin = Vector2.zero; humanBar.anchorMax = Vector2.one;
            humanBar.offsetMin = humanBar.offsetMax = Vector2.zero;
            if (!hasResponses)
            {
                aiPercentage.text = humanPercentage.text = "—";
                return;
            }
            int ai = Mathf.RoundToInt(split * 100);
            aiPercentage.text = ai.ToString(CultureInfo.InvariantCulture) + "%";
            humanPercentage.text = (100 - ai).ToString(CultureInfo.InvariantCulture) + "%";
        }

        public void Stop() { movement?.Kill(); movement = null; }
        private void OnDisable() { Stop(); }
    }
}
