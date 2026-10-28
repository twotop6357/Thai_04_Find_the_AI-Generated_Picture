using System.Collections.Generic;
using Hardware;
using UnityEngine;

namespace Core
{
    /// <summary>RFID 카드 ID를 임시 명화 카드 Sprite에 연결한다.</summary>
    internal sealed class RfidCardImageCatalog
    {
        // 중앙 미리보기와 분류 썸네일이 함께 사용할 카드별 Sprite다.
        private readonly Dictionary<string, Sprite> images = new Dictionary<string, Sprite>();

        /// <summary>기존 카드 목록에 대응하는 AI·인간 이미지 여섯 장을 준비한다.</summary>
        internal RfidCardImageCatalog()
        {
            LoadImages(RfidCardCatalog.AiCardIds);
            LoadImages(RfidCardCatalog.HumanCardIds);
        }

        /// <summary>카드에 대응하는 원본 이미지를 반환하고 미등록·빈 카드는 숨긴다.</summary>
        /// <param name="cardId">리더에서 인식한 카드 ID이며 빈 슬롯은 null이다.</param>
        /// <returns>공유할 이미지이며 미등록 카드나 리소스 누락이면 null이다.</returns>
        internal Sprite GetImage(string cardId)
        {
            return cardId != null && images.TryGetValue(cardId, out Sprite image) ? image : null;
        }

        /// <summary>카드 순서와 같은 번호의 Resources 이미지를 등록한다.</summary>
        /// <param name="cardIds">기존 RFID 입력에 등록된 한 출처의 카드 목록이다.</param>
        private void LoadImages(string[] cardIds)
        {
            // 실제 카드 목록에서 이미지 번호를 정할 인덱스다.
            for (int index = 0; index < cardIds.Length; index++)
            {
                // 별도 썸네일 없이 원본 Sprite 하나를 모든 표시 영역에서 공유한다.
                Sprite image = Resources.Load<Sprite>("TEMP_MasterpieceCards/Sprites/" + cardIds[index]);
                images[cardIds[index]] = image;
                if (image == null)
                    Debug.LogWarning($"[RfidCardImageCatalog] Temporary masterpiece sprite is missing for {cardIds[index]}. Check TEMP_MasterpieceCards/Sprites.");
            }
        }
    }
}
