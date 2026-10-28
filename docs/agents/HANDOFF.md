# 2026-10-27 TEMP-MASTERPIECES / COVER — 완료(미커밋 / 소유권 해제)

- 사용자요청: ImageMask비율의명화6종으로기존낙서대체,임시임을명시한프로젝트폴더에저장/연결,추가입력으로좌우·상하여백없이미세크기조정. Codex단독/기존미커밋보존/커밋·push없음.
- 생성/자료: Assets/Resources/TEMP_MasterpieceCards에AI3개(내장image_gen원화:강변야경/붉은스카프인물/붓꽃·살구정물)와Human3개(Met공식API isPublicDomain=true자료:VanGogh1887자화상/1890Roses/Manet1874MonetFamily). AI정물1회구도보정,실제작품에는생성형편집없음. 전체프롬프트PROMPTS.json,작품·다운로드·CC0정책출처SOURCES.json,임시폴더README.md기록. 원본래스터는온전히보존하고Unity Sprite.rect만표시용crop. 정식인증/감별기능이아닌기존6카드임시콘텐츠교체임.
- Sprite: Sprites/AI_1~3/HUMAN_1~3.asset6개에실제ImageMask비율0.595934를정확히적용. Texture는원본파일공유(Max2048/ReadWriteoff/mipmapoff),늘이거나찌그러뜨리지않음. Resources.Load<Sprite> 경로만RfidCardImageCatalog에서교체. 기존_PROTO파일/metaSHA모두동일/보존,리더ID/정답타입/통계저장형식·기존응답파일보존. 이전ID의기록을새작품전용기록으로자동분리·초기화하지않았으며README에명시.
- 여백조정: Game 중앙1+리더6+통계6=13사진에UnityUI AspectRatioFitter.EnvelopeParent와localScale1.01을적용. Sprite비율유지Cover+1%overscan,기존마스크모양/위치불변. builtinFitter는mode를먼저설정하고aspectRatio0.595934를나중에설정하여편집기None모드가중앙의기존ratio0.6138로값을되돌리는문제를방지. 카드pop의원래크기도1.01로캐시되어연출완료후여백없음.
- 발견·수정: 사용자씬에서점수/안내의이전TMP오브젝트가제거되어scoreText/instructionText참조null이었고필수검사가확정을막고있음. ClassificationResultPanel에서작품점수presentation이있으면이전TMP는선택사항으로처리하고출력부도존재할때만갱신. 객체를다시만들거나사용자UI배치를되돌리지않음. 기존필수panel/mark/점수표시보호·실제저장·입력가드는유지.
- 실제검증: Unity2022.3.62f3 컴파일CS오류0/기능콘솔Error0. 최종Play29PASS/0FAIL:6고유Sprite/6공유원본Texture/정확mask비율,중앙6종전부실제표시+Cover범위/비율,리더6종/순차pop중입력거부/6개사진Cover/역재생동일tween/빠른재인식,6점판정/1회임시저장/통계6사진동일Sprite+Cover/실제응답률/중복저장거부/전체제거재시작/운영응답전후해시동일. 모두timeScale0실제Play,6종실제화면캡처육안확인. 13사진모두Rect비율이Sprite와일치하며마스크boundingbox보다네방향0.2UI단위이상확대되는것검사.
- 실패이력분리: 최초play-results.json의5개후반검사실패는제거된TMP필수검사문제,보완후play-final-results.json의중앙Cover6FAIL은AspectFitter초기설정순서문제. 최종cover-final-results.json29개모두PASS로해결확인. 소스검증최종판과구분하여삭제하지않음.
- 증거 Temp/MasterpieceCards/{Verify.cs,BuildSprites.cs,cover-final-results.json,final-audit.json,legacy-hashes.json,Central-Preview.png,Game-Masterpieces.png,Statistics-Masterpieces.png,Game.before-cover.unity,RfidCardImageCatalog.before.cs,ClassificationResultPanel.before-optional-text.cs}. 최종1_Game편집모드/dirty=false/Play정지. 기존_Source API/생성서버/운영응답전송/PC종료/스케줄러발동없음. 내장image_gen호출은이번사용자이미지제작요청에따른것이며Unity생성API검증과다름. 실물RFID/물리버튼/Windows빌드/장시간메모리미검증. 실제명화원본정체성은공식소장처metadata기준.

---

# 2026-10-27 GAME-MASK-ALIGN — 완료(미커밋 / 소유권 해제)

