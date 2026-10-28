# 대상 전용 UI 이미지 목록

현재 Assets/UI에는 사용자가 원래 Thai_04에 추가한 PNG31개만 있다. 다른 프로젝트에서 잘못 반입했던 이미지95개와 재질·폴더 meta 등199개 파일은 제거했다. 원래 대상 PNG의 바이트와 GUID를 보존했다. 정답/오답 게이지의 border만 Sliced 표시를 위해 (12,12,12,12)로 지정했다.

Lang_En.json / Lang_Th.json은 대상31이미지와 실제2씬53개 Sprite 연결을 기록한 schemaVersion1 준비 파일이다. 정적 Image뿐 아니라 ClassificationPresentation의 점수0~6, 정답/오답 Sprite 배열도 포함한다. 카드 사진은 기존 Resources/_PROTO 카탈로그를 그대로 사용하며 번역 목록에서는 제외한다.

Th는 전체 isPlaceholder=true, fallbackLanguage=En이다. 태국어 그림과 런타임 언어 로더는 구현하지 않았다. assetPath/GUID는 에디터 관리 정보이며 빌드에서 AssetDatabase를 사용할 수 있다고 가정하지 않는다. 런타임 교체가 필요하면 명시 Sprite 참조 목록 등 별도 로더를 연결해야 한다.

대략적인 배치와 이미지 연결만 구현했다. 이후 사용자가 RectTransform/배치/크기를 조정할 수 있다. 공통 코드만 원본에서 참고했으며 음식 생성·설문·갤러리·영상 화면이나 해당 이미지는 추가하지 않았다.

Title의 짧은 입력/2초 유지 입력은 Thai/English 선택 로그와 SelectedLanguage 값만 기록한다. 실제 번역 이미지 교체는 수행하지 않는다.
