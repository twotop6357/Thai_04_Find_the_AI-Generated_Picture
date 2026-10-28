# TEMP / 임시 명화 카드 — 정식 전시용 확정 자료 아님

이 폴더는 기존 `_PROTO` 낙서 대신 사용하는 **임시 테스트 이미지 6종**입니다. 기존 원본 낙서는 삭제하거나 덮어쓰지 않았습니다.

| 카드 ID | 구분 | 이미지 |
|---|---|---|
| AI_1 | AI 생성 원화 | Cobalt Riverside — 푸른 밤의 강변 마을 |
| AI_2 | AI 생성 원화 | Crimson Scarf Portrait — 붉은 스카프 인물화 |
| AI_3 | AI 생성 원화 | Irises and Apricots — 붓꽃과 살구 정물 |
| HUMAN_1 | 실제 인간 화가 작품 | Vincent van Gogh, Self-Portrait with a Straw Hat, 1887 |
| HUMAN_2 | 실제 인간 화가 작품 | Vincent van Gogh, Roses, 1890 |
| HUMAN_3 | 실제 인간 화가 작품 | Edouard Manet, The Monet Family in Their Garden at Argenteuil, 1874 |

AI 3장은 내장 image_gen 도구로 제작한 가상의 작품이며 실제 화가의 진품으로 표시하지 않습니다. 세 번째 정물화는 카드 표시에서 꽃/과일을 보존하도록 한 번 구도를 보정했습니다. 전체 생성/보정 프롬프트는 [PROMPTS.json](PROMPTS.json)에 있습니다.

실제 작품은 The Metropolitan Museum of Art API의 isPublicDomain=true 및 이미지 URL을 확인한 자료만 사용했습니다. 원본 사진 바이트는 그대로 보존했으며 생성형 편집은 하지 않았습니다. 작품 정보/다운로드 URL/CC0 정책은 [SOURCES.json](SOURCES.json)에 기록했습니다.

- [Van Gogh Self-Portrait](https://www.metmuseum.org/art/collection/search/436532)
- [Van Gogh Roses](https://www.metmuseum.org/art/collection/search/436534)
- [Manet Monet Family](https://www.metmuseum.org/art/collection/search/436965)
- [The Met Open Access / CC0 policy](https://www.metmuseum.org/policies/image-resources)

## 파일과 화면 비율

`AI/`, `Human/`에는 생성/공개 원본 이미지가 있고 `Sprites/AI_1.asset` 등 6개는 기존 ImageMask 비율(약0.595934)에 맞춘 Unity Sprite입니다. 파일 원본을 늘이거나 변형하지 않고 UV 표시 영역만 자른 것입니다. 실제 작품은 세로 카드에 맞춘 부분 이미지이며, 원작 전체를 표시하는 것으로 오해하지 않도록 출처를 유지합니다. 정확한 표시 Rect/원본 경로는 [SPRITE_CROPS.json](SPRITE_CROPS.json)에 있습니다.

Game의 중앙 미리보기/분류6장/통계6장에는 AspectRatioFitter.EnvelopeParent와1% overscan을 적용했습니다. 그림의 비율을 유지하며 마스크를 채우고 가장자리의 렌더링 틈을 방지합니다. 프레임에 맞춰 소량의 가장자리가 추가로 잘립니다. 실제 그림 안의 흰 꽃이나 배경색은 여백 오류가 아닙니다.

`RfidCardImageCatalog`가 `TEMP_MasterpieceCards/Sprites/{cardId}`를 읽습니다. 기존 카드 ID·정답 분류·물리 리더 위치·점수·로컬 응답 저장 구조는 유지했습니다. 이전 카드 ID의 기존 응답도 그대로 남아 있으므로, 이 파일은 신규 작품별 운영 통계를 초기화하거나 버전을 나누는 기능을 포함하지 않습니다.

정식 이미지를 확정할 때는 이 임시 폴더와 카탈로그 경로/표시 Rect를 교체하면 됩니다. Sprite 파일은 원본 텍스처를 공유하며 런타임 Texture 복사나 이미지 생성 API 호출은 하지 않습니다.