- 요청: GamePanel의7개사진마스크를사용자배경의프레임내곽과정확일치. Codex단독/현재미커밋보존/커밋·push없음. 사용자dirty1_Game씬을Temp/GameMaskAlignment/Game.editor-before.unity로먼저사본저장하고현재사용자변경을유지한상태에서적용.
- 수정: 1_Game의AI/Human 각3개와중앙RFID 마스크의Rect/정규화anchors/실제내곽메시. AI/Human 그룹2개는배경에stretch되도록설정. 임의radius9대신배경PNG의실제색경계/서브픽셀등고선을추출: 작은슬롯40점×6,금색중앙317점의정규화outline과삼각형을씬직렬화. RoundedRectMaskGraphic에선택적backgroundContour/backgroundTriangles지원추가(기존일반라운드경로유지). 사진의Sprite/비율/리더연결/연출/배경PNG내용/다른씬은변경하지않음.
- 1920×1080 원본픽셀좌표: 작은영역 x=229.01,420.01,611.01,1164.01,1355.01,1546.01 / y535.52 / w145.98,h244.96. 중앙 x830.5213,y398.20314,w259.39442,h422.58819. 크림색/베이지스트로크색구분(작은영역51%내부커버리지)과중앙저채도내부연결영역으로실제모양추출. 0.02px이하단순화/작은영역0.001px단순화,중앙의장식곡선/손그림미세곡률포함.
- 실제검증: Unity2022.3.62f3 컴파일오류0/기능오류0. 7개를단색진단채움으로실제Stencil렌더후1920×1080캡처를추출참조비트맵과픽셀비교:7/7 mismatch0,IoU1.0. 최초작은마스크의GPU경계반올림1줄137픽셀차이는51%경계및곡선정밀도로보정하고최종7개전부0확인(초기결과별도보존). 이것은추출한내곽참조와의기준해상도검사이며모든배율의안티앨리어싱값동일을주장하지않음.
- 1920×1080/1200×1000/2560×900 패널형상에서배경native좌표로역변환해모든contour정점확인:최대오차0.0001221nativepixel. 사진별앵커는배경의정규화좌표를따라가므로비율변경에도정렬유지. 5개Play조건PASS,기존일반라운드fallback53정점/156인덱스확인PASS. fallback검증의첫reflection은기반오버로드로AmbiguousMatch가나서DeclaredOnly로수정후검증,제품런타임오류와구분.
- 보존감사: RectTransform변경9개(그룹2+마스크7),Image컴포넌트변경0,사진보존. 선택적contour가없는다른기존마스크는종전round메시사용. 새이미지/유료플러그인/런타임AssetDatabase의존추가없음. 임시진단cyan채움은Play에서만사용하고종료로원상복구. API/응답저장/PC종료/스케줄러발동없음.
- 증거 Temp/GameMaskAlignment/{Game.editor-before.unity,RoundedRectMaskGraphic.before.cs,extract.py,contours.json,expected.png,Verify.cs,aligned-native.png,aligned-square.png,render-comparison.json,render-comparison.initial.json,coordinate-errors.json,play-results.json,fallback-result.json,final-audit.json}. 최종1_Game편집모드/dirty=false/Play정지. Windows빌드/실물장치/다른해상도전체raster비교는미수행. 해당배경이미지내용자체를바꾸면다시내곽을보정해야함.

---

# 2026-10-27 TUTORIAL-CARD-ONLY — 완료(미커밋 / 소유권 해제)

- 최종 수정: 전체 UI투명화 제거. 첫회후 해당 이동카드만0.5초fadeout→안보이는상태에서원위치준비→원본Card1 CanvasGroup을명시적0→1로0.5초fadein. 복원된카드가즉시나타나는경로없음. 기존1초복원/75%속도/2회는유지. 보드/배경/다른5카드alpha를트윈으로변경하지않음. 가이드/장식은복원중유지하고다음회시작시cue만초기화.
- 2회차는정확안착순간부터0.5초동안놓인상태유지(장식pop시간포함)한후IsComplete/자동Game진입. 배속과독립된실시간0.5초이며장식완료뒤필요한나머지hold를추가함. finalArrivalHold=0.5/ReplayResetDuration=1/PlaybackSpeed=0.75/PlayCount=2. 총튜토리얼약11.527초(패널/씬전환제외).
- 수정 TutorialCardPresentation.cs,0_Idle직렬화와관련문서. obsolete replayFadeGroups참조/검증제거. 기존중립CanvasGroup컴포넌트는사용자UI용으로그대로두되코드가alpha를변경하지않음. 사용자RectTransform/이미지/언어입력/분류로직/다른씬미변경. 원본프로젝트미접근/미수정. 커밋/push없음.
- Unity2022.3.62f3실제컴파일오류0/기능콘솔Error0. Play15PASS/0FAIL:75%·2회·타임라인/보드alpha1유지/다른5장상시표시/이동카드fadeout/복원카드중간alpha실제샘플/fadein/약1초복원/2회정확안착/최종도착0.5초정지/연타·조기이동없음/자동Game/복원중취소·재진입·중복proxy/늦은콜백정리. 전부timeScale0. 실제프레임복원1.003초,도착유지0.516초(프레임관측오차포함). 초기검증코드Mathf.Abs(double)오기는Math.Abs로수정한후실행했으며프로젝트컴파일오류와구분함.
- 증거 Temp/TutorialCardOnly/{Before.cs,Before.unity,Editor-before.unity,Verify.cs,results.json,frames.json,timing.json,audit.json,CardOnly-Fade.png}. 기존전체fade검사기록은과거구현이며이번결과로재사용하지않음. 최종0_Idle편집모드/dirty=false/Play정지. 운영API/생성/응답저장/PC종료/스케줄러발동없음. 실물장치/Windows빌드미검증.

