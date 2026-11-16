# 공통 계약과 참고 자료

여기서 계약은 “서로 다른 파일이 어떤 이름의 함수로 무슨 값을 주고받는가”라는 약속이다. 구현 중 함수 이름이 달라져 문서 사이의 연결이 끊기는 일을 줄인다. 아래 타입은 기존 구현이라고 가정하지 않으며 해당 단계에서 만든다.

전체 구조의 통합 기준은 [기술 설계 v0.1](../BLACKOUT_TechnicalDesign.md)이다. 이 문서의 초기 단계 API는 학습 출발점이며, Driver·GameSession·복원 알림 경계는 [통합 변경 표](../BLACKOUT_TechnicalDesign.md#migration)를 따른다.

## 멤버 표를 읽는 법

표는 클래스에 직접 작성할 값과 함수의 약속이다. 한 행에 세미콜론으로 여러 선언이 있으면 각각 별개의 멤버를 작성한다. `OnEnable()/OnDisable()`도 두 메서드를 뜻한다. 표를 C# 파일에 그대로 붙이지 않고 선언 모양을 읽어 작성한다.

| 표기 | 쉬운 뜻 |
|---|---|
| `public` / `private` | 다른 코드에서 사용 가능 / 해당 클래스 내부에서 사용 |
| `[SerializeField] private ...` | 외부 코드에는 숨기되 Unity Inspector에서 연결·조정 |
| `_input` / `s_TickMarker` | 직접 정한 필드 이름; `_`는 개체별, `s_`는 정적 필드를 구분하는 이 문서의 관례 |
| `float dt` / `bool jump` | 경과 시간 숫자 / 점프 요청의 참·거짓 |
| `public float Current { get; private set; }` | 외부에서 읽을 수 있고 클래스 안에서만 바꾸는 속성 |
| `void` / `bool` 반환 | 결과값 없음 / 성공 여부를 true·false로 반환 |
| `out Vector3 direction` | 성공 여부와 함께 direction 값도 호출자에게 돌려줌 |
| `enum` | Playing·Paused처럼 정해진 상태 이름의 목록 |
| `interface` | 내부 구현을 몰라도 호출할 수 있는 최소 기능 약속 |
| `event Action Changed` | 상태 변경을 구독자에게 알리는 신호; `+=` 등록과 `-=` 해제를 짝지음 |
| `readonly struct` | 값으로 복사되고 생성 후 바꾸지 않는 작은 데이터; 내부 참조까지 자동 독립 복사되지는 않음 |
| `namespace` / `using` | 타입의 소속 이름 / 그 소속의 타입 이름을 짧게 쓰는 선언 |

Unity 타입은 `using UnityEngine;`, 입력 타입은 `using UnityEngine.InputSystem;`, Dictionary는 `using System.Collections.Generic;`가 필요하다. 다른 BLACKOUT 폴더의 타입도 해당 namespace를 using으로 연결한다. 모르는 타입에 붉은 줄이 보이면 먼저 첫 컴파일 오류와 선언 위치를 확인하고 같은 이름의 가짜 클래스를 추가하지 않는다.

## 경로·이름·생명주기

- 모든 코드 경로는 Unity 프로젝트 `BlackOut` 기준 `Assets/BLACKOUT/`에서 시작한다.
- 새 실행 코드는 `Scripts/Runtime/Gameplay/...`, 화면·카메라·소리는 `Scripts/Runtime/Presentation/...`, 검사 도구는 `Scripts/Editor/`에 둔다.
- 새 코드의 namespace는 `Blackout.Gameplay.Player`, `Blackout.Gameplay.Combat`처럼 폴더 책임과 맞춘다. 기존 전역 이름의 Reader·Snapshot은 일괄 이동하지 않는다.
- `new`는 입력 래퍼와 순수 C# 데이터에 사용한다. `MonoBehaviour`는 Unity의 Add Component, 프리팹은 `Instantiate`로 만든다.
- `transform`, `gameObject`, `enabled`는 Unity에서 상속한 멤버다. `_motor` 같은 참조는 직접 선언하고 Inspector에서 연결한다.
- `Blackout.Presentation.Camera` namespace 안에서 실제 카메라 타입은 `UnityEngine.Camera`로 명시해 이름 충돌을 피한다.
- `Awake`는 자기 준비, `OnEnable`/`OnDisable`은 구독·해제, `Update`는 입력·행동, `LateUpdate`는 카메라 배치에 사용한다. 다른 객체의 `Awake` 실행 순서에 기대지 않는다.

## 단계별 공개 API 지도

| 도입 | 타입 / 주요 공개 계약 | 소유하는 상태 |
|---|---|---|
| T01 수정 | `OnAim`은 `context.ReadValueAsButton()`, 일회 콜백은 `context.performed` | 유지 상태와 다음 소비까지 보관할 요청 구분 |
| 기존 / T01 | `PlayerInputReader.CaptureSnapshot(): PlayerInputSnapshot` | 장치 입력 캐시, 한 번 요청 |
| T02 → 통합 | `PlayerController`, `PlayerControlGate.SetAllowed(bool allowed)` | 호출 순서 / GameFlow에서 파생한 조작 허용; 통합 시 GameSession만 동기화 |
| T03~04 | `PlayerMotor.Tick(Vector2 move, Vector3 forward, Vector3 right, bool jump, float dt)` | 현재 이동·수직 속도 |
| T05 | `ThirdPersonCameraRig.ApplyLook(Vector2 delta)`, `LateTick(float dt)`, `PlanarForward`, `PlanarRight`, `AimRay`, `HasValidAim` | 각도·거리·배치 유효성; 독립 LateUpdate 없음 |
| T06 | `PlayerFacing.Tick(Vector3 moveDirection, Vector3 aimForward, bool aiming, float dt)` | 모델의 회전 |
| T07 | `PulseGun.TryFire(Ray aimRay, bool charged): bool` | 다음 발사 가능 시간 |
| T07~11 | `PulseProjectile.Launch(Vector3 direction, float damage, GameObject owner)`, `Finish()` | 비행·명중·반환 상태 |
| T08 | `IDamageable.TryApplyDamage(float amount): bool`, `Health` | 현재 체력·사망 알림 |
| T10 | `PulseGun.TickCharge(bool pressed, bool held, bool released, Ray aimRay, float dt)`, `CancelCharge()` | 충전 상태·시간 |
| T12 | `PowerStore`, `PowerTransfer.TryTransfer(PowerStore source, PowerStore target, int amount): bool` | 개체별 보유량 / 이전 규칙 |
| T13 | `IInteractable.CanInteract(): bool`, `TryInteract(): bool`, `CancelInteraction(): void` | 각 대상의 행동 진행 |
| T15 | `FacilityPower.IsPowered`, `Changed` | 시설 소켓의 저장량·통전 여부 |
| T20 | `GameFlow.SetState(GameState next)`, `GameState` | 전체 행동 허용 상태 |
| T20~T22 | `GameSession`, `GameplayTickDriver` | 세션 전환 요청·재시도 조정 / 순서가 필요한 Update·LateUpdate 호출 |
| T21~22 | `CheckpointSnapshot`, `CheckpointCoordinator.Capture()`, `TryRestore(): bool` | 독립 기록·복원 진행 |

`bool` 반환은 예상 가능한 거절이다. 용량 부족·재사용 대기 등은 `false`, 잘못된 제작 설정은 한 번의 구체적인 오류 로그나 프로그래머 예외로 드러낸다. 전력 이전의 실패 이유 문자열은 규칙층에서 만들지 않고 필요해지면 결과 enum을 도입한다.

## 한 프레임의 약속

T00~T08도 최종 카메라 뒤 발사를 지킨다. 아직 Driver·GameSession은 만들지 않는다.

```text
PlayerController.Update
  Snapshot 한 번 → Gate → ApplyLook → Motor.Tick → Facing.Tick
  T07부터 _pendingFire에 의도 보관
PlayerController.LateUpdate
  CameraRig.LateTick(dt) → 요청 비우기 → 허용·HasValidAim 검사 → TryFire
```

T02~T07 Gate 시작값 true는 실습 임시 정책이다. T08 Controller는 Died 구독으로 Gate 차단·pending 삭제·StopMotion을 수행한다. OnEnable은 구독만 하며 자동 허용하지 않는다. Update/LateUpdate에서 사망도 조회해 비활성 중 놓친 알림을 보완한다. T20에 이 조정을 GameSession으로 옮긴다.

T16 이후 통합의 목표 순서는 다음과 같다.

```text
Input System이 콜백 전달
GameplayTickDriver.Update (통합 단계)
  CargoLift.Tick → 탑승 변위 전달 → PlayerController.Tick
  Snapshot 한 번 소비 → 조작 허용 검사
  Camera.ApplyLook → Motor.Tick → Facing.Tick → Gun/Interaction 의도 보관
GameplayTickDriver.LateUpdate
  Camera 최종 배치·벽 보정 → 최종 Ray·대상 검사
  보관한 발사/상호작용 요청 한 번 확정 → 흡수 완료 재검증
  예약된 Capture/Restore 등 세션 요청 처리
```

T02는 아직 카메라·무기가 없으므로 입력 소비와 허용 검사만 연결한다. 처음에는 PlayerController.Update에서 직접 호출해도 된다. T03은 고정 기준 Transform으로 이동하고 T05에서 카메라 기준으로 교체한다. T16 통합 때 Driver로 순서를 옮기면 원래 자동 Update/LateUpdate를 함께 제거해 중복 호출을 막는다. 아직 없는 타입을 참조해 컴파일을 깨지 않는다.

T06~07의 사격 기준은 **최종 카메라 위치에서 만든 중심 Ray**다. T07부터 Update에 발사 의도를 저장하고 최종 카메라 배치 뒤 한 번 소비한다. 그 사이 사망·정지하면 요청을 버린다. Snapshot을 다시 읽지 않는다. AI Update·탄환 FixedUpdate는 각 시간 기준과 허용 정책을 따르며, 같은 렌더 프레임에 물리 스텝이 0회/여러 번일 수 있다.

Playing이 아니면 Driver의 세계 진행·행동 확정만 건너뛴다. 세션 요청은 계속 처리해 Dead 재시도와 Paused 메뉴가 동작하게 한다. 장면 시작에는 안전한 시작 Anchor에서 최초 Snapshot을 만들고, 첫 체크포인트 이전 사망도 그 기록으로 복원한다.

## 문서 간 규칙 차이

| 차이 | 이번 실습 기준 |
|---|---|
| 기존 TPS 문서의 방향키·게임패드·UI Map | 제안이다. 현재 에셋의 실제 키보드·마우스만 먼저 검증하고 T25에서 확장한다 |
| 기존 `FirePressed`는 누르는 즉시 발사 | T07의 학습 단계. T10에서 짧게 누르고 떼면 기본, 길게 눌렀다 떼면 충전으로 명시적으로 교체한다 |
| 환경의 보안 문 스크립트 | `BlackoutSecurityGatePreview`는 시각 미리보기다. 자원 이전과 전투가 구현된 증거가 아니다 |
| 기존 README의 설치 전 표기와 제작 기록 | T00에서 실제 로컬 에셋·리그·재질을 확인한다. 문서만으로 승인하지 않는다 |
| 보스 기본 사격 허용과 전력 필수 관계 | PRD 기준으로 2단계 약점은 전력으로 연다. 기본 사격도 열린 약점에는 피해를 줄 수 있고 충전은 유리하다. 2회 순환 처치 가능 여부는 T23에서 수치 검증한다 |
| 베스티지아 가이드의 빈 Reader | 2026-10-10 고정 커밋 기준. 현재 직접 작성된 Reader를 출발점으로 한다 |
| 초기 실습의 단일 GameFlow·즉시 Restore 알림 | 통합은 GameSession의 세션 조정, GameFlow의 상태 소유, Coordinator의 적용으로 분리하고 복원 완료 후 표시 갱신 |

## 베스티지아 원본을 읽는 방법

베스티지아는 설계·문법을 읽을 교재다. 현재 BLACKOUT의 동작 검증 자료와 구분한다. 아래 링크는 가이드가 지정한 `b90f9aaf...` 시점이며 최신 develop 상태라고 주장하지 않는다. 붕어빵 코드는 이전 경험 참고용이다.

| 읽기 | 읽을 질문 | 연결 작업 |
|---|---|---|
| [CharacterRuntimeState](https://github.com/hoegidong-makgulli/VESTIGIA/blob/b90f9aaf4c1a7fba8c075b3339f262657731b202/waredo/Assets/03.%20InGame/Scripts/Combat/Core/CharacterRuntimeState.cs#L7) | 현재 값은 누가 소유하고 복사하는가 | T08, T12, T21 |
| [ICharacterActions](https://github.com/hoegidong-makgulli/VESTIGIA/blob/b90f9aaf4c1a7fba8c075b3339f262657731b202/waredo/Assets/03.%20InGame/Scripts/Combat/Core/ICharacterActions.cs#L4) | 호출자가 정말 알아야 하는 기능은 무엇인가 | T02, T13 |
| [CharacterAgent](https://github.com/hoegidong-makgulli/VESTIGIA/blob/b90f9aaf4c1a7fba8c075b3339f262657731b202/waredo/Assets/03.%20InGame/Scripts/Combat/Core/CharacterAgent.cs#L49) | 검증과 변경은 어떤 순서인가 | T08, T12 |
| [BattleSimulation](https://github.com/hoegidong-makgulli/VESTIGIA/blob/b90f9aaf4c1a7fba8c075b3339f262657731b202/waredo/Assets/03.%20InGame/Scripts/Combat/Core/BattleSimulation.cs#L20) | 기록과 현재 상태는 독립적인가 | T21~22 |

로컬 교재는 `C:\DevHub\02_GameDev\Vestigia\learning\Vestigia_코드로_배우는_BLACKOUT_설계_학습가이드.md`다. 다른 컴퓨터에서는 원본 링크와 위 읽기 질문으로 진행할 수 있다. 이번 문서는 해당 저장소를 수정하지 않는다.

## 공식 자료와 버전

2026-10-15에 프로젝트 manifest·로컬 패키지 문서와 아래 공식 자료를 대조했다. 웹의 `@1.20` 문서는 1.20.1을 표시할 수 있으며 프로젝트 설치 선언은 1.20.0이다. 패키지 문서의 작은 버전 표기를 설치 완료로 해석하지 않는다.

- [Input Actions](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/manual/Actions.html): 입력 의미와 장치 연결. T01·T25.
- [CharacterController.Move](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/CharacterController.Move.html): 변위 전달과 충돌. T03~04.
- [SphereCast](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.SphereCast.html): 카메라·탄환 부피 검사. T05·T07.
- [ObjectPool](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Pool.ObjectPool_1.html): 재사용 콜백. T11.
- [Unity Profiler](https://docs.unity3d.com/6000.3/Documentation/Manual/Profiler.html): 실행 비용 확인. T00·T26.
- [Build Profiles](https://docs.unity3d.com/6000.3/Documentation/Manual/BuildSettings.html): 빌드 설정. T00·T27.
- [Cinemachine Third Person Follow](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineThirdPersonFollow.html), [Two Bone IK](https://docs.unity3d.com/Packages/com.unity.animation.rigging@1.3/manual/constraints/TwoBoneIKConstraint.html): E01·E02의 개념 참고. 설치 버전은 도입 때 결정한다.

[구현 작업 목록으로](README.md)
