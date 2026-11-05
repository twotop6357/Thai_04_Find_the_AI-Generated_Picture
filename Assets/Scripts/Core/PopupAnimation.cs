using DG.Tweening;
using UnityEngine;

namespace Core
{
    /// <summary>Shared entrance animation for Game/Finish guide popups. The caller owns cancelling and completing the returned tween.</summary>
    public static class PopupAnimation
    {
        public const float FadeDuration = 0.25f;
        public const float PopDuration = 0.3f;
        public const float Overshoot = 1.70158f;

        public static Sequence Show(CanvasGroup backdrop, RectTransform popup, Vector3 restScale, GameObject owner)
        {
            backdrop.alpha = 0;
            backdrop.gameObject.SetActive(true);
            popup.localScale = Vector3.zero;
            popup.gameObject.SetActive(false);

            return DOTween.Sequence().SetUpdate(true).SetRecyclable(false).SetEase(Ease.Linear)
                .SetLink(owner, LinkBehaviour.KillOnDisable)
                .Append(backdrop.DOFade(1, FadeDuration).SetEase(Ease.InOutSine))
                .AppendCallback(() => { if (popup != null) popup.gameObject.SetActive(true); })
                .Append(popup.DOScale(restScale, PopDuration).SetEase(Ease.OutBack, Overshoot));
        }
    }
}