---

# 2026-10-27 TITLE-TUTORIAL — 완료(미커밋 / 소유권 해제)

- 사용자 최종요구: Title 짧은1=Thai/2초유지=English(언어는로그만,영문UI유지), Tutorial 화살표→가이드→카드확대이동/정확안착→장식팝. 전체튜토리얼75%속도로2회. **첫회 직후 1초 동안 페이드로 초기상태복원(대기1초 없음)**, 두번째 완료후 자동Game진입. Codex 단독구현/자체검증, 기존미커밋/사용자UI배치보존, 커밋/push없음.
- EntryController: 키보드1/NumPad1 held와 공개SetPhysicalButtonPressed(bool) down/up을 합산. 짧은입력은release에Thai,2초도달은release없이English를한번로그. 경계release도English,유지/OS반복/긴입력release로추가진입없음. 재진입/포커스변화/전환중입력은취소·release가드. 공개PressButton1은완료된짧은입력호환. SelectedLanguage만기록하며이미지교체로더없음. 에디터의Titlefalse/Tutorialtrue표시는보존하고실제Play첫화면은Title로초기화.
- 신규 TutorialCardPresentation: 기존튜토리얼Image들명시참조. 기본타임라인:Arrow 0.25in/0.2hold/0.2out×2→꺼짐;Guide동일2회중마지막out생략→alpha1켜짐;제자리lift8%→0.9초이동하며확대/작은호→6%높은hover크기에서정확크기로안착→동일완료시점장식2개0.2초OutBack강조. 이기본타임라인전체timeScale0.75,실제한회약5.147초. 언어hold2초·복원1초·공통화면전환시간은이배속과독립.
- 반복최종:첫회직후연출영역0.5초fadeout→완전투명상태에서카드/가이드/장식복원→0.5초fadein→두번째. Board/EquipBoard/Arrow CanvasGroup들과이동카드alpha를같이페이드하여보이는위치순간이동없음. 1초대기후빠른복원했던중간요구는최종요구로대체. 두회+복원총약11.293초,두회완료후에만공통SceneAsync로1_Game자동이동. 버튼을계속누르고있어도자동이동은막히지않음. 실패자동무한재시도없음.
- 카드정렬:대상기존112×169 PNG에서그림자를제외한opaque본체100×157(x2..102/y10..167,bottom-left)을정규화Rect로보정. 목표CardLayoutImage의실제Rect/pivot/scale기준으로최종scale(현재약1.4/1.3822)및중심오프셋계산,안착후본체4모서리일치. 원본6장HorizontalLayout은건드리지않고Card1 CanvasGroup만투명화,같은Sprite를빌린런타임Image1개를이동용으로재사용. 새그림/Texture복사없음. Disable/재진입/씬이탈에서트윈·대기CTS·alpha·원본카드표시·장식/진행횟수정리.
- Inspector: 0_Idle Canvas/ClassificationEntryController(언어입력), Canvas/TutorialPanel/TutorialCardPresentation(대상참조/본체보정/이동시간/Playback Speed0.75/Play Count2/Replay Reset Duration1). 기존RectTransform18개전부사용자미저장씬사본과동일,Title/Tutorial에디터활성상태동일. 편집전dirty사용자씬은Temp/TitleTutorial/0_Idle.editor-before.unity로먼저사본보존후현재연결과함께저장했다.
- 로그: 프로젝트작성Debug.Log/Warning/Error영어정책을CLAUDE에추가. 새언어로그는[ClassificationEntryController] Starting Thai/English mode...영문. RfidCardImageCatalog의누락경고에서한글리소스경로보간을없애영문화. Assets/Scripts의Debug.Log*한글문자검색매치0. 외부패키지/유료플러그인소스는수정하지않음.
- 실제검증: Unity2022.3.62f3 컴파일CS오류0. 최초입력/단일연출검사23PASS/0FAIL(물리입력공개down/up,NumPad1경계상태수동주입,로그한번/길게유지/포커스/취소,화살표·가이드·이동·안착/장식순서,scale증가,source고정,재진입/Disable,timeScale0). 223실제프레임샘플,이동중maxscaleX1.48379,안착4모서리오차<0.01검사통과. 초기키보드큐검사는에디터unfocused+ResetAndDisableNonBackgroundDevices로이벤트가제외돼미진입했으며제품오류로간주하지않고공개물리입력및명시InputSystem.Update로라우팅검사를보완했다(초기실패results.json별도보존).
- 최종반복/복원검사16PASS/0FAIL:75%배속/2회시간/첫회직후즉시복원/실제1초fade복원/투명시점초기화/카드·보드동시fade/두번안착정렬/첫회자동진입없음/2회후자동Game/계속유지된버튼과무관한자동진입/복원중Disable/reentry/중복이동Image없음/latecallback정리. 두단계모두timeScale0실제Play에서진행. 기능중콘솔Error0,종료시기존apiUrl미설정exit-log생략은구분. 실제장치/Windows빌드/설치PC성능미검증. API/생성/운영응답저장/종료/스케줄러없음.
- 증거: Temp/TitleTutorial/{Verify.cs,final-results.json(23),frame-samples.json,language-logs.json,VerifyReplayReturn.cs,return-final-results.json(16),return-final-frames.json,audit.json,Card-In-Flight.png,Card-Placed.png,Replay-Fade.png,0_Idle.editor-before.unity}. 중간1초대기버전replay-results.json은최종복원검사와구분. 최종0_Idle편집모드/dirty=false/Play정지/timeScale1. 사용자세부UI배치유지.

