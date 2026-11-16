# T03 — 카메라 기준으로 일정한 속도로 걷기

> 문서: 있음 · 문서 검토 완료 · 점검일 2026-11-06
>
> Unity 6000.3.22f1 / URP 17.3.0 / Input System 1.20.0 · 실제 컴파일·플레이·빌드: 이번 문서 작업에서 미검증

카메라가 아래를 보더라도 W를 누르면 바닥을 따라 앞으로 가야 한다. 입력 장치와 이동 계산을 분리하고, 대각선 속도를 일정하게 만든다.

선행: [T02](02-player-wiring.md) · 다음: [T04](04-jump.md)

## 먼저 볼 자료와 이번 선택

| 구분 | 자료와 읽을 부분 | 이 단계에서 참고할 것 |
|---|---|---|
| 직접 근거 | [CharacterController.Move](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/CharacterController.Move.html) — motion·CollisionFlags | 속도와 변위 구분 |
| 직접 근거 | [데이터와 동작 분리](https://unity.com/how-to/separate-game-data-logic-scriptable-objects) — Data containers | 재사용 설정과 개체별 결과 분리 |
| BLACKOUT용 설계 판단 | [상태 소유권](../BLACKOUT_TechnicalDesign.md#ownership) | 위치는 Motor 하나만 변경; CharacterController는 첫 기준안 |

Rigidbody·Cinemachine이 열등해서 제외한 것은 아니다. 이 단계는 직접 만든 이동 경로를 이해하고 이후 같은 조건으로 대안을 비교하기 위한 기준이다.

## 개념과 책임

벡터는 방향과 크기를 함께 나타낸다. 카메라 앞 방향의 높이 성분을 제거하면 바닥 방향을 얻는다. 이동량은 속도에 프레임 시간을 곱해 계산한다.

`PlayerMotor`만 플레이어 위치를 바꾼다. `PlayerMovementConfig`는 공통 설정 에셋이고 현재 속도는 개별 Motor의 값이다. SO는 ScriptableObject, 즉 프로젝트에 저장하는 설정 객체다.

## 구현 계약

| 파일 | 작성할 멤버 | 역할·실패 |
|---|---|---|
| `Gameplay/Player/PlayerMovementConfig.cs` | `[SerializeField, Min(0.01f)] private float _speed = 4f`; `public float Speed => _speed` | 속도 설정; 4는 출발 실험값 |
| 같은 Config | `[CreateAssetMenu(menuName = "BLACKOUT/Player Movement")]` | Inspector에서 에셋 생성 |
| `Gameplay/Player/PlayerMotor.cs` | `[SerializeField] private CharacterController _controller`; `[SerializeField] private PlayerMovementConfig _config` | Unity 이동기·설정 참조 |
| Motor | `public Vector3 PlanarVelocity { get; private set; }` | 실제 이전/현재 위치 차이의 수평 속도 |
| Motor | `private void Awake()` | 참조·양의 유한 속도 검사; 잘못됐으면 로그·비활성화 |
| Motor | `public void Tick(Vector2 move, Vector3 forward, Vector3 right, bool jump, float dt)` | 수평 변위 한 번 적용; 이 단계 jump 미사용; dt<=0이면 속도 0 |
| Controller 변경 | `[SerializeField] private PlayerMotor _motor`; `[SerializeField] private Transform _movementBasis` | T02 Awake에서 추가 참조 검사 |
| Controller.Update 변경 | 기존 LastInput 결정 뒤 `_motor.Tick(...)` | basis 앞·오른쪽과 Time.deltaTime 전달 |

## 생각할 질문·의사코드

입력 `(1,1)`의 길이는 1인가? 카메라 앞이 수직이면 수평 투영 길이는 어떻게 되는가?

```text
dt 검사 → 입력 크기 최대 1 → 앞·오른쪽 수평화·정규화
방향 합성·크기 제한 → speed × dt → CharacterController.Move
실제 위치 차이에서 수평 속도 기록
```

## TODO 실습

표의 선언은 직접 만든 뒤 메서드 내부를 채운다. 이 단계에서는 평지에서만 확인한다.

```csharp
public void Tick(Vector2 move, Vector3 forward, Vector3 right,
    bool jump, float dt)
{
    // TODO: 시간을 확인한다. 0일 때 나누지 않는다.
    // TODO: 높이 성분을 제거하고 입력 크기를 제한한다.
    // TODO: Move 전후 위치로 PlanarVelocity를 구한다.
}
```

힌트 순서: y 제거 → `Vector3.ProjectOnPlane` / `ClampMagnitude` → `CharacterController.Move`는 속도가 아닌 변위를 받음.

[수평 방향 계산 참고 구현](answers/01-input-movement.md#movement)

## 따라하기·Unity 연결

1. Config는 ScriptableObject, Motor는 MonoBehaviour로 각각 작성한다. 둘을 모두 new로 만들지 않는다.
2. Assets → Create → BLACKOUT → Player Movement로 `Data/Player/DefaultMovement.asset`을 만든다.
3. Root에 CharacterController를 붙이고 Height·Radius·Center를 캡슐 대역과 맞춘다. 시험값 Height 1.8, Radius 0.3, Center Y 0.9에서 실제 모델에 맞춰 조정한다.
4. 같은 Root에 Rigidbody가 위치를 동시에 제어하지 않게 한다. 지형 Collider와 Controller 겹침을 확인한다.
5. Motor에 Controller·Config를, PlayerController에 Motor·고정 방향 Transform을 연결한다. T05에서 방향 기준을 카메라로 바꾼다.
6. W 이동 3초와 W+D 이동 3초의 거리를 평지에서 비교한다. 30fps·60fps 조건에서도 같은 시간을 걸어 비교한다.

## 03-1. 설정과 현재값을 구분하기

Config의 Speed는 공통 기준이고 Motor의 PlanarVelocity는 충돌 후 실제 이동 결과다. Config 안에 현재 속도를 저장하면 같은 에셋을 사용하는 개체들이 상태를 공유할 수 있다.

1. `Assets/BLACKOUT/Scripts/Runtime/Gameplay/Player/`에 Config와 Motor를 만든다. 두 파일 모두 `Blackout.Gameplay.Player` namespace를 사용한다.
2. Config는 `ScriptableObject`, Motor는 `MonoBehaviour`를 상속한다. Speed는 외부에서 읽기만 가능하게 제공한다.
3. Awake에서 Controller·Config가 없거나 Speed가 양의 유한 값이 아니면 개체 문맥을 포함해 한 번 오류를 알리고 Motor를 비활성화한다. Inspector의 Min 표시는 런타임 검증의 대체가 아니다.
4. Controller의 Awake에도 Motor·movementBasis 참조와 Motor 활성 상태 검사를 추가한다. 잘못 연결한 구성으로 계속 Tick하지 않는다.

[설정 파일 전체와 이동 메서드 부분 교체 참고 구현](answers/01-input-movement.md#movement)

### 단계별 힌트

<details>
<summary>힌트 1 — W가 가리키는 방향</summary>

기준 Transform의 forward를 바닥에 투영한다. 카메라가 아래를 봐도 바닥을 향해 걷게 하지 않는다.

</details>

<details>
<summary>힌트 2 — 작은 입력 크기 보존</summary>

입력의 최대 길이만 1로 제한한다. 무조건 normalized로 바꾸면 작은 아날로그 입력도 최고 속도가 된다.

</details>

<details>
<summary>힌트 3 — 전달할 단위</summary>

`CharacterController.Move`는 이번 이동량을 받는다. 초당 속도에 dt를 한 번 곱한다. 실제 속도를 구할 때는 위치 차이를 dt로 나누되 0과 비유한 dt는 먼저 거절한다.

</details>

## 03-2. Controller에서 한 번 호출하기

T02 Controller에 표의 두 필드를 추가하고, Update에서 LastInput을 결정한 뒤 다음 **호출부만 추가**한다. 아래는 Controller 전체 파일이 아니다.

```csharp
_motor.Tick(LastInput.Move, _movementBasis.forward,
    _movementBasis.right, LastInput.JumpPressed, Time.deltaTime);
```

Motor에는 Update를 추가하지 않는다. Unity가 Controller.Update를 호출하고, Controller가 직접 Motor.Tick을 호출한다. `transform.position`은 상속받은 Unity 값이고 `PlanarVelocity`는 직접 만든 속성이다.

## 03-3. Inspector를 연결하고 거리를 비교하기

1. Root에 CharacterController·Motor·Controller를 각각 하나씩 둔다. 모델 캡슐의 추가 CapsuleCollider와 Rigidbody가 중복으로 위치를 제어하지 않게 확인한다.
2. Root의 원점을 발밑 기준으로 정했다면 CharacterController의 Height 1.8, Radius 0.3, Center Y 0.9를 출발값으로 사용한다. 실제 모델의 치수와 바닥 겹침을 보고 조정한다.
3. Create → BLACKOUT → Player Movement로 만든 에셋을 Motor의 Config에 넣는다. Config는 컴포넌트로 붙이지 않는다.
4. T05 전에는 Root와 별개인 고정 빈 오브젝트 `MovementBasis`를 두고 Controller에 연결한다. 이 오브젝트를 Y축으로 90도 돌려 W 이동 방향도 돌아가는지 본다.
5. 바닥에 1m 간격 기준을 놓고 직선·대각선 각각 3초 이동 거리를 기록한다. 벽을 향한 때의 PlanarVelocity는 목표 speed와 다를 수 있다.
6. 실제 프레임률을 확인하며 30/60fps에서 비교한다. VSync가 켜져 있으면 Application.targetFrameRate만 바꿔도 원하는 값이 적용된다고 가정하지 않는다. 바꾼 설정은 검증 후 복구한다.

## 실패 검사

| 상황 | 기대 결과 |
|---|---|
| 입력 없음 | 수평 이동 없음 |
| 대각선 | 직선보다 빨라지지 않음 |
| 벽으로 이동 | 벽을 통과하지 않고 실제 속도 감소 |
| 속도 0·음수·NaN | 설정 오류를 식별; NaN이 Transform으로 전파되지 않음 |
| dt 0 | 나누기 오류·이동 없음 |

## 완료 기준

- [ ] 방향 기준을 돌리면 이동 방향도 바뀐다.
- [ ] 대각선·프레임률에 따라 속도가 달라지지 않는다.
- [ ] 실제 이동 속도와 입력 의도 차이를 설명한다.
- [ ] Inspector 참조 누락을 즉시 찾는다.
- [ ] 거리 비교 증거를 남겼다.

## 선택 이유·다음 불편

설정과 현재 상태를 나누면 같은 설정의 두 플레이어가 속도 상태를 공유하지 않는다. 작은 시제품은 직렬화 float 하나로 시작해도 된다. 현재는 중력이 없어 계단 끝에서 떨어지지 않는다. [T04](04-jump.md)가 그 문제를 해결한다.

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
