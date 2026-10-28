using DG.Tweening;
using UnityEngine;

namespace Core
{
    /// <summary>Game/Finish 안내 팝업의 공통 등장 연출. 반환 트윈의 취소/완료 처리는 호출자가 소유한다.</summary>
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
