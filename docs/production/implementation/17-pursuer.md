# T17 — 길을 찾고 예고 후 공격하는 추적 로봇

적이 플레이어 위치로 달려오기만 하면 벽을 뚫고 보거나 죽은 뒤 공격을 끝낼 수 있다. 이동 경로, 감지, 행동 상태와 공격 중단을 구분한다.

선행: [T08](08-health.md) · 다음: [T18](18-turret.md), [T19](19-encounters.md)

## 개념·책임

NavMesh는 걸을 수 있는 공간의 지도다. NavMeshAgent가 그 위의 이동을 처리한다. 상태 머신은 현재 행동 하나와 가능한 전환을 명시한다. EnemyBrain은 행동·예고 타이머를, Health는 생존을 소유한다. Animator는 결과를 표시한다.

## 구현 계약

| 파일 | 필요한 멤버 | 역할·실패 |
|---|---|---|
| `Gameplay/AI/EnemyState.cs` | `enum EnemyState { Patrol, Chase, Windup, Recover, Search, Dead }` | 한 번에 한 상태 |
| `Gameplay/AI/EnemyBrain.cs` | `[SerializeField] private UnityEngine.AI.NavMeshAgent _agent`; `[SerializeField] private Transform _target, _eyes`; `[SerializeField] private Health _health`; `[SerializeField] private LayerMask _sightMask`; `[SerializeField] private Transform[] _patrolPoints` | Inspector 연결 |
| Brain | `[SerializeField] private float _viewDistance = 10f, _viewAngle = 100f, _attackRange = 1.8f`; `private float _timer`; `private int _patrolIndex`; `private Vector3 _lastSeen`; `public EnemyState State { get; private set; }` | 탐지·행동 상태 |
| Brain | `private void Awake()`; `private void OnEnable()/OnDisable()`; `private void Update()` | 참조 검사·Died 구독·해제·상태별 처리 |
| Brain | `private bool CanSeeTarget()`; `private void EnterState(EnemyState next)`; `public void CancelAction()` | 거리→내적 시야각→벽 Raycast; 전환 때 타이머/경로 정리 |
| Brain | `private void OnDied()` | EnterState(Dead); 공격·경로 중단; OnDisable도 CancelAction 호출 |
| Brain | `public void RestoreAlive(Vector3 position)` | T22에서 Warp·공격 상태 초기화; 복원 후 행동 시작은 흐름 담당 |

피해는 대상의 IDamageable로 보낸다. 이 단계의 근거리 공격은 Windup 시작 0.6초 뒤 **그때도** 생존·거리·시야가 맞을 때 한 번 적용한다. 0.6초는 PRD 값이다. _timer를 예고와 회복에서 재사용하면 상태 진입 때 반드시 초기화한다.

## 질문·전환표

| 현재 | 조건 | 다음 / 종료 처리 |
|---|---|---|
| Patrol/Search | 시야 확보 | Chase / 최근 위치 기록 |
| Chase | 공격 거리·시야 | Windup / 이동 정지·0.6초 예고 |
| Windup | 시간 완료 | 한 번 공격 후 Recover |
| Chase | 시야 소실 | Search / 마지막 발견 위치로 이동 |
| 어떤 생존 상태 | 사망 | Dead / 예약 공격·경로 삭제 |

```text
감지 → 현재 상태 판단 → 유효 전환 → 이전 행동 정리 → 새 행동 진입
경로 없음/불완전 → 멈춤·재탐색 간격 → 실패 이유 표시
```

## TODO 실습

```csharp
public void CancelAction()
{
    // TODO: 예고·회복 타이머와 남은 공격을 무효화한다.
    // TODO: NavMesh 위의 유효 Agent에만 경로 정지를 요청한다.
}
```

힌트: `pathPending` 동안 remainingDistance만 믿지 않기 → `isOnNavMesh`, pathStatus 확인 → 죽음은 공격 완료보다 우선.

## 따라하기·연결

1. PlayerLab 복사 장면에서 바닥 Root에 NavMeshSurface를 붙인다. Agent Type·Include Layers·Geometry를 확인하고 Bake한다. 실제 지형 Collider를 기준으로 확인한다.
2. 로봇 Root에 Agent·Health·Brain을 붙인다. Agent와 Rigidbody/Root Motion이 동시에 위치를 소유하지 않게 한다.
3. 순찰만 먼저 구현하고 도달 불가 목적지를 일부러 넣어 본다. 다음 감지·추적, 마지막 공격 순서로 늘린다.
4. Eyes를 적 눈 높이에 놓고 Ray가 자기 Collider를 먼저 맞지 않게 한다. 시야각·레이를 Gizmo로 표시한다.
5. 공격 예고는 바닥·몸 발광 등 최소 표시와 소리로 붙인다. 타깃이 나갔는데도 이전 대기 공격이 실행되면 CancelAction과 상태 전환을 확인한다.
6. 전력 보상은 T14 Drop과 연결한다. Agent 사망·표현 종료와 보상 처리를 한 거대한 메서드로 합치지 않는다.

## 완료 기준

- [ ] 바닥·문·좁은 통로에서 경로 성공/실패를 구분한다.
- [ ] 벽 뒤 플레이어를 직접 보지 않는다.
- [ ] 0.6초 예고 후 유효할 때만 한 번 피해를 준다.
- [ ] 공격 예고 중 사망하면 공격이 취소된다.
- [ ] 상태 전환 이유와 막힌 경로 증거를 남겼다.

## 선택과 한계

상태 enum은 간단하고 전환을 추적하기 쉽다. 상태 수와 복합 행동이 늘면 분리 비용이 생긴다. 행동 트리와의 비교는 [E12](E12-architecture.md)에서 같은 적 하나로 한다. 초기부터 AI 전체를 도구에 맡기지 않는다.
