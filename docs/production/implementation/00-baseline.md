# T00 — 오류를 재현할 장면과 첫 실행 파일 만들기

> 문서: 있음 · 문서 검토 완료 · 점검일 2026-11-06
>
> Unity 6000.3.22f1 / URP 17.3.0 / Input System 1.20.0 · 실제 컴파일·플레이·빌드: 이번 문서 작업에서 미검증

같은 버그를 다시 만들 수 있어야 원인을 비교할 수 있다. 기존 환경을 보존하며 작은 검증 장소와 첫 Windows 실행 파일을 준비한다.

선행: 없음 · 다음: [T01 입력](01-input.md) · [전체 목록](README.md)

## 먼저 볼 자료와 이번 선택

| 구분 | 자료와 읽을 부분 | 이 단계에서 참고할 것 |
|---|---|---|
| 직접 근거 | [Unity Build Profiles](https://docs.unity3d.com/6000.3/Documentation/Manual/BuildSettings.html) — 프로필·장면 목록 | 실행할 장면과 플랫폼 설정 |
| 직접 근거 | [Unity Profiler](https://docs.unity3d.com/6000.3/Documentation/Manual/Profiler.html) — 연결·기록 | 기준 측정 준비 |
| BLACKOUT용 설계 판단 | [기술 설계의 검증 계획](../BLACKOUT_TechnicalDesign.md#verification) | PlayerLab·고정 경로·첫 빌드의 시점은 이 프로젝트의 선택 |

공식 설계 샘플의 SceneBootstrapper는 시작 장면을 보장하는 도구다. 이번에는 여러 장면 초기화가 필요하지 않으므로 그 도구를 가져오지 않는다.

## 개념과 현재 상태

Sandbox의 `testScene`에 플레이어용 이름들이 있다. 이름만으로 이동·카메라가 연결됐다고 판단하지 않는다. Console은 Unity가 알려주는 오류 창이고, Development Build는 문제를 추적할 정보를 포함한 실행 파일이다.

## 파일 책임과 작업 계약

| 대상 | 이번 작업 | 소유·수명 |
|---|---|---|
| `Scenes/Sandbox/testScene.unity` | 기존 배치 확인 | 사용자 원본 장면 |
| `Scenes/Sandbox/PlayerLab.unity` | Save As로 검증 복사 | 실습 장면, 원본 덮어쓰기 금지 |
| `ProjectSettings` / Build Profiles | Windows·장면 목록 확인 | 프로젝트 설정, 변경값 기록 |

이번 단계는 새 C# 파일을 요구하지 않는다. 다른 프로젝트나 에셋을 일괄 이동하지 않는다.

## 먼저 생각할 힌트

- 재현할 때 매번 플레이어 위치와 카메라 방향이 다르면 비교가 쉬운가?
- 붉은 오류가 있는 상태에서 새 기능까지 넣으면 원인 범위가 어떻게 되는가?

```text
현재 변경 기록 → 장면 열기 → 첫 오류 확인 → 검증 장소 고정 → 실행 파일 생성 → 같은 장소 확인
```

## 따라하기

1. 터미널에서 프로젝트 경로와 `git status --short`를 확인한다. 변경 파일을 삭제하거나 초기화하지 않는다.
2. Unity Hub에서 `BlackOut`을 정확한 버전으로 연다. Window → General → Console에서 첫 붉은 오류의 전체 메시지·파일·줄을 기록한다. 연쇄 오류 수보다 첫 원인을 본다.
3. `testScene`을 열고 Hierarchy의 `Player_Root`, `MainCam`, `CameraTarget`을 찾는다. Inspector의 컴포넌트와 Missing Script를 확인한다.
4. File → Save As로 `PlayerLab`을 만들고 이번 T00~T08의 공통 검증 장면으로 사용한다. 평지·계단·경사·낮은 천장·벽 모서리·얇은 벽을 놓고 Collider를 확인한다. 모델 승인 전에는 캡슐을 대역으로 사용한다.
5. Game 화면을 1920×1080 기준으로 맞춘다. Layers에 World, Player, PlayerProjectile, Enemy, CameraObstacle의 필요를 적고 실제 검사 용도를 정한다. 지형은 필요에 따라 여러 검사 마스크에 포함한다.
6. File → Build Profiles에서 Windows 프로필과 현재 검증 장면을 설정한다. Development Build를 켜고 저장소의 생성 파일 관리 규칙에 맞는 `Builds/Windows/Dev`에 첫 빌드를 만든다. 아직 이동하지 않아도 장면이 열리면 초기 실행 검증 자료다.
7. 같은 컴퓨터에서 실행한다. 해상도·입력 포커스·분홍 재질·Console/Player 로그를 확인한다. 프로파일러 연결을 시험하고 아무 조작 없는 10초 기준 기록을 남긴다.

## 클릭 순서로 확인하기

### 00-1. 현재 상태를 먼저 적는다

1. 터미널의 위치가 저장소 루트인지 확인하고 Git 변경 목록을 기록한다. 이 목록은 되돌릴 대상 목록이 아니다.
2. Unity의 Project 창에서 `Assets/BLACKOUT/Scenes/Sandbox/testScene.unity`를 찾아 연다. 다른 작업의 Play나 미저장 장면이 열려 있으면 그 작업을 먼저 마친다.
3. Console의 Collapse를 잠시 끄고 첫 붉은 오류를 선택한다. 메시지·경로·줄 번호를 기록한다. 오류가 있으면 원인 해결 전 새 기능을 덧붙이지 않는다.
4. Hierarchy에서 Player_Root를 선택해 Transform, 붙은 스크립트, Missing Script 유무를 기록한다. 현재 이름만 확인했으며 올바른 연결까지 확인된 것은 아니다.

### 00-2. 같은 조건의 검증 장소를 만든다

1. File → Save As로 `Assets/BLACKOUT/Scenes/Sandbox/PlayerLab.unity`에 저장한다. 이후 실습은 이 복사본에서 진행한다.
2. 평지·정면 벽·모서리·얇은 벽·낮은 천장·계단·경사를 구분해 배치한다. 바닥과 벽의 Collider를 Inspector에서 확인한다.
3. 이름만 있는 Visual은 임시 캡슐로 표시해도 된다. 총은 큐브, 표적은 별도 캡슐로 검증하고 실제 에셋 채택 검수와 구분한다.
4. Layers에 필요한 이름이 이미 있는지 먼저 확인한다. `World`, `Player`, `PlayerProjectile`, `Enemy`를 사용하고, 카메라 마스크는 World를 포함하고 Player를 제외한다. 한 오브젝트의 레이어는 하나지만 여러 LayerMask에 포함될 수 있다.
5. T03에서 쓰는 Root의 위치·방향과 시작 지점을 기록한다. Root 스케일은 1을 기준으로 하고 모델 크기는 Visual에서 조정한다.

### 00-3. 첫 실행 파일을 확인한다

1. File → Build Profiles에서 Windows용 프로필을 연다. Windows 빌드 모듈이 없으면 Hub의 해당 Editor → Add modules에서 설치한다.
2. Scene List에 PlayerLab을 넣고 시작 장면을 확인한다. 다른 장면을 제거하기 전에 기존 프로필의 목록을 기록한다.
3. Development Build로 첫 실행 파일을 만든다. 출력 폴더를 `Builds/Windows/Dev`로 정하되 Git 제외 여부를 먼저 확인하고 생성 파일을 자동 커밋하지 않는다.
4. 실행 파일에서 같은 배치가 보이는지 확인한다. 아직 이동하지 않는 것은 T00의 예상 상태다.
5. Profiler를 연결해 CPU/GPU/GC 기록이 수집되는지 10초 확인한다. PC 사양·해상도·품질·VSync·빌드 종류를 적는다. 숫자가 낮다는 이유로 최종 성능을 합격 처리하지 않는다.

## 다음 단계로 넘길 것

이 단계의 결과는 PlayerLab 장면, 고정 검증 경로, 첫 오류 기록, 첫 실행 파일이다. 새 게임 관리자는 만들지 않는다. 다음 T01에서는 같은 장면에서 입력 요청을 추적한다.

## 실패를 확인하는 방법

| 증상 | 확인할 곳 |
|---|---|
| 빌드 버튼 비활성 | Unity Hub의 Windows 모듈과 현재 프로필 |
| Editor에서만 정상 | 장면 목록, Editor 전용 코드, Player 로그 |
| 분홍 재질 | 해당 재질의 URP Shader와 변환 결과 |
| 다른 장면 실행 | 프로필에 포함된 장면 순서 |

실행 파일 성능에는 하드웨어·해상도·동기화 설정이 영향을 준다. 이 단계는 60fps 달성 판정이 아니다.

## 완료 기준

- [ ] 첫 오류 유무와 현재 장면 연결 상태를 기록했다.
- [ ] 원본을 보존한 고정 검증 장소를 열 수 있다.
- [ ] Windows 실행 파일에서 같은 장소가 열린다.
- [ ] 화면·빌드·측정 환경 중 하나 이상을 증거로 남겼다.

## 선택 이유와 한계

재현 장면은 버그를 찾는 시간을 줄이지만 본편에서도 다시 확인해야 한다. 더 단순하게 기존 장면 한 구역만 써도 된다. 중요한 것은 같은 조건을 반복하는 것이다.

기록: 오류 / 변경한 설정 / 실제 실행 결과 / 증거 위치 / 남은 문제.

## 실습 기록 — 실제 확인 후 작성

아래는 빈 기록 양식이다. 문서 검토와 구현 결과를 구분하며 실행하지 않은 항목은 미확인으로 남긴다.

```text
태스크 / 확인 날짜:
직접 작성·변경한 파일 / Inspector 설정:
재현 순서:
예상 결과 / 실제 관찰:
정상·실패·취소·중복·재활성화 결과:
Console 첫 오류 또는 오류 없음 확인 조건:
영상·스크린샷·빌드 위치:
원인 근거 / 선택한 해결 / 추가 비용 / 남은 한계:
```
