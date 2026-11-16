# T14 — 처치 잔류 전력과 한 번 쓰는 충전소 만들기

사망 알림이 여러 번 오거나 복원 직후 다시 호출되면 보상이 복제된다. 사망 보상과 충전소의 사용 상태를 명시적으로 보관한다.

선행: [T13](13-absorption.md) · 다음: [T15](15-facilities.md), [T19](19-encounters.md)

## 책임과 계약

| 파일 | 필요한 멤버 | 주인·실패 |
|---|---|---|
| `Gameplay/Power/ResidualPowerDrop.cs` | `[SerializeField] private Health _health`; `[SerializeField] private AbsorbInteraction _residuePrefab`; `private bool _spawned`; `private AbsorbInteraction _residue` | 한 적의 한 보상 인스턴스 |
| Drop | `private void OnEnable()/OnDisable()`; `private void OnDied()` | Died 구독·해제; _spawned를 먼저 true로 하고 1칸 Store 생성·연결 |
| Drop | `public void RestoreReward(bool spawned, bool collected)` | 복원용 상태 설정; Died를 다시 발행하지 않음 |
| Drop | `private bool _restoredCollected`; `public bool Spawned => _spawned`; `public bool Collected { get; }` | 잔류 인스턴스가 있으면 Source.Amount==0, 없으면 복원된 회수값 반환 |
| `Gameplay/Power/StationChoice.cs` | `public enum StationChoice { Heal, Power }` | 회복 또는 전력 선택 |
| `Gameplay/Power/OneUseStation.cs` | `[SerializeField] private Health _health`; `[SerializeField] private PowerInventory _inventory`; `private PowerStore _supply`; `public bool Used { get; private set; }`; `private void Awake()` | 충전소 공급 1칸·사용 상태 |
| Station | `public bool TryUse(StationChoice choice)`; `public void RestoreUsed(bool used)` | 효과 적용 성공 때만 Used=true; 실패 때 보존 |
| Station | `[SerializeField] private string _id`; `public string Id => _id` | T21에서 사용할 고정 ID; 중복·빈 값 검사 |

보상 생성 시 플레이어 참조는 생성 담당이 전달한다. 프리팹이 장면의 플레이어를 영구 참조할 수 있다고 가정하지 않는다. 필요한 초기화 인자에 플레이어 Inventory·Health를 추가하고 호출부를 같이 갱신한다.

## 질문·의사코드

체력이 꽉 찼는데 회복을 누르면 충전소를 소모할 것인가? 이 실습은 효과가 없으면 사용을 거절한다. 사망 뒤 생긴 잔류 전력을 보지 못한 채 복원하면 무엇을 저장해야 하는가?

```text
Drop: 사망 첫 처리 → spawned 표시 → 잔류 전력 생성
Station: 미사용 확인 → 선택 효과 가능 확인 → 효과 적용 → Used true
Restore: 저장된 used/spawned/collected를 반영, 보상 생성 이벤트 재실행 금지
```

## TODO 실습

```csharp
public bool TryUse(StationChoice choice)
{
    // TODO: 사용 여부·선택의 효과 가능 여부를 검사한다.
    // TODO: 회복 또는 1칸 이전 성공 때만 사용 완료로 기록한다.
    return false;
}
```

힌트: Health.TryHeal과 PowerTransfer의 성공값을 사용 → 실패 전에 Used 변경 금지 → 복원은 일반 사용 함수와 별개.

## 따라하기·연결

1. 표적 로봇에 Drop을 붙이고 Health·잔류 프리팹을 연결한다. 생성 후 Source가 1칸인지 확인한다.
2. 죽은 로봇의 사망 이벤트를 개발 호출로 다시 보내거나 중복 충돌을 만들어 잔류 전력 수가 늘지 않는지 확인한다.
3. 충전소 대역을 놓고 월드 안내·선택 패널을 만든다. 버튼은 Heal/Power를 명시적으로 전달한다. 패널을 연 클릭이 총으로 이어지지 않게 Gate와 연결한다. 전체 메뉴 정책은 T20에서 통일한다.
4. 회복 효과와 전력 효과를 각각 확인하고 Used 후에는 두 선택 모두 거절한다.
5. 복원에서 필요한 사실인 `적 사망`, `보상 생성`, `잔류 회수`, `충전소 사용`을 구분해 T21 저장 목록에 적는다. Collected를 읽기 전에 잔류를 임의로 Destroy하지 않는다. 인스턴스를 없앨 때는 회수 사실을 보관한 뒤 정리한다.

## 검증·완료 기준

- [ ] 한 로봇이 잔류 전력 한 칸을 한 번만 남긴다.
- [ ] 잔류를 흡수하면 Source가 소진되어 재흡수되지 않는다.
- [ ] 충전소는 선택한 한 효과만 성공시키고 꺼진다.
- [ ] 가득 참·회복 불필요·중복 클릭은 소모하지 않는다.
- [ ] 저장해야 할 보상·사용 상태와 증거를 기록했다.

## 선택과 한계

보상을 Health 밖에 두면 체력 규칙을 재사용하기 쉽다. 대신 적 ID와 보상 ID를 연결해 복원해야 한다. 임시 랜덤 생성보다는 고정 조우별 보상으로 먼저 검증한다.