---

# 2026-10-27 UI-TRANSFER-02 — 완료(대략적 UI / 소유권 해제)

- 최종 사용자 지시: 기존 분류·점수·통계를 유지하고 대응 UI/연출만 적용. 다른 프로젝트의 이미지는 쓰지 않으며 사용자가 원래 추가한 이미지로만 구성. 구체적인 UI 조정은 사용자가 하므로 세부 배치는 대략적으로 마무리.
- 정정: 앞선 반입 판단은 오류였다. asset-audit의 작업전 exists=false인 UI 파일199개(PNG95개/meta/재질/폴더meta)를 제거했다. 기존 사용자 UI31개 PNG와 GUID 보존, 외부 프로젝트 PNG0개/중복GUID0개. Temp/UITransfer/removed-source-assets.json 및 target-only-final-audit.json 확인. 이전 기록의 이미지95개 반입·126개목록은 과거 상태이며 현재와 다름.
- 대상 main/e8f1f58, Codex 단독 구현·자체검증. 기존 미커밋/미추적 파일 보존. 원본 Thai_03은 코드 참고만 했고 수정 없음. 패키지/Unity버전/설정/API/통계 저장모델/실물 입력 통신 변경 없음. 커밋/push 없음.
- Game: RfidImagePanel.Use Sequential Front Card Pop=true. 기존 Ai1..3/Human1..3 고정 리더 위치를 유지(음식 프로젝트처럼 첫 빈 슬롯으로 재배치하지 않음). 앞면만 순차0.3초 OutBack/1.70158, 제거 시 동일 트윈 현재위치 역재생, 재인식 시 정방향 재개, 교체 카드는 이전 이미지가0에 도달한 뒤 배정. 대기 중 제거 취소/전체해제/실행 중 불값 변경/패널 재진입/Disable에서 트윈 정리. false는 기존 즉시 사진 미리보기 방식이며 음식 뒷면 카드 이미지는 사용하지 않는다. 중앙 리더는0.25초 사진페이드. 실제 RfidInput 상태는 UI가 지우지 않음.
- Result: 기존 대상0~6개정답/정답/오답/정답판/카드프레임/버튼안내 그림을 ClassificationPresentation에 연결. 점수0.25초 페이드→피드백 카드 시작간격0.2초/각0.3초 OutBack→버튼안내페이드. 기존 점수·정답판정/저장은 유지. Canvas 컨트롤러가 공통 전환 및 입력잠금 소유. TryAgain은 대상 아웃트로팝업을 공통0.25초페이드→0.3초팝으로 띄우며7리더 전체제거 후 Game으로복귀.
- Statistics: 기존 대상 비교패널배경/비교판을 대략배치, 여섯 이미지별 실제 로컬응답비율을 기준선에서1.2초/카드간격0.12초 OutCubic 상승. 원래 가로누적바를 실제비율의 AI/Human 두 세로막대로 표현했으며 저장/이미지 정체성/응답횟수/중복 저장방지는 유지. 대상 게이지2개만 border(12,12,12,12)/Sliced로 지정하고 RectMask2D로 기준선 clipping. 빈응답은 가짜50/50으로 채우지 않음.
- Idle: 기존 Title/Tutorial 그림·배치를 유지하고 Canvas/ClassificationEntryController의 숫자1·숫자패드1·공개PressButton1()을 Title→Tutorial→Game에 연결. 패널0.5초퇴장/0.5초등장, 씬은 완전암전 후 로드·표시준비·해제. Game을 빌드씬 목록에 추가. GameManager의 무입력 첫화면 복귀도 공통 SceneAsync로0_Idle에 연결. 실제 타임아웃 설정은 켜지 않았음.
- 목록: Lang_En/Th 대상31이미지/2씬53bindings로 재생성, 점수배열·정오답 Sprite도 포함. Th placeholder/영어fallback 준비상태, 실제언어교체로더·롱홀드언어선택 없음. 음식생성/로딩/설문/갤러리/영상·학습후속화면은 추가하지 않음.
- 실제 검증: Unity2022.3.62f3 기존에디터 컴파일 CS오류0. 전체흐름 최종30PASS/0FAIL + 보완회귀 최종17PASS/0FAIL. 실제 InputSystem 가상키보드 Digit1→Tutorial/NumPad1→통계, Idle→Game 씬전환, timeScale0, 순차앞면/퇴장역재생 동일트윈/재인식/교체대기/대기카드제거/불값스왑/패널재진입/빠른30회재인식/전체정리, 7개점수그림/정오답그림/연출중입력거부, 실제100/0·50/50·빈응답, 그래프Disable/reentry, 전환취소/소유자비활성복구/팝업대기재진입/실제카드제거후복귀 확인.
- 발견·수정: 1차전체30검사 중 대기카드제거 후 pending 플래그가 남는1FAIL → null/null동기화 시 대기취소로 수정 후 전체30/30. 1차회귀17검사 중 재진입한 제거팝업에서 결과연출이 물리제거완료를 지연시키는1FAIL → 대기상태에서 결과인트로 재시작하지 않고 물리제거 확인을 연출입력가드보다 먼저 처리, 최종회귀17/17. 초기실패기록은 삭제하지 않고 별도파일에 보존.
- 검증데이터는 Temp/UITransfer/phase2*-responses.json만 사용. 전체흐름에서 운영응답파일 전후해시 동일 확인. 실제 API/생성/운영응답저장/PC종료/스케줄러 발동 없음. 기능검사 중 콘솔 Error0; Play종료 시 기존 ApiManagerBase의 apiUrl미설정 exit-log생략 메시지는 기존항목으로 구분.
- 증거: Temp/UITransfer/{Phase2Before,phase2-final-live-results.json(30),phase2-final-edge-results.json(17),VerifyTargetFlow.cs,VerifyTargetEdges.cs,target-only-final-audit.json,Target-Title.png,Target-Tutorial.png,Target-Game.png,Target-Result.png,Target-Statistics.png,Target-RemovalPopup.png}. 화면캡처 육안 확인. 최종1_Game 편집모드/dirty=false/Play정지/timeScale복구, 외부PNG0/대상31개유지/언어목록53참조존재/누락컴포넌트0.
- 남은 조정: 사용자가 RectTransform/크기/여백/정밀배치/색·타이포를 후속조정. 현재 통계 요약문 등 배치는 대략적이며 최종디자인 QA 완료로 주장하지 않음. 프로토타입 사진은 기존 가로비율을 유지하므로 세로프레임 안에 여백이 있음. 실제RFID/물리버튼/Windows빌드/설치PC성능·장시간 메모리/타임아웃 이벤트 발동은 미검증. 라이브 장치연동을 완성했다고 보고하지 않음.

