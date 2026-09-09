# BLACKOUT 문서 안내

문서는 파일 형식이 아니라 **사용 목적**에 따라 나눈다. 새 문서를 추가할 때도 아래 분류를 먼저 선택하고, 관련 이미지나 검증 결과는 `images/` 아래의 같은 성격 폴더에 둔다.

## 문서 구조

| 폴더 | 성격 | 주요 문서 |
|---|---|---|
| `course/` | 수업 요구사항과 일정 | [강의계획서](course/강의계획서.md) |
| `planning/` | 게임 목표, 기능, 범위 | [게임 기획서](planning/BLACKOUT_PRD.md) |
| `planning/concepts/` | 현재 채택한 초기 콘셉트 | [OVERCLOCK 콘셉트](planning/concepts/concept-01-OVERCLOCK.md) |
| `art-direction/` | 시각 언어와 제작 기준 | [아트 바이블](art-direction/BLACKOUT_ArtBible.md), [폰트·타이포그래피 가이드](art-direction/BLACKOUT_Typography.md) |
| `assets/` | 외부 에셋 선정, 라이선스, 상태 | [에셋 가이드](assets/BLACKOUT_AssetGuide.md), [에셋 원장](assets/BLACKOUT_AssetManifest.json) |
| `production/` | Unity 제작 절차와 구현 기록 | [개발 역량 로드맵](production/BLACKOUT_Engineering_Learning_Roadmap.md), [TPS 이동·펄스 발사 구현](production/BLACKOUT_TPSPlayerMovementAndPulseFire.md), [프로젝트 구조](production/BLACKOUT_ProjectStructure.md), [3D 공간 제작](production/BLACKOUT_3DSpaceBuilding.md), [인트로 씬 워크플로](production/BLACKOUT_IntroScene_Workflow.md) |
| `images/` | 문서에서 사용하는 이미지와 기계 검증 결과 | `art-direction/`, `typography/`, `production/` |
| `archive/` | 현재 방향에서 제외된 과거 문서 | `archive/concepts/` |

## 관리 규칙

학습·개발을 시작할 때는 [개발 역량·실전 경험 로드맵](production/BLACKOUT_Engineering_Learning_Roadmap.md)에서 핵심 적용 항목, 심화 실험과 완료 증거를 확인한다.

- 기획이 바뀌면 먼저 `planning/`을 고치고, 영향을 받는 아트·에셋·제작 문서를 함께 확인한다.
- 색, 재질, 조명, UI 모양은 `art-direction/`을 기준으로 판단한다.
- 외부 파일을 채택하거나 제외하면 `assets/BLACKOUT_AssetManifest.json`에 출처와 이유를 기록한다.
- 실제 Unity 작업 과정과 검증 결과는 `production/`에 기록한다.
- 이미지 파일은 문서 옆에 섞지 않고 `images/` 아래에 둔다.
- 더 이상 현재 기준이 아닌 문서는 삭제하지 않고 `archive/`로 이동한다.
