# BLACKOUT 문서 안내

문서는 파일 형식이 아니라 **사용 목적**에 따라 나눈다. 새 문서를 추가할 때도 아래 분류를 먼저 선택하고, 관련 이미지나 검증 결과는 `images/` 아래의 같은 성격 폴더에 둔다.

## 문서 구조

구현 전에 보여줄 기술 역량과 설계 기준을 잡으려면 [채용 관점의 기술 설계·문제 해결 조사](production/BLACKOUT_PortfolioEngineeringResearch.md)를 먼저 읽는다. 게임 루프와 코드 설계를 구분하고, 설계 산출물·대표 사례·검증 증거를 연결한다.

현재 구현 전 구조 기준은 [BLACKOUT 기술 설계 v0.1](production/BLACKOUT_TechnicalDesign.md)이다. 전체 구조·시스템 책임·상태 소유권·의존 방향·초기화/취소/복원·선택 이유와 대안을 정리했다.

직접 구현을 시작하려면 [구현 태스크와 단계별 따라하기](production/implementation/README.md)를 연다. 핵심 작업 28개와 확장 실험 14개에 대표 MD가 하나씩 연결되어 있다. [문서 준비 현황](production/implementation/README.md#document-status)에서 파일 유무·내용 점검·구현 검증을 구분한다. T00~T08은 공식 근거·단계별 연결·문제 해결을 상세 보완한 범위다. 첫 실습은 [T00 검증 장면](production/implementation/00-baseline.md), 현재 입력 코드 검증은 [T01](production/implementation/01-input.md)이다.

| 폴더 | 성격 | 주요 문서 |
|---|---|---|
| `course/` | 수업 요구사항과 일정 | [강의계획서](course/강의계획서.md) |
| `planning/` | 게임 목표, 기능, 범위 | [게임 기획서](planning/BLACKOUT_PRD.md) |
| `planning/concepts/` | 현재 채택한 초기 콘셉트 | [OVERCLOCK 콘셉트](planning/concepts/concept-01-OVERCLOCK.md) |
| `art-direction/` | 시각 언어와 제작 기준 | [아트 바이블](art-direction/BLACKOUT_ArtBible.md), [폰트·타이포그래피 가이드](art-direction/BLACKOUT_Typography.md) |
| `assets/` | 외부 에셋 선정, 라이선스, 상태 | [에셋 가이드](assets/BLACKOUT_AssetGuide.md), [에셋 원장](assets/BLACKOUT_AssetManifest.json) |
| `production/` | Unity 제작 절차와 구현 기록 | [개발 역량 로드맵](production/blackout-개발-역량-실전-경험-로드맵.md), [TPS 이동·펄스 발사 구현](production/BLACKOUT_TPSPlayerMovementAndPulseFire.md), [프로젝트 구조](production/BLACKOUT_ProjectStructure.md), [3D 공간 제작](production/BLACKOUT_3DSpaceBuilding.md), [인트로 씬 워크플로](production/BLACKOUT_IntroScene_Workflow.md) |
| `images/` | 문서에서 사용하는 이미지와 기계 검증 결과 | `art-direction/`, `typography/`, `production/` |
| `archive/` | 현재 방향에서 제외된 과거 문서 | `archive/concepts/` |

## 관리 규칙

컨테이너 통로와 보안 게이트의 실제 제작 결과는 [공간 제작·검수 기록](production/BLACKOUT_CargoStation_Workflow.md)과 [구역 에셋 원장](assets/BLACKOUT_CargoStation_Manifest.json)에서 확인한다.

공간에 적용한 재질의 위치·최신 화면·검증 결과는 [화물역 재질 제작·적용 기록](production/BLACKOUT_CargoMaterials_Workflow.md)과 [재질 원장](assets/BLACKOUT_CargoMaterials_Manifest.json)에서 확인한다.

관제탑 보스전 공간의 위치·장치 미리보기·검수 결과는 [관제탑 제작 기록](production/BLACKOUT_ControlTower_Workflow.md)과 [관제탑 에셋 원장](assets/BLACKOUT_ControlTower_Manifest.json)에서 확인한다.

학습·개발을 시작할 때는 [개발 역량·실전 경험 로드맵](production/blackout-개발-역량-실전-경험-로드맵.md)에서 핵심 적용 항목, 심화 실험과 완료 증거를 확인한다.

- 기획이 바뀌면 먼저 `planning/`을 고치고, 영향을 받는 아트·에셋·제작 문서를 함께 확인한다.
- 색, 재질, 조명, UI 모양은 `art-direction/`을 기준으로 판단한다.
- 외부 파일을 채택하거나 제외하면 `assets/BLACKOUT_AssetManifest.json`에 출처와 이유를 기록한다.
- 실제 Unity 작업 과정과 검증 결과는 `production/`에 기록한다.
- 이미지 파일은 문서 옆에 섞지 않고 `images/` 아래에 둔다.
- 더 이상 현재 기준이 아닌 문서는 삭제하지 않고 `archive/`로 이동한다.
