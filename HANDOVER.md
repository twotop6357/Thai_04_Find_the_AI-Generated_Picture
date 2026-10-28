# HANDOVER

새 작업은 기존 기록 위에 추가한다.

## 2026-10-21 — Button 1 opens charts from scoring

- Button 1 now routes by screen: completed GamePanel submits once and shows the score; a subsequent press opens image response charts. Digit 1, numpad 1 and public PressButton1 share the routing. Updated English score instruction and flow documentation.
- Eight Play Mode checks passed for first-press score only, second-press chart, numpad 1, Arduino entry, no duplicate responses, blocked retry waiting, and reset. Isolated Temp statistics used; production records untouched. Console errors zero during verification; temporary keyboard removed, board cleared, Play Mode stopped.

## 2026-10-21 — Persistent image response distributions

- Replaced the session score histogram with six picture cards in the supplied 3x2 graph layout: English title, blue AI/yellow Human stacked bars, percentages and response counts. No circled-number annotations. Result screen red button/Space/Enter opens this screen; Back to Results returns. Future learning content is outside this graph implementation.
- Added ImageClassificationStatistics and ImageClassificationChart plus metadata. Accepted button 1 submission persists one category vote per image keyed by card identity, including incorrect choices. Data survives scene/app restart in Application.persistentDataPath/image-classification-statistics-v1.json; no server sync. Atomic replacement keeps a previous-version backup, validates loaded counters, recovers the backup if needed, and refuses recording if persistence is unavailable. Empty data does not invent a distribution. Current response is included and explicitly labelled.
- Updated 1_Game statistics subtree and ClassificationResultPanel references; score, keyboard 1 submission and physical-board-clear replay preserved. Updated docs and changelog. Scene backup Temp/ClassificationCharts/BeforeCharts.unity.
- Verification: fifteen isolated storage checks plus live UI ratios, all six sprites, label fit, no automatic submission, duplicate suppression, empty/100%/33.3% cases, and corrupted-main backup recovery followed by successful saving. Screenshot Temp/ClassificationCharts/Distribution.png visually inspected. Only deliberate backup-recovery test emitted a warning. Production statistics were not modified; all test stores used Temp/ClassificationCharts. Simulated cards cleared and Play Mode stopped. Physical Arduino and Player build untested; no commit.

## 2026-10-21 — Button 1 submission

- Replaced automatic completion with explicit button 1 submission. Keyboard digit 1 and numpad 1 call ClassificationResultPanel.PressButton1(); the future Arduino adapter should invoke the same method on Unity's main thread. The method checks the current board, ignores incomplete/unknown cards and waiting-for-clear state, and does not queue early presses.
- Updated classification flow documentation and changelog. No scene or hardware transport changes needed.
- Play Mode: 11 checks passed using a temporary Input System keyboard device, including digit 1, numpad 1, full-board waiting, early/incomplete/unknown submissions, duplicate suppression, replay, and the public Arduino entry point. Console error count was zero during verification. Temporary keyboard removed, simulated board cleared, and Play Mode stopped. Physical Arduino remains untested.

## 2026-10-21 — Classification result screen

- Added ClassificationResultPanel.cs and ResultMarkGraphic.cs with Unity metadata, and wired Canvas/ResultPanel in 1_Game. Six known classification cards trigger one frozen result; Center is excluded from scoring. English score, AI/Human rows and circle/cross feedback follow the supplied reference without numbered annotations. Artwork is provisional as requested.
- Red button, Space and Enter open in-memory session statistics. Persistent participant statistics and physical red-button transport are not present; PressRedButton is the integration entry point. Try Again waits for all physical readers to clear before reopening GamePanel. Existing RFID, preview and HelpPanel code is preserved.
- Play Mode: all 720 permutations and 10,808 conditions passed. Visual QA found missing CanvasRenderer components on the procedural marks; added them and a RequireComponent attribute, then verified 0/6, 4/6, 6/6, twelve nonempty rendered meshes and three-round statistics. Captures: Temp/ResultPanel/Result-4-of-6.png and Statistics.png. No console errors/warnings during feature verification. Physical devices and Player build untested.
- Added docs/Classification-Results.md. Existing unrelated working changes were preserved. Initial scene backup is Temp/ResultPanel/1_Game.before.unity. Play Mode was stopped after clearing simulated cards. No commit created.