---

# 2026-10-27 UI-TRANSFER — 공통 부분 적용・범위 답변 대기

- 기준 main/e8f1f58. Codex 단독 구현·자체검증. 원본 Thai_03의 현재 작업폴더만 읽고 원본 수정 없음. 커밋/push 없음. 대상 AGENTS.md/DEVELOPMENT_GUIDELINES.md 없음; CLAUDE.md, HANDOVER.md, docs/agents/HANDOFF.md와 기능 문서 확인. 원본 필수 문서/최신 FEAT-039 기록 확인.
- 중요 차이: 대상 Thai_04는 RFID 7리더 사진 분류/점수/누적 응답 통계 프로젝트이며 원본의 음식 생성/로딩/Finish/설문/갤러리/영상 화면이나 데이터 모델이 없다. 사용자가 기존 분류 흐름에 대응 연출만 적용할지, 새 화면도 추가할지 질문한 상태. 해당 화면 이식·카드 첫 빈 슬롯 방식은 범위 결정 전 진행하지 않음. 전체 요청 완료 아님.
- 적용: 원본 UI PNG 95개/meta 및 재질 반입. 대상 기존 UI 31개와 English.meta 보존. GUID 충돌0/중복0. ScreenTransition/PopupAnimation/RoundedRectMaskGraphic 및 meta 이식. 재질의 원본 전용 폰트 atlas GUID는 대상에 없으므로 기존 대상 Inter SDF atlas로 Unity 에디터에서 재연결. 유료 플러그인/생성 사진/응답/설정/영상 복사 없음.
- 실제 연결: ClassificationResultPanel의 Game→Result→Statistics 및 돌아가기/재시작에 0.5초 퇴장 후 0.5초 등장 InOutSine 공통 전환. Canvas 컨트롤러가 수명 소유, 연타/중복 저장 방지, 취소 후 이미 저장한 결과 복구. Scene 1_Game에서 StatisticsPanel을 Canvas의 형제로 이동(기존 배치 보존)해 부모 페이드/비활성 영향을 제거. HelpPanel/BlackBox를 추가하고 HelpPanelController.blackBox에 연결; 기존 BoardClearPopup은 공통 0.25초 페이드→0.3초 OutBack 1.70158. 실제 RfidInput 상태를 UI 비활성화에서 지우지 않음.
- 이미지 목록: 대상 실제 에셋/SerializedObject를 읽어 Lang_En/Th.json 생성: 126이미지(원본95+대상31)/현재2씬16bindings. 원본147연결을 대상 연결이라고 복사하지 않음. Th는 영어 fallback placeholder, 런타임 언어 로더 없음. 원본95그림은 반입 상태이며 현재 분류 화면을 음식 UI로 교체하지 않았다.
- 실제 검증: Unity 2022.3.62f3 기존 에디터에서 컴파일 CS 오류0. Play Mode 공통헬퍼13 PASS/0 FAIL, 실제 1_Game 분류/HelpPanel18 PASS/0 FAIL. 퇴장 중 원패널 유지/등장 전 숨김, 연타 거부, timeScale0, 취소/잠금해제, popup 순서/완료/소유자해제/재진입, RFID 상태 보존/마지막 제거 해제, 중복 저장 억제, 중간 Disable→재활성 복구. 임시 통계 경로 Temp/UITransfer/isolated-responses.json만 사용. 테스트 스크립트 첫 실행의 RfidInput.Count 오기1건은 인메모리 검증 코드 컴파일 오류이며 IsBoardClear로 수정 후 실행 완료; 프로젝트 컴파일 오류 아님.
- 콘솔: 기능검증 중 새 Error0. Play 종료 시 기존 ApiManagerBase의 apiUrl 미설정으로 exit log 전송을 건너뛰는 Error1건 확인. 외부 API 요청/운영응답 기록/종료/스케줄러 발동 없음.
- 증거: Temp/UITransfer/{Before,asset-audit.json,common-play-verification.json,classification-play-verification.json,VerifyClassification.cs,final-audit.json,Statistics-AfterTransition.png}. 통계 스크린샷 육안 확인(기존 F9 테스터가 상단 일부를 가림). 원본95 PNG/meta 바이트동일, 기존 대상 UI 전부/0_Idle 작업전 사본과 바이트동일 확인. Scene은 Unity API로 저장, 최종 편집모드 1_Game dirty=false.
- 미검증/남은 항목: 원본 음식카드 순차등장/동일트윈 역재생/재인식 및 생성 입력 가드는 아직 대상에 미구현; 로딩/결과사진/갤러리/설문/영상/Idle 전환 및 원본과 동일 배치는 대응 화면 범위 결정 후 작업. 공통 SceneAsync는 코드 이식만 했고 실제 대상 씬 전환에 아직 연결/검증하지 않음. RoundedRectMaskGraphic도 코드 이식만 했고 기존 사진에 아직 미연결. 실제 키보드 이벤트/물리버튼/RFID 장치/Windows빌드/장시간 성능 미검증. 운영 통계 원본의 전후 해시는 수집하지 않았으므로 바이트동일 검증이라고 보고하지 않음(테스트는 임시 저장소 주입).

