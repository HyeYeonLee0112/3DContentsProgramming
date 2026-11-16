# 전력 이전·기록 복사 참고 구현

화면과 분리된 작은 규칙을 비교하는 자료다. 직접 구현한 코드의 거절 조건과 변경 순서를 먼저 적고 확인한다. 이 문서 작성 중 Unity 컴파일·Test Runner를 실행한 결과는 아니다.

<a id="power"></a>
## T12 — 저장량과 한 번의 이전

계약은 T12의 두 순수 C# 타입이다. PowerStore의 생성자는 잘못된 설정을 예외로, TryTransfer는 예상 가능한 거절을 false로 표현한다. SetAmount는 같은 규칙 assembly의 내부 변경 지점이며 이벤트를 발행하지 않는다. 공개 Restore는 외부 호스트의 복원에 사용하며 일반 획득은 TryTransfer로 처리한다.

```csharp
using System;

namespace Blackout.Gameplay.Power
{
    public sealed class PowerStore
    {
        public int Capacity { get; }
        public int Amount { get; private set; }

        public PowerStore(int capacity, int initial)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (initial < 0 || initial > capacity)
                throw new ArgumentOutOfRangeException(nameof(initial));
            Capacity = capacity;
            Amount = initial;
        }

        internal void SetAmount(int amount)
        {
            if (amount < 0 || amount > Capacity)
                throw new ArgumentOutOfRangeException(nameof(amount));
            Amount = amount;
        }

        public void Restore(int amount)
        {
            SetAmount(amount);
        }
    }

    public static class PowerTransfer
    {
        public static bool TryTransfer(PowerStore source, PowerStore target, int amount)
        {
            if (source == null || target == null || ReferenceEquals(source, target))
                return false;
            if (amount <= 0 || amount > source.Amount)
                return false;
            if (amount > target.Capacity - target.Amount)
                return false;

            int sourceAfter = source.Amount - amount;
            int targetAfter = target.Amount + amount;
            source.SetAmount(sourceAfter);
            target.SetAmount(targetAfter);
            return true;
        }
    }
}
```

프로젝트에서는 클래스별 파일로 나눈다. 동기 메서드의 두 변경 사이에 콜백·await·다른 스레드가 끼지 않는 전제다. lock으로 모든 게임 상태가 안전해진다고 해석하지 않는다. 알림은 TryTransfer가 true를 반환한 뒤 호스트가 보낸다.

### 직접 만들 검사

| source / target | 이전량 | 기대 |
|---|---:|---|
| 용량3·보유2 / 용량2·보유0 | 1 | true, 보유1/1, 합계2 |
| 용량3·보유0 / 용량2·보유0 | 1 | false, 0/0 유지 |
| 용량3·보유2 / 용량2·보유2 | 1 | false, 2/2 유지 |
| 같은 객체 | 1 | false, 원래값 유지 |
| 정상 두 객체 | 0·음수·int.MaxValue | false, 원래값 유지 |

Source의 처음 3칸과 대상 시설의 1~2칸은 별도 Store다. 생성·사망 보상·충전소 공급은 일반 이전 합계와 구분해 테스트한다.

[T12로](../12-power-rules.md)

<a id="snapshot"></a>
## T21 — 입력 컬렉션과 독립적으로 복사하기

추가 구현 계약: CheckpointSnapshot에 `private static Dictionary<string,T> Copy<T>(IReadOnlyDictionary<string,T> source)`를 작성한다. null·빈 ID는 프로그래머 오류로 예외다. T21의 값은 int·bool·읽기 전용 EnemyRecord이므로 이 복사로 독립 기록이 된다. 참조형 값을 넣으면 그 내부까지 자동 복사되지는 않는다.

```csharp
private static Dictionary<string, T> Copy<T>(IReadOnlyDictionary<string, T> source)
{
    if (source == null) throw new System.ArgumentNullException(nameof(source));
    var copy = new Dictionary<string, T>(System.StringComparer.Ordinal);
    foreach (var pair in source)
    {
        if (string.IsNullOrWhiteSpace(pair.Key))
            throw new System.ArgumentException("체크포인트 ID가 비어 있습니다.");
        copy.Add(pair.Key, pair.Value);
    }
    return copy;
}
```

T21의 Snapshot 생성자에서 `_facilityPower = Copy(facilities)`, `_enemies = Copy(enemies)`, `_encounters = Copy(encounters)`, `_stations = Copy(stations)`로 연결한다. 각 값의 범위·존재 ID·용량은 Capture/복원 사전검증에서 함께 확인한다.

계약의 조회 함수 한 개는 다음과 같다. 나머지 조회도 각 레코드 타입으로 동일한 책임을 수행한다.

```csharp
public bool TryGetFacility(string id, out int amount)
{
    if (string.IsNullOrWhiteSpace(id))
    {
        amount = default;
        return false;
    }
    return _facilityPower.TryGetValue(id, out amount);
}
```

### 왜 이렇게 복사하는가

IReadOnlyDictionary 매개변수는 이 함수가 수정하지 않는다는 계약이며 원본이 다른 곳에서 변하지 않는다는 보장이 아니다. 자기 Dictionary로 복사하고 내부 컬렉션을 외부에 반환하지 않으면 현재 게임의 변경과 저장 기록을 구분할 수 있다.

원본 시설 Dictionary의 값을 바꾸거나 항목을 지운 뒤에도 Snapshot 조회 결과가 동일한지 확인한다. 이후 같은 Snapshot을 10번 복원해 시설·전력·적·충전소가 일치하는지 T22에서 검사한다.

[T21로](../21-checkpoint-data.md)
