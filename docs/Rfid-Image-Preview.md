# Current GamePanel fades — 2026-10-27

All seven GamePanel photos now fade in independently over 0.25 seconds, with no scale animation. Existing cover fitting and 1% overscan remain fixed. Removed photos fade out over 0.25 seconds. ButtonInstruction starts inactive and fades in over 0.25 seconds only when all six classification readers recognize known cards; losing any one fades it out over 0.25 seconds and deactivates it. Center is excluded. Wrong answers still count as recognized cards. Rapid re-detection continues from the current alpha. As of 2026-10-28, panel entry starts with no accepted cards: existing physical cards require removal and a fresh placement, as described in Rfid-Game-Flow.md.

Play Mode verification passed 40/40 checks, including each reader's removal, rapid re-detection, fixed scales, unknown cards, panel cleanup and score submission. Evidence: Temp/GamePhotoFades/final-results.json. Real statistics remained unchanged. The older implementation notes below are historical.

# Current temporary artwork — 2026-10-27

Resources/TEMP_MasterpieceCards replaces the previous drawn previews via RfidCardImageCatalog: three generated AI originals and three real public-domain paintings from The Met. Existing _PROTO files remain preserved. Six prebuilt Sprite assets share their original textures and use the exact Game ImageMask ratio. Each painting keeps its original aspect and uses a presentation crop; provenance and prompts are inside the temporary folder.

The central preview, six reader photos and six response-chart photos use EnvelopeParent fitting and 1% overscan. Their image rectangles cover the entire mask while maintaining the sprite ratio. Existing masks and reader identities are unchanged. Score/instruction TMP references are optional when the artwork presentation is assigned, so removing obsolete text objects no longer blocks score submission.

# GamePanel mask alignment — 2026-10-27

The seven apertures now use normalized contours traced from this project's existing background, rather than approximate rounded rectangles. AI/Human group transforms stretch with GamePanel, and each mask uses normalized background coordinates. The custom contour is serialized on RoundedRectMaskGraphic; other rounded masks retain the original implementation. The source PNG and photo sprite references are unchanged.

At native1920×1080, an actual stencil-fill capture matched all seven extracted reference regions with zero differing pixels. Point-coordinate checks also passed at1200×1000 and2560×900 panel shapes. A changed background drawing requires recalibration; arbitrary display antialiasing results were not measured. Extraction and verification evidence is in Temp/GameMaskAlignment.

# RFID 이미지 미리보기와 분류 썸네일

2026-10-21 사용자 요청. 구현 대상은 Thai_04_Find_the_AI-Generated_Picture다.

## 동작

- GamePanel의 중앙 리더 Center에 카드를 올리면 RFIDImageBox에 원본을 표시한다.
- Ai1·Ai2·Ai3는 AI_ImageGroup의 첫째·둘째·셋째 Image에 대응한다.
- Human1·Human2·Human3는 Human_ImageGroup의 첫째·둘째·셋째 Image에 대응한다.
- 썸네일도 원본과 같은 Sprite를 공유하며 이미지 비율을 보존한다. 기존 Image의 좌표·크기·그룹 순서를 유지한다.
- 카드가 제거되면 해당 Image의 Sprite를 비우고 Image만 숨긴다. 빈 슬롯의 오브젝트를 끄지 않으므로 레이아웃 위치가 유지된다.
- 카드 이동·교체는 기존 RfidInput의 제거/배치 이벤트를 따르며 다른 칸은 보존한다. 모두 내리기는 일곱 칸을 비운다.
- 카드의 실제 출처와 분류 리더 위치는 독립적이다. AI 카드를 인간 칸에 놓아도 AI 카드의 이미지가 보이며, 이번 작업에서 정답 판정이나 자동 분류는 추가하지 않는다.
- 미등록 카드·누락 이미지는 이전 이미지를 남기지 않고 해당 칸을 비운다. 파일 누락은 카탈로그 초기화 때 경고한다.
- GamePanel 진입·재진입 시 게임 인정 상태를 비운다. 기존 물리 배치는 유지하지만 제거 후 다시 태그한 카드만 표시·판정에 사용한다. 비활성화 중에는 구독을 해제한다.

## 카드와 이미지

| 카드 ID | Resources 이미지 |
| --- | --- |
| AI_1 | _PROTO/AI1.png |
| AI_2 | _PROTO/AI2.png |
| AI_3 | _PROTO/AI3.png |
| HUMAN_1 | _PROTO/인간1.png |
| HUMAN_2 | _PROTO/인간2.png |
| HUMAN_3 | _PROTO/인간3.png |

이미지는 Sprite/Single로 임포트하고 NPOT 리사이즈와 밉맵을 끈다. PNG 내용과 기존 GUID는 보존한다.

RfidCardImageCatalog는 카드 ID와 이미지 조회를, RfidImagePanel은 일곱 UI의 표시와 구독 수명만 담당한다. GamePanel에 RfidImagePanel을 붙이고 previewImage와 두 Image 배열을 Scene 참조로 연결한다. 런타임 상태·리소스 캐시는 직렬화하지 않는다.

콘텐츠 입력은 기존 ContentGate.OnCardChanged를 구독한다. HelpPanel의 보드 비우기 팝업으로 콘텐츠가 차단되는 동안 표시 갱신도 중지하며, 차단 해제 후 새 카드 입력은 정상 반영한다. RFID 테스터 자체와 실물 입력의 공통 진입점 RfidInput은 수정하지 않는다. 실제 UID·통신 연결 추가는 이번 범위 밖이다.

## 검증

Unity 2022.3.62f3의 1_Game Scene Play Mode에서 실제 RfidTester.DropCard에 각 리더 사각형 중심 좌표를 전달해 여섯 카드 × 일곱 리더 42조합을 검사했다. 정확한 Sprite·빈 슬롯·종횡비·반복 입력·카드 이동·동일 Sprite 공유·교체·제거·모두 내리기·비활성화/재진입·미등록 카드까지 443개 조건을 자동 확인했다. 별도로 HelpPanel 차단·보드 비우기 후 해제·후속 입력 재개를 확인했다.

Game View 캡처 Temp/RfidImagePreview/GamePanel.png로 중앙 원본과 분류 칸의 동일 이미지 썸네일을 육안 확인했다. 이 검증은 Play Mode의 모의 입력 자동 검사이며 Unity Test Runner의 NUnit 테스트나 실제 마우스/물리 RFID 검증으로 기록하지 않는다. Windows Player 빌드는 미실행이다.