# HANDOFF

Claude가 Antigravity(agy)에 맡긴 검증 기록. 새 항목을 맨 위에 추가한다. 항목 형식은 공통 스킬 `handoff` 를 따른다.

---

### [2026-10-16 10:59] Claude → Antigravity · HuliacDev Template 26.10.16-1 업데이트 (main)
- 요청: Template을 최신으로 올리고, 바뀐 내용과 이 프로젝트에 걸리는 것이 있는지 확인.
- 변경: packages-lock.json 고정 커밋 1c5e127(26.10.12-1) → a036657(26.10.16-1, 항상 위 옵션 `alwaysOnTop`). Settings.json에 `"alwaysOnTop": false` 추가, bundleVersion 26.10.16, CHANGELOG에 10-12·10-16 운영자 영향 항목 추가.
- Claude 확인: 배치 모드로 a03665700c 패키지 받기·스크립트 컴파일, error CS 0, return code 0. 프로젝트 코드는 `ConfigureWindowFocus`·`ConfigureProjectMessageBrokers`·`GetTextWithRetryAsync` 를 override·호출하지 않음.
- agy (gemini-3.8-flash-high, 2묶음):
  - 프로젝트 코드와 바뀐 Template API 충돌, Settings.json 키 이름·기본 동작 → 통과
  - CHANGELOG 사실 일치, 배치 모드 로그, lock 해시·bundleVersion·README → 통과(로그 근거 줄 440·866 직접 확인)

