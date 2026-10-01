# 달빛 인쇄소 · Moonlit Press

달밤 서재에서 이야기를 쓰는 **Unity 6 리듬 타이핑 게임**. 왼쪽은 실제 3D 학자와 Mixamo 집필 동작, 오른쪽은 지금까지 쓴 글이 쌓이는 펼친 책, 아래는 두벌식 키 입력과 박자선입니다.

[웹에서 플레이](https://wwonnn.github.io/moonlit-typing/) · [GitHub 저장소](https://github.com/wwonnn/moonlit-typing)

## 플레이

- `집필 시작`: 두벌식 키보드의 물리 키를 박자선에 맞춰 누릅니다. 영문 입력 상태에서도 동작합니다.
- `ㄱ → ㅏ`는 `R → K`입니다. 각각 별도로 판정하고, `가`가 완성되어야 책으로 날아갑니다.
- 쌍자음·일부 모음은 Shift와 함께 입력합니다. 합성 모음과 겹받침은 구성 키를 차례대로 누릅니다.
- 화살표나 궤적선은 없습니다. 글자는 본문의 다음 칸에 본문 크기로 착지합니다.
- `ESC` 또는 `Ⅱ`: 일시정지, 판정 보정, 음량, 효과 강도 조절.
- `자동 연주 보기`: 전체 곡과 연출을 감상합니다. 최고 점수에는 기록하지 않습니다.
- 한 곡을 마치면 판정 점수가 책 판매량과 수입으로 이어집니다.

![실제 Unity WebGL 플레이 화면](Documentation/gameplay.png)

## 이번 플레이 빌드

- 기본 **쉬움: 달빛 잔치** — 새 오리지널 곡, 120 BPM, 74초, 28글자·67키. 한 글자를 4박 묶음으로 배치하고 글자·문구 사이에 쉼을 둡니다. 키 간격은 최소 0.5초입니다.
- **도전: 달빛 인쇄소** — 기존 144 BPM, 약 84초, 122글자·301키 채보를 유지합니다. 곡 선택 화면에서 전환하고 최고 기록을 따로 저장합니다.
- 실제 승인 Meshy 학자 메시와 텍스처, 수정된 Mixamo 애니메이션.
- 저채도 달밤 배경, 따뜻한 등불과 푸른 달빛, 카툰 명암 셰이더.
- 자모 키별 판정과 글자 단위 노트 묶음 표시. 쉬움 판정은 ±100/180/240ms, 도전은 기존 ±65/125/185ms입니다.
- Kenney Splat Pack 먹물 튐, 입력 글자 눌림·튀어오름, 가속하며 휘어지는 글자 비행. 착지 좌표·글꼴·크기는 본문과 일치합니다.
- 입력음은 Kenney Impact Sounds의 나무 타격 녹음, 착지는 중간 펀치, 문구 완성은 무거운 펀치 소리로 교체했습니다. 기존 합성 삑 소리는 플레이 중 사용하지 않습니다.
- 키 입력에 의한 카메라·모델·책상 흔들림 제거. 20콤보 단위로 다음 여유 구간에 SittingVictory 모션을 짧게 재생합니다.
- 브라우저 물리 키 이벤트 시각 보정, 반복 키다운 무시, 포커스 손실 시 자동 일시정지.
- 점수에 따른 판매 결과, 최고 기록 저장, 자동 연주 모드.

현재는 쉬움·도전 두 곡을 끝까지 플레이할 수 있는 프로토타입입니다. 다수의 책, 상점 경제, 장기 성장 시스템, 얼굴 표정 블렌드셰이프는 포함하지 않습니다.

## Unity

Unity **6000.0.74f1**, Built-in Render Pipeline, uGUI. `Assets/Scenes/MoonlitStudy.unity`를 열고 Play합니다. 실제 3D 무대와 UI는 런타임에 생성됩니다.

프로젝트 전체는 사용자가 지정한 로컬 `ChatGPT/리듬타이핑` 폴더에 있습니다. 공개 저장소에서는 원본 캐릭터 FBX·텍스처와 Mixamo 독립 애니메이션 파일을 제외합니다. 웹 빌드는 이 에셋을 게임에 통합한 형태입니다. 따라서 다른 컴퓨터에서 소스를 빌드하려면 사용 권한이 있는 승인 캐릭터 에셋을 먼저 가져와야 합니다.

```powershell
./Tools/Import-Scholar.ps1 -SourceDirectory "승인된 Scholar 에셋 폴더"
& "Unity.exe" -batchmode -nographics -projectPath "$PWD" -executeMethod BuildMoonlit.Prepare -quit -logFile prepare.log
& "Unity.exe" -batchmode -nographics -projectPath "$PWD" -buildTarget WebGL -executeMethod BuildMoonlit.BuildWeb -quit -logFile web-build.log
```

`SourceDirectory`에는 `Models/Scholar-Writing.fbx` 등 7개 FBX와 `Textures/BaseColor.png`, `Textures/Normal.png`가 있어야 합니다. 공개 저장소에서 재생 가능한 결과물은 `docs/`의 WebGL 빌드입니다. GitHub Pages는 `main` 브랜치 `/docs`를 사용합니다.

## 음악과 타격음

`Tools/compose_easy.py`는 새 120 BPM 곡 **달빛 잔치**, `Tools/compose.py`는 기존 144 BPM 곡을 합성합니다. 외부 음악 샘플을 쓰지 않으며 Python + NumPy가 필요합니다. 곡 정보는 `easy-music-report.json`과 `music-report.json`에 기록합니다. 게임 타격음은 `Audio/Impacts`에 있는 Kenney 녹음을 사용합니다.

## Cartoon FX 연결 상태

Cartoon FX Remaster Free 원본은 계정의 내 에셋에 추가되었으며 Unity 에디터의 다운로드·임포트가 남아 있습니다. 현재 웹 빌드에는 Kenney 이펙트가 적용되어 있고 Cartoon FX 원본은 포함되지 않았습니다. 원본을 Unity에서 임포트한 뒤 `Moonlit > Prepare imported Cartoon FX`를 실행하면 실제 Hit/Impact 프리팹을 `Resources/MoonlitFX`에 연결합니다. 연결 과정에서 에셋의 카메라 흔들림·광원 제어 스크립트를 제거하고 전용 UI 합성 카메라로 표시합니다. 원본 임포트 후에는 셰이더와 WebGL 렌더링 검증이 추가로 필요합니다.

## 검증

`BuildMoonlit.Validate`는 한글 분해(겹받침·합성 모음·Shift 포함), 판정 경계, 채보 시간 순서, 곡 안에 채보가 들어가는지, 판매량 계산의 단조성, Humanoid Avatar를 검사합니다. 결과는 `validation.txt`에 기록됩니다. 브라우저 실제 플레이와 시각 확인 결과는 `QA.md`를 참고하세요.

## 출처

- 캐릭터: 사용자가 승인·제공한 Meshy 모델. 원본 에셋 권리는 해당 제공 조건을 따릅니다.
- 모션: Adobe Mixamo, 승인 모델로 내려받고 A/T pose 차이를 수정한 7개 모션.
- 배경·책 UI: 이 프로젝트를 위해 이미지 생성으로 제작. 확정 모델을 다시 생성하지 않았습니다.
- 글꼴: [Google Fonts 나눔고딕](https://github.com/google/fonts/tree/main/ofl/nanumgothic), [나눔명조](https://github.com/google/fonts/tree/main/ofl/nanummyeongjo), SIL OFL. 라이선스 파일은 `Assets/Resources/Fonts`에 포함합니다.
- 음악: 본 프로젝트의 자체 합성 오리지널 2곡.
- 먹물 튐: [Kenney Splat Pack](https://kenney.nl/assets/splat-pack), CC0.
- 입력·착지 타격음: [Kenney Impact Sounds](https://kenney.nl/assets/impact-sounds), CC0. 라이선스는 `Assets/ThirdParty/Kenney`에 포함합니다.

고정 배경 그림과 실제 3D 캐릭터·책상을 결합한 2.5D 무대입니다. 콘셉트 이미지를 그대로 게임 실행 화면으로 표시하는 방식은 아닙니다.
