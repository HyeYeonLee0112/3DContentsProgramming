# T12 — 전력을 옮겨도 보유량이 깨지지 않는 규칙 만들기

한쪽에서 전력을 뺀 뒤 반대쪽이 가득 찼다고 알게 되면 전력이 사라질 수 있다. 양쪽 조건을 먼저 확인하고 두 값을 함께 바꾼다.

선행: [T08](08-health.md) · 다음: [T13](13-absorption.md)

## 문제와 개념

일반적인 전력 이전은 양쪽 합계를 보존해야 한다. 한 번의 처리에서 검증을 끝내고 두 값을 함께 바꾸는 것을 여기서는 원자적 이전이라 부른다. CPU 스레드용 lock을 넣는다는 뜻은 아니다. Unity 메인 스레드에서 중간 이벤트·대기 없이 실행한다.

## 파일 책임과 구현 계약

| 파일 | 필요한 멤버 | 핵심 로직·실패 |
|---|---|---|
| `Gameplay/Power/Rules/PowerStore.cs` 순수 C# | `public int Capacity { get; }`; `public int Amount { get; private set; }`; `public PowerStore(int capacity, int initial)` | capacity>0, initial 0~capacity; 잘못된 생성은 ArgumentOutOfRangeException |
| Store | `internal void SetAmount(int amount)` | 범위 밖 대입 예외; 알림·UI 호출 없음 |
| Store | `public void Restore(int amount)` | 복원 전용 범위 검증 대입; 내부 SetAmount 호출, 보상 알림 없음 |
| `Gameplay/Power/Rules/PowerTransfer.cs` | `public static bool TryTransfer(PowerStore source, PowerStore target, int amount)` | null·동일 객체·0이하·부족·초과면 false; 변경 없음 |
| `Gameplay/Power/PowerInventory.cs` MonoBehaviour | `public PowerStore Store { get; private set; }`; `private void Awake()` | 플레이어 최대 3칸의 실행 상태 생성 |
| Inventory | `public void Restore(int amount)` | 검증 후 Store.Restore; 보상 이벤트 발행 안 함 |
| `Tests/EditMode/PowerTransferTests.cs` | 부족·가득 참·동일 대상·성공·잘못된 양에 대한 테스트 메서드 | 합계 보존과 실패 시 양쪽 보존 |

순수 규칙은 GameObject·소리·HUD를 모른다. 이전 성공 후 호출자가 시설/HUD를 갱신한다. 두 Store 사이에 변경 이벤트를 발행하면 미완료 상태가 관찰되므로 하지 않는다.

## 먼저 생각할 질문

플레이어 2칸, 시설 1칸에서 시설로 2칸을 옮길 때 시설 용량이 2면 어떻게 해야 하는가? 이 단계는 부분 이전 없이 전체 거절한다.

```text
대상·양 검사 → 보유량·여유 공간 검사 → 변경 후 값 둘 계산
source.SetAmount → target.SetAmount → true
앞 단계 거절은 양쪽 변경 없음
```

## TODO 실습

```csharp
public static bool TryTransfer(PowerStore source, PowerStore target, int amount)
{
    // TODO: 어떤 상태도 바꾸기 전에 모든 거절 조건을 확인한다.
    // TODO: 두 값이 모두 범위 안임이 확인되면 변경한다.
    return false;
}
```

힌트: `target.Amount + amount` 대신 `amount <= target.Capacity - target.Amount`로 공간 비교 → 동일 참조 거절 → 변경 사이에 await/이벤트 금지.

[PowerStore·이전 참고 구현과 경계값](answers/03-power-checkpoint.md#power)

## 따라하기와 테스트 연결

1. 순수 타입 두 개를 먼저 작성하고 Console 컴파일 오류를 해결한다. using UnityEngine 없이 동작할 수 있어야 한다.
2. Window → General → Test Runner에서 EditMode 테스트 구성을 만든다. 순수 규칙 두 파일만 둔 `Gameplay/Power/Rules`에 `Blackout.Power.Rules.asmdef`를 만들고 테스트 asmdef에서 참조한다. Auto Referenced를 켜 기존 호스트에서 사용하게 한다. Power 폴더 전체에 만들면 기존 Assembly-CSharp의 Absorb/Health 등과 참조가 꼬일 수 있으므로 순수 규칙 범위만 나눈다. 외부 호스트의 복원은 공개 Restore를 사용한다.
3. `(3,2) → (2,0)`에 1 이전: `(1,1)`을 예상값으로 적고 확인한다. 같은 source/target은 false다.
4. 부족·가득 참·0·음수·null·동일 객체·매우 큰 양에서 실패와 양쪽 보존을 검사한다. 실제 요구에서 기대값을 도출한다.
5. 두 Inventory 인스턴스의 Store가 서로 다른 객체인지 본다. 설정 에셋에 현재 전력을 저장하지 않는다.

## 완료 기준

- [ ] 성공 시 합계가 같고 양쪽 값이 용량 안이다.
- [ ] 실패 시 양쪽 값이 이전과 같다.
- [ ] 생성 오류와 정상적인 이전 거절을 구분한다.
- [ ] 화면 없이 자동 검사를 실행하고 결과를 기록했다.
- [ ] 같은 설정의 두 개체가 현재 전력을 공유하지 않는다.

## 이득·비용·읽기

순수 규칙은 장면 없이 실패 조건을 반복 검사할 수 있다. Unity 연결용 Inventory와 알림을 별도 작성하는 비용이 생긴다. 더 단순한 단일 스크립트도 가능하지만 자원 중복·복원까지 다룰 BLACKOUT에는 검증 경계가 유용하다. [공통 자료](CONTRACTS.md)의 CharacterAgent에서 검증 후 변경 순서를 읽는다.