## 2026-10-27 UI-TRANSFER-02 — 진행 중
- 사용자 확정: 기존 분류 흐름 유지, 대응 UI·연출 적용. 음식 생성/설문/갤러리/영상 화면 추가 없음. 기존 리더별 위치 의미를 보존한 순차팝/역재생, 결과/통계 전용 artwork 및 결과 카드/실제비율 연출, Idle/Tutorial 공통 전환 연결 예정. Codex 단독, 기존 파일 Phase2Before 백업.

## 2026-10-27 TITLE-TUTORIAL — 진행 중
- 사용자 요청: Title 짧은1=Thai/2초hold=English 로그만, Tutorial 화살표2회/가이드2회(마지막fadeout없음)/카드확대이동·정확안착/장식팝. Codex 단독. 현재0_Idle 미저장 사용자UI배치/Titlefalse·Tutorialtrue를 보존하며 에디터API로 현재씬사본 백업 후 연결. 수정예정 EntryController, 새TutorialCardPresentation, Idle씬연결, 로그정책문서·로그영문화, CHANGELOG/HANDOFF.

## 2026-10-27 GAME-PHOTO-FADES — 완료
- RfidImagePanel의 순차 확대/역재생을 제거하고 7개 사진을 고정 크기에서 독립적으로 0.25초 페이드인/아웃하도록 변경. 기존 마스크·비율·1% overscan 유지.
- 1_Game의 ButtonInstruction에 CanvasGroup 연결, 초기 비활성. AI/Human 6개 분류 리더가 모두 알려진 카드를 인식하면 0.25초 등장, 하나라도 해제되면 0.25초 퇴장 후 비활성. Center 제외, 정답 여부 무관. 빠른 재인식은 현재 투명도에서 반전하며 오래된 콜백을 취소.
- 실제 Play 모의 입력 최종 40/40 통과: 각 6개 리더 해제, 빠른 재인식, 크기 고정, 미등록 카드, 패널 수명, 점수 6점 및 1회 저장. Temp/GamePhotoFades/final-results.json과 final-frames.json 참조. 최초 실행의 첫 프레임 타이밍 검사 1건은 준비 프레임 후 재검증하여 통과. 실통계 해시 동일, 외부 API 호출 없음.
- Unity 컴파일/기능 오류 없음. 종료 시 기존 apiUrl 미설정 로그와 MCP 중복 명령 억제 경고만 확인. Play 종료. Tutorial/결과 연출·다른 프로젝트 이미지·실물 인식 상태 변경 없음.
# 2026-10-27 RESULT-STAMP — 완료

- FeedbackArtwork 원래174×274/scale1 유지. 크기 트윈 없음, 위치12단위·회전4도에서 빠르게 찍고 작은 반동 후0.22초에 제자리 정착. 6개를0.2초 간격 재생. PhotoMask와 형제이며 maskable=false로 사진 마스크에 잘리지 않음.
- 결과 사진6개는 제출 카드 순서로 동결. 기존 카드PNG의 안쪽경계16.01,17.01,145.98,244.98/42정점 추출하여 정규화 마스크, EnvelopeParent+1%overscan. 사용자 이미지/원래 Result/Feedback 배치·크기 보존.
- 전체 피드백 종료→정답수 ScoreArtwork0.25초fade→ButtonInstruction0.25초fade. 안내 완료까지 ClassificationPresentation.IsAnimating=true로 숫자1/공개버튼 입력 무시. 삭제된 ResultMarkGraphic은 presentation이 있으면 선택사항. hasResult=false 시작 시 Game만 활성화하여 편집기에서 여러 패널이 켜져 있어도 결과대기와 충돌하지 않음.
- 최종 Play82/82PASS와 실제시간 프레임검증 통과. 4정답 실제제출/이미지/1회격리저장,0~6점Sprite매핑,고정스케일/원위치복귀/마스크Cover/인식해제후사진동결/버튼가드/취소재진입 확인. Temp/ResultStamp/final-results.json,realtime-result.json,Final-4.png. 첫검증은 다중활성 시작 문제를 발견하여 수정 후 재검증. Goto기반 테스트 외 실제재생으로 안내활성과 연출순서도 확인. 실통계 해시 동일, 외부API 호출 없음, Play종료. 컴파일/기능Error0; MCP재연결경고와 기존apiUrl종료로그는 별개. 실제RFID장치/Player빌드는 미검증.
# 2026-10-27 HORIZONTAL-STATISTICS — 완료