## 2026-10-21 14:57 KST — Codex

- 작업 요약: 사용자가 이번 요청 대상을 Thai_04 프로젝트로 명시 승인했다. 기존 RFID 테스터의 중앙 리더 입력은 GamePanel/RFIDImageBox에, AI·Human 각1~3 리더는 각 ImageGroup의 같은 순번 Image에 표시하도록 연결했다. _PROTO의 AI/인간 각3장과 동일 Sprite를 썸네일로 사용한다. 카드 이동·교체·제거·모두 내리기·패널 재진입·미등록 카드의 표시를 처리하고 기존 ContentGate 차단 규칙을 따른다. Codex가 새 표시 스크립트·Scene 연결·여섯 이미지 임포트 설정을 단독 담당했다.
- 변경 파일: Assets/Scripts/Core/RfidCardImageCatalog.cs 및 meta, Assets/Scripts/Core/RfidImagePanel.cs 및 meta, Assets/Scenes/1_Game.unity(GamePanel 컴포넌트 추가 및 Image7개 참조), Assets/Resources/_PROTO/AI1.png.meta·AI2.png.meta·AI3.png.meta·인간1.png.meta·인간2.png.meta·인간3.png.meta(Sprite/Single·밉맵 해제·NPOT 리사이즈 해제), docs/Rfid-Image-Preview.md, CHANGELOG.md, HANDOVER.md. PNG 원본·기존 GUID·RFID 입력/테스터·ContentGate·HelpPanelController 소스는 변경하지 않았다. 작업 전 사용자 미완료 Scene·설정·소스·에셋을 보존했다.
- 검증 결과: Unity 2022.3.62f3 AssetDatabase.Refresh(ForceSynchronousImport) 후 컴파일 완료·Console error0 확인. 실제 1_Game Play Mode에서 실제 RfidTester.DropCard에 리더 좌표를 전달해 42개 카드/리더 조합을 포함한 443개 조건을 자동 검사해 모두 통과했다. 정확한 Sprite·빈 슬롯·비율 보존·반복 보고·이동/교체/개별 제거·모두 내리기·패널 비활성/재진입·미등록 UID를 확인했다. 별도로 HelpPanel 차단 중 이미지 갱신 금지·모두 제거 시 차단 해제·후속 카드 표시를 확인했다. Game View 캡처 Temp/RfidImagePreview/GamePanel.png로 중앙 이미지와 분류 썸네일을 육안 확인했다. Unity Test Runner/NUnit 테스트 결과와 구분하며 새 테스트 패키지는 추가하지 않았다. Scene 저장은 Unity API로 수행했고 시작 백업과 YAML 노드 비교에서 기존 GamePanel GameObject 노드1개 변경과 신규 MonoBehaviour 노드1개만 확인했다. 0_Idle Scene·URP Renderer 설정은 작업 시작 백업과 바이트 동일, 여섯 이미지 기존 GUID 유지 확인. 모의 카드를 모두 제거하고 테스터를 다시 표시한 뒤 Play Mode를 종료했다. 이번 소스·문서의 공백 검사 통과. 전체 git diff --check는 작업 전부터 있던 0_Idle.unity의 Unity YAML 빈 값 뒤 공백으로 실패했으며 해당 사용자 변경은 수정하지 않았다. Play 종료 시 기존 ApiManagerBase가 apiUrl 미설정으로 종료 로그를 생략한다는 오류1건을 남겼고 새 표시 기능 오류와 구분했다. 활성 Scene이 dirty로 표시됐지만 저장본과 임시 스냅샷의 전체 YAML 노드가 동일함을 확인한 후 다시 저장해 dirty를 해제했다.
- 미해결 사항: 실제 마우스로 드래그하는 수동 시험·실제 RFID 리더·Windows Player 빌드·장시간 실행은 미실행이다. 요청 범위는 이미지 표시이며 분류 정답 판정이나 실제 UID 매핑은 추가하지 않았다. 기존 사용자 미커밋/미추적 파일과 이번 변경은 커밋하지 않았다.
- 다음 권장 작업: 1_Game을 재생해 F9 테스터의 여섯 카드를 중앙과 AI/Human1~3에 직접 드래그하고 우클릭 제거·모두 내리기를 확인한다. 실제 RFID 연결 시 기존 RfidInput.NotifyPlaced/NotifyRemoved 경로를 사용한다.
