# T23 — 2단계 보스와 전력 약점 순환 만들기

보스가 새로운 조작을 요구하면 앞 구간에서 배운 전력 규칙이 끊긴다. 기존 사격·공격 예고·전력 주입을 재조합하고 공격 중단을 확실히 만든다.

선행: [T10](10-charge.md), [T18](18-turret.md), [T22](22-checkpoint-restore.md) · 다음: [T24](24-level-integration.md)

## 규칙부터 확정하기

- 1단계는 직선 사격과 바닥 충격을 번갈아 예고한다.
- 체력 50% 이하에서 2단계로 한 번 전환한다. 진행 중 공격을 먼저 취소한다.
- 냉각기 2칸 통전은 코어를 8초 노출한다. 노출 종료 후 전력 회수·재주입으로 다음 순환을 시작한다.
- 2단계 닫힌 코어에는 피해를 주지 않는다. 열린 코어에는 기본탄도 피해를 주고 충전탄은 유리한 수치를 사용한다. 전력을 써야 열린다는 PRD 조건을 유지한다.
- 2회 순환으로 처치 가능한지는 체력·피해·발사 간격을 실제 플레이로 조정한다. 계산만으로 플레이 검증을 대신하지 않는다.

## 책임·구현 계약

| 파일 | 필요한 멤버 | 동작·실패 |
|---|---|---|
| `Gameplay/AI/BossState.cs` | `enum BossState { PhaseOne, Transition, PhaseTwoClosed, PhaseTwoOpen, Dead }` | 보스 큰 흐름 |
| `Gameplay/AI/BossController.cs` | `[SerializeField] private Health _health`; `[SerializeField] private FacilityPower _cooler`; `[SerializeField] private GameFlow _flow`; `private float _weakTime`; `private bool _phaseTwoEntered, _coolerArmed`; `public BossState State { get; private set; }` | 단계·노출 타이머·재주입 준비 |
| Boss | `private void OnEnable()/OnDisable()`; `private void Update()`; `private void OnHealthChanged()`; `private void OnCoolerChanged()`; `public void CancelAction()`; `public void ResetBoss()` | 상태 구독·해제·취소·초기화 |
| `Gameplay/Combat/BossWeakPoint.cs` | `[SerializeField] private BossController _boss`; `[SerializeField] private Health _health`; `public bool TryApplyDamage(float amount)` | IDamageable 구현; 현재 약점 상태에서만 Health 전달 |
| Boss 표현 | 단계·예고·코어 노출·체력 표시 | 화면은 State/Health를 읽고 규칙을 바꾸지 않음 |

보스 몸통에서 부모 Health를 직접 찾아 약점 방어를 우회하지 않게 Collider별 피해 경계를 정한다. 닫힌 약점 Collider를 비활성화하면 탄환이 몸을 통과할 수 있으므로 충돌·피해 거절을 따로 검토한다.

## 먼저 생각하기·의사코드

2칸이 계속 꽂힌 상태에서 8초가 끝나면 자동으로 다시 8초 열려야 하는가? 이번 규칙은 아니다. 냉각 종료 후 비통전→재통전이 있어야 새 순환이다.

```text
Health 변경 → 사망 최우선 → 50% 첫 진입이면 공격 취소·Transition
2단계 준비 → 냉각기 통전 상승 + armed → 8초 Open, armed false
노출 만료 → Closed, 전력은 그대로 남음
냉각기 비통전 → 다음 재통전에 대비해 armed true
```

## TODO 실습

```csharp
private void OnCoolerChanged()
{
    // TODO: 2단계·생존·현재 통전과 재준비 상태를 검사한다.
    // TODO: 조건을 만족한 새 주입에서만 노출 시간을 시작한다.
}
```

힌트: 전환과 사망은 예고 완료보다 우선 → Invoke/코루틴을 쓰면 CancelAction에서 중단 → 상태 진입마다 타이머 초기화.

## 따라하기·연결

1. 보스 대역 모델·Health·코어 Collider부터 만들고 기본 피해 경계를 확인한다.
2. 1단계 공격 두 가지를 각각 만들고 예고·실행·회복의 시점을 보이게 한다. 기존 Gun과 범위 검사를 재사용한다.
3. 체력 50% 경계 전후를 개발용 피해로 여러 번 넘겨 전환이 한 번인지 확인한다.
4. 냉각기 FacilityPower 요구량 2를 연결하고 노출 8초·만료·회수·재주입을 검증한다.
5. Boss HUD와 코어 상태 문구를 동시에 붙인다. 빛이 꺼져도 예고·코어 상태가 읽혀야 한다.
6. 예고 중 사망·단계 전환 중 사망·노출 직전 복원·일시정지를 시험한다. ResetBoss는 진입 전 Snapshot 복원과 연결한다.

## 완료 기준

- [ ] 체력 50%에서 이전 공격을 끝내고 한 번 전환한다.
- [ ] 전력 2칸으로 8초 노출하며 회수·재주입이 다음 순환을 연다.
- [ ] 닫힌 2단계 약점은 피해를 거절하고 열린 약점은 받는다.
- [ ] 사망·정지·복원 뒤 이전 공격이 남지 않는다.
- [ ] 두 순환 처치 가능 여부와 공격 가독성을 영상으로 검증했다.

## 구조의 이득과 비용

큰 단계와 개별 공격을 구분하면 취소 우선순위를 설명하기 쉽다. 단계·공격·약점의 조합 검증이 늘어난다. Animator 상태만으로 피해 허용을 결정하는 단순 대안은 중단·복원 조건을 추적하기 어려워 이번 규칙의 주인으로 삼지 않는다.