- 요청 시안 E04-S05에 맞춰 기존 StatisticsPanel의 ComparisonBoard를233,-372로 배치. 기존 비교판PNG의AI/Human표기를 그대로 사용. 각 카드 사진 마스크는PNG내곽42정점(첫칸81.01,63.01,166.98,279.67885515/이후225간격)을 추출해 정규화. 기존 사진EnvelopeParent/1%overscan 유지.
- ImageClassificationChart를 수평 누적으로 변경.174×24/반경12인 단일Mask, Human풀배경+AI덮개로 끊김/내부둥근틈 없이 경계 이동.0.5초InOutSine,50/50에서실응답비율로이동하며정수퍼센트상보합100유지. 데이터없으면중립트랙/—,0%·100%도정상. 원본게이지PNG를비틀거나따로잘라붙이지않음.
- 불필요 ImageLabel/ResponseCount/DistributionBar/Percentages/AiBaseline/HumanBaseline 제거. 숫자12개만별도TMP, TMP기본폰트weightTable[7].regularTypeface의Inter_18pt-ExtraBold SDF/font18직접사용. AI/Human글자와하단안내는프로젝트의기존이미지. 하단 응답결과비교패널버튼안내.png는StatisticsPresentation이6그래프동시0.5초종료후0.25초페이드인. 입력은안내완료까지차단,패널비활성시트윈정리.
- 수정: ImageClassificationChart/ClassificationResultPanel/새StatisticsPresentation+meta/1_Game/docs/CHANGELOG. 실저장형식/실응답/작품ID/RFID/Game·Result·Tutorial연출보존. 학습패널흐름은이번에추가하지않음.
- 검증: Play최종53/53PASS. actualGame→Result→Statistics/격리1회저장/0.5초duration/수치변화·합100/안내순서/입력가드/마스크cover·경계/볼드폰트/빈데이터/재진입/취소/0%·100%/운영파일해시동일. Temp/HorizontalStatistics/final-results.json,final-frames.json,Final.png,render-seams.json. 최초6duration검사는AutoKill후조회해서0반환한검사문제였으며,활성중길이를캡처하도록수정해재검증통과. 실제렌더중앙행6개모두두색외틈픽셀0,분할오차0.2px이하. 기능/컴파일오류0. Play종료/Scene저장. 실제장치/Player빌드미실행.
# 2026-10-27 FINISH-VIDEO — 완료

- 통계 안내 완료 후1/numpad1/공개버튼이 공통SceneAsync로2_Finish/VideoPanel에 진입. finishScenePath를1_Game에저장/BuildSettings에Finish활성추가. 준비/전환/연타가드유지, Finish에복사되어있던불필요ClassificationResultPanel만제거.
- 새FinishVideoPresentation/VideoPlayer/BlackBoxCanvasGroup연결. 기존4번영상_임시.mp4(1080p30/105초)직접참조. 첫프레임준비까지RawImage숨김/black배경/ISceneDisplayReady전환대기. Prepare→Play→첫frameReady에표시/이후frameeventoff. 재생중일시정지포스터없음. unscaled재생/DirectAudio.
- 실제종료에Pause하여마지막3149프레임유지, 공통PopupAnimation.BlackBox0.25fade→RemoveCardPopup0.3OutBack(overshoot1.70158). 사용자팝업restscale/이미지/레이아웃보존. 패널disable전표시·texture분리/이벤트해제/decoderStop. 패널재진입은동일단일RT재사용/blackclear/0번부터prepare. 씬destroy시RTRelease+Destroy. CPU복사/프레임캐시/복수decoder없음. 카드제거후후속전환은이번범위아님.
- 검증36/36PASS:3회진입firstFrame0/동일실프레임hash(white·blackblank해시와다름)/실제프레임진행/종료근처seek후native완주/3149유지/hash불변/blackfade·팝순서·overshoot/단일RT재사용·씬unload해제/실Game→Result→Stats→Finish입력/중복저장없음/실통계hash동일. Temp/FinishVideo/results.json,samples.json,FinalHold.png,Verify.cs. 최초VideoEnded.png는다음리셋프레임이캡처되어FinalHold.png로재촬영·육안확인.
- 기능/컴파일오류0. 원본MP4의WindowsMediaFoundation colorprimaries미지원경고, 검사코드의AutoKill후Duration조회DOTween경고, MCP재연결경고/기존apiUrl로그는별개. 소스영상재인코딩/변경없음. Play종료, 활성Finish/두씬저장. 실RFID/Player빌드미실행.
