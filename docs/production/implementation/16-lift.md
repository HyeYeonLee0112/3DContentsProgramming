# T16 — 운행 중 회수할 수 없는 승강기와 탑승 이동

CharacterController는 움직이는 바닥을 자동으로 정확히 따라준다고 가정할 수 없다. 승강기의 상태와 이번 프레임 이동량을 명시해 탑승자를 함께 움직인다.

선행: [T15](15-facilities.md), [T04](04-jump.md) · 다음: [T21](21-checkpoint-data.md)

## 책임·계약

| 파일 | 필요한 멤버 | 동작·소유 |
|---|---|---|
| `Gameplay/Facilities/LiftState.cs` | `enum LiftState { StoppedBottom, MovingUp, StoppedTop, MovingDown }` | 운행 상태 |
| `Gameplay/Facilities/CargoLift.cs` | `[SerializeField] private Transform _platform, _bottom, _top`; `[SerializeField] private FacilityPower _power`; `[SerializeField] private float _speed = 1.5f`; `public LiftState State { get; private set; }`; `public Vector3 FrameDelta { get; private set; }` | 플랫폼 위치·이번 변위 |
| Lift | `public bool IsMoving { get; }`; `public bool TryTravel()`; `public bool CanWithdraw()`; `public void Tick(float dt)`; `public void RestoreAtBottom()` | 통전 중 출발, 운행 중 회수 거절, 복원은 아래 정차 |
| Motor 추가 | `private Vector3 _externalDisplacement`; `public void AddPlatformDisplacement(Vector3 delta)` | 다음 Tick에 한 번 소비할 외부 변위 |
| Motor.Tick 변경 | 수평·수직·발판 변위를 합쳐 Move | 소비 후 외부 변위 삭제; StopMotion도 삭제 |
| FacilitySocket 회수 변경 | `[SerializeField] private CargoLift _lift` 선택 참조 | 승강기 소켓만 CanWithdraw 확인 후 기존 회수 |

## 정책을 먼저 적기

PRD는 운행 중 회수 금지와, 정차 후 회수하면 아래에 고정되는 결과를 요구한다. 위층에서 회수하면 즉시 순간이동시키지 않는다. 이 실습은 **위층 정차 상태의 회수 요청도 귀환이 필요한 상태로 거절하고, 먼저 아래 정차 후 회수**하게 안내한다. 이 조작 정책은 기획 보완 제안이므로 본편 적용 전 PRD와 함께 확정한다. 자동 귀환을 택한다면 하강 완료 전 전력 반환을 금지한다.

## 생각할 질문·의사코드

승강기를 Update에서 먼저 옮기고 플레이어가 그 변위를 나중에 소비해야 하는 이유는 무엇인가?

```text
출발: 통전·정차 확인 → Moving
프레임: 이전 위치 보관 → MoveTowards → FrameDelta 계산
탑승자: 접지한 플랫폼이 이 승강기인지 확인 → 변위 한 번 추가
Motor.Tick → 탑승 변위+자기 이동 → 다음 프레임 외부 변위 초기화
```

## TODO 실습

```csharp
public bool CanWithdraw()
{
    // TODO: 확정한 회수 정책의 정차 상태인지 검사한다.
    return false;
}
```

힌트: Transform을 부모로만 묶으면 회전·스케일·점프 분리 문제가 숨음 → 접지 플랫폼 식별 → 운행 Tick의 명시적 호출 순서.

## 따라하기·Inspector

1. 실제 승강기 실습은 수직 직선 이동부터 만든다. 플랫폼 Collider와 상·하 정차점을 별도 Transform으로 둔다. 무한 회전·물리 밀기까지 함께 넣지 않는다.
2. CargoLift에 FacilityPower(2칸)와 정차점을 연결한다. 먼저 플레이어 없이 왕복·정지를 확인한다.
3. 현재 바닥 플랫폼을 하향 검사로 판정하는 탑승 연결을 추가한다. 공중·다른 바닥에서는 FrameDelta를 받지 않는다.
4. 장면 호출 담당이 Lift.Tick → 탑승 변위 전달 → PlayerMotor.Tick 순서를 보장하게 한다. 승강기와 플레이어가 각각 순서를 추측하는 독립 Update를 쓰지 않는다.
5. 점프·하차·운행 중 피격·회수 연타·정전 요청을 확인한다. 캐릭터를 끼우는 천장과 문은 탑승 시험에 포함한다. 애니메이션용 수평 속도에서는 발판 자체 변위를 빼 상대 이동을 사용해 가만히 탄 캐릭터가 걷지 않게 한다.
6. 체크포인트는 승강기 운행 중 저장하지 않고 정차 지점에서만 저장한다. T21에 이 제한을 명시한다.

## 완료 기준

- [ ] 2칸 통전 때만 출발하고 정차점에 정확히 멈춘다.
- [ ] 탑승·점프·하차 때 이전 변위가 남지 않는다.
- [ ] 운행 중 회수는 양쪽 전력 변경 없이 거절된다.
- [ ] 상층 회수와 하층 고정 정책을 문장·화면 안내로 확정했다.
- [ ] 저프레임 탑승과 천장 조건을 영상으로 남겼다.

## 선택과 한계

명시적 변위는 위치 변경자를 Motor로 유지해 디버깅하기 쉽다. 바닥 식별·업데이트 순서 비용이 생긴다. 회전 플랫폼·다수 탑승자·물리 Rigidbody 승강기는 후속 실험이며 이번 직선 방식의 검증을 대신하지 않는다.
