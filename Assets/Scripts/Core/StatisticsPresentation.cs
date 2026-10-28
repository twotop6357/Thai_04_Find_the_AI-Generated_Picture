using DG.Tweening;
using UnityEngine;

namespace Core
{
    /// <summary>Runs six horizontal charts together, then reveals the existing guide artwork.</summary>
    public sealed class StatisticsPresentation : MonoBehaviour
    {
        [SerializeField] private ImageClassificationChart[] charts = new ImageClassificationChart[6];
        [SerializeField] private CanvasGroup instruction;
        private Sequence sequence;
        private bool ready;
        public bool IsAnimating => !ready;

        public void HideForEntry()
        {
            Stop();
            instruction.alpha = 0;
            instruction.gameObject.SetActive(false);
        }

        public void Play()
        {
            HideForEntry();
            foreach (var chart in charts) chart.Play();
            sequence = DOTween.Sequence().SetUpdate(true).SetRecyclable(false)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            sequence.AppendInterval(0.5f);
            sequence.AppendCallback(() => instruction.gameObject.SetActive(true));
            sequence.Append(instruction.DOFade(1, 0.25f).SetEase(Ease.InOutSine));
            sequence.OnComplete(() => ready = true);
        }

        public void Stop()
        {
            ready = false;
            sequence?.Kill(); sequence = null;
            foreach (var chart in charts) if (chart != null) chart.Stop();
        }

        private void OnDisable() { Stop(); }
    }
}
