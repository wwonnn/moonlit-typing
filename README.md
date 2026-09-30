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

- 별주부전을 재구성한 16개 문구, 오리지널 144 BPM 곡 **달빛 인쇄소**.
- 실제 승인 Meshy 학자 메시와 텍스처, 수정된 Mixamo 애니메이션.
- 저채도 달밤 배경, 따뜻한 등불과 푸른 달빛, 카툰 명암 셰이더.
- 자모 키별 타이밍 판정, 연타, 입력음, 글자 비행, 종이 착지음과 입자 효과.
- 브라우저 물리 키 이벤트 시각 보정, 반복 키다운 무시, 포커스 손실 시 자동 일시정지.
- 점수에 따른 판매 결과, 최고 기록 저장, 자동 연주 모드.

현재는 한 곡을 끝까지 플레이할 수 있는 첫 구현입니다. 다수의 책, 상점 경제, 장기 성장 시스템, 얼굴 표정 블렌드셰이프는 포함하지 않습니다.

## Unity

Unity **6000.0.74f1**, Built-in Render Pipeline, uGUI. `Assets/Scenes/MoonlitStudy.unity`를 열고 Play합니다. 실제 3D 무대와 UI는 런타임에 생성됩니다.

프로젝트 전체는 사용자가 지정한 로컬 `ChatGPT/리듬타이핑` 폴더에 있습니다. 공개 저장소에서는 원본 캐릭터 FBX·텍스처와 Mixamo 독립 애니메이션 파일을 제외합니다. 웹 빌드는 이 에셋을 게임에 통합한 형태입니다. 따라서 다른 컴퓨터에서 소스를 빌드하려면 사용 권한이 있는 승인 캐릭터 에셋을 먼저 가져와야 합니다.

```powershell
./Tools/Import-Scholar.ps1 -SourceDirectory "승인된 Scholar 에셋 폴더"
& "Unity.exe" -batchmode -nographics -projectPath "$PWD" -executeMethod BuildMoonlit.Prepare -quit -logFile prepare.log
& "Unity.exe" -batchmode -nographics -projectPath "$PWD" -buildTarget WebGL -executeMethod BuildMoonlit.BuildWeb -quit -logFile web-build.log
```

`SourceDirectory`에는 `Models/Scholar-Writing.fbx` 등 7개 FBX와 `Textures/BaseColor.png`, `Textures/Normal.png`가 있어야 합니다. 공개 저장소에서 재생 가능한 결과물은 `docs/`의 WebGL 빌드입니다. GitHub Pages는 `main` 브랜치 `/docs`를 사용합니다.

## 직접 만든 사운드

`Tools/compose.py`는 외부 음원 샘플 없이 신스·타악기·발현음 합성으로 원곡과 짧은 게임 효과음을 만듭니다. Python + NumPy가 필요합니다. `music-report.json`에 BPM, 길이, 샘플레이트, 피크와 RMS를 기록합니다.

## 검증

`BuildMoonlit.Validate`는 한글 분해(겹받침·합성 모음·Shift 포함), 판정 경계, 채보 시간 순서, 곡 안에 채보가 들어가는지, 판매량 계산의 단조성, Humanoid Avatar를 검사합니다. 결과는 `validation.txt`에 기록됩니다. 브라우저 실제 플레이와 시각 확인 결과는 `QA.md`를 참고하세요.

## 출처

- 캐릭터: 사용자가 승인·제공한 Meshy 모델. 원본 에셋 권리는 해당 제공 조건을 따릅니다.
- 모션: Adobe Mixamo, 승인 모델로 내려받고 A/T pose 차이를 수정한 7개 모션.
- 배경·책 UI: 이 프로젝트를 위해 이미지 생성으로 제작. 확정 모델을 다시 생성하지 않았습니다.
- 글꼴: [Google Fonts 나눔고딕](https://github.com/google/fonts/tree/main/ofl/nanumgothic), [나눔명조](https://github.com/google/fonts/tree/main/ofl/nanummyeongjo), SIL OFL. 라이선스 파일은 `Assets/Resources/Fonts`에 포함합니다.
- 음악·효과음: 본 프로젝트의 자체 합성 오리지널.

고정 배경 그림과 실제 3D 캐릭터·책상을 결합한 2.5D 무대입니다. 콘셉트 이미지를 그대로 게임 실행 화면으로 표시하는 방식은 아닙니다.
