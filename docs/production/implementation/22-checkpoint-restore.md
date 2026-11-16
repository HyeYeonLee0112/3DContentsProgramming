# T22 — 같은 장면에서 안전하게 복원하고 10번 죽어보기

위치만 옮기면 이전 탄환·흡수·보상·적 공격이 남는다. 진행 중 작업을 먼저 끝내고 저장값을 적용한 뒤 행동을 다시 허용한다.

선행: [T21](21-checkpoint-data.md) · 다음: [T23](23-boss.md)

## 책임·변경 계약

| 대상 | 추가·변경 멤버 | 동작·실패 |
|---|---|---|
| Coordinator | `[SerializeField] private GameFlow _flow`; `private bool _restoring`; `private int _generation`; `public bool TryRestore()` | 읽기 전용으로 Restoring 사전조건 검사; 중복 거절·세대·적용 순서; SetState 호출 금지 |
| Coordinator | `private bool ValidateSnapshot()`; `private void CancelCurrentActions()`; `private void ApplySnapshot()`; `private void RefreshPresentation()` | 검사/취소/상태/표시 구분 |
| ProjectilePool | `public void ReleaseAllActive()`; 활성 대여 목록 필드 | 활성 목록의 복사본으로 반환; 순회 중 수정 주의 |
| Motor | 기존 `StopMotion()` | 수직·외부 발판 변위 삭제 |
| Brain/Turret/Gun/Absorb | 기존 취소·복원 함수 | 이전 요청을 모두 무효화 |
| Facility/Health/Inventory/Station/Encounter | 기존 Restore 계열 | 일반 상호작용·사망 보상을 재호출하지 않고 값 적용 |
| GameSession 추가 | `[SerializeField] private CheckpointCoordinator _checkpoints`; `private bool _retryPending`; `public void RequestRetry()`; `public void ProcessPendingRetry()` | 버튼은 요청만 기록; 안전 지점에서 Restoring→TryRestore→결과에 따라 Playing/Dead |

세션 전환은 [기술 설계 v0.1](../BLACKOUT_TechnicalDesign.md#critical-paths)을 따른다. Coordinator가 GameSession을 참조하거나 게임 상태를 바꾸지 않는다. 개별 Restore의 즉시 일반 알림은 억제하고 전체 적용 뒤 RefreshPresentation에서 갱신한다. 이 계약으로 기존 Restore 호출부·알림 경로를 함께 정리한다.

세대 번호는 이전 작업이 끝나도 현재 실행에 반영하지 않도록 구분하는 값이다. 현재 동기 타이머만으로 취소가 충분하면 사용하지 않아도 된다. 나중의 비동기 실험에서는 작업 시작 때 세대를 기억하고 완료 때 비교한다.

## 복원 순서

```text
GameSession이 재시도 요청을 한 번 소비하고 Restoring 전환
Coordinator가 Snapshot와 모든 대상 ID·범위·재배치 위치를 값 적용 전에 검증
입력/AI/흡수/충전/연출 중단 확인 → 활성 탄환 정리
CharacterController 잠시 끄기 → 안전 위치·방향 → StopMotion → 다시 켜기
Health/전력 → 시설/승강기 → 적/잔류 → 조우/충전소 순서로 적용
HUD·조명·포탑 표시 재계산 → 결과 반환 → GameSession이 성공 후 Playing
```

Agent 위치는 유효 NavMesh 위치에서 Warp한다. 살아있는 적의 Health 복원·이동·상태 초기화 이후에 AI를 허용한다. 죽은 적의 Died를 다시 발생시켜 잔류를 생성하지 않는다.

## TODO 실습

```csharp
public bool TryRestore()
{
    // TODO: 중복 요청과 누락 기록·ID를 먼저 거절한다.
    // TODO: GameFlow가 Restoring인지 검사하고 취소 → 값 적용 → 표시를 수행한다.
    // 결과만 반환한다. GameSession이 성공 여부에 따라 상태를 전환한다.
    return false;
}
```

힌트: 이 메서드가 모든 규칙을 직접 재구현하지 않게 각 대상 Restore 호출 → 사전검증 실패는 현재값 보존 → 적용 중 예외는 Dead 상태·원인 로그, 임의 재개 금지.

## 따라하기·연결

1. 우선 플레이어 위치·체력·전력만 복원하고 Controller 충돌·잔류 속도를 확인한다.
2. 시설·승강기, 적·잔류, 조우·충전소를 차례로 추가한다. 각 추가 후 이전 항목을 재검사한다.
3. 사망 화면 재시도 버튼을 GameSession.RequestRetry에 연결한다. Driver의 안전 지점에서 ProcessPendingRetry를 한 번 처리한다. 클릭 연타·상태 알림 중 재진입으로 두 번 복원하지 않는다.
4. 흡수 중 죽기, 전력 주입 후 죽기, 충전소 사용 후 죽기, 적 사망 직후 죽기를 각각 실행한다.
5. 동일 Snapshot으로 10회 복원한다. 전력 합계·적 수·잔류 수·충전소·현재 ID를 표에 기록한다.
6. 생성한 적/잔류의 주인을 확인하고 복원 전 개체를 정리한 뒤 필요한 개체만 만든다. 리스트에 이전 참조가 누적되지 않게 한다.
7. 저장 ID 하나를 일부러 누락해 검증 실패가 발견되는지 본다. 검사 전 일부 시설부터 변경하면 안 된다.

## 검증·완료 기준

- [ ] 복원 도중 신규 공격·흡수·입력이 없다.
- [ ] 전력·시설·적·잔류·조우·충전소가 기록과 일치한다.
- [ ] 같은 기록 10회 복원에서 개체·보상·구독이 늘지 않는다.
- [ ] 누락 ID·중복 재시도는 구체적 이유와 함께 안전하게 거절된다.
- [ ] 반복 결과와 Memory/Profiler 관찰 자료를 남겼다.

## 이득·비용·한계

명시적 복원 순서는 오래된 작업이 새 상태를 덮는 문제를 줄인다. 등록 대상과 복원 계약을 유지해야 한다. 사전검증은 원자적 롤백을 자동 보장하지 않는다. 적용 중 예외까지 완전 복구하려면 별도 백업·롤백이 필요하며 이번 버전은 실패 시 멈추고 원인을 드러내는 정책이다.
