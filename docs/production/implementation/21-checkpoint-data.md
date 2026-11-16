# T21 — 현재 상태와 독립적인 체크포인트 기록 만들기

저장한 리스트를 현재 게임과 함께 쓰면 적을 처치할 때 저장 내용도 바뀔 수 있다. 기록은 숫자·문자열·값 객체를 복사한 독립 자료여야 한다.

선행: [T16](16-lift.md), [T19](19-encounters.md), [T20](20-game-flow.md) · 다음: [T22](22-checkpoint-restore.md)

## 개념·소유권

스냅샷은 특정 시점의 값들을 따로 보관한 기록이다. GameObject·Health 참조를 저장하는 것과 다르다. Snapshot은 메모리에 보관하며 이번 필수 범위는 같은 장면 복원이다. 저장 파일은 [E14](E14-save-version.md)의 별도 실험이다.

## 구현 계약

| 파일 | 필요한 멤버 | 의미·실패 |
|---|---|---|
| `Gameplay/Checkpoints/PlayerRecord.cs` | `public readonly struct PlayerRecord`; `public Vector3 Position { get; }`; `public Quaternion Rotation { get; }`; `public float Health { get; }`; `public int Power { get; }`; `public PlayerRecord(Vector3 position, Quaternion rotation, float health, int power)` | Unity 값 형식·수치 복사 |
| `Gameplay/Checkpoints/EnemyRecord.cs` | `public readonly struct EnemyRecord`; `public bool Dead { get; }`; `public bool ResidueCollected { get; }`; `public EnemyRecord(bool dead, bool residueCollected)` | 고정 배치 기준 상태 |
| `Gameplay/Checkpoints/CheckpointSnapshot.cs` | `private readonly Dictionary<string,int> _facilityPower`; `private readonly Dictionary<string,EnemyRecord> _enemies`; `private readonly Dictionary<string,bool> _encounters, _stations` | 외부 컬렉션을 생성자에서 복사 |
| Snapshot | `public PlayerRecord Player { get; }`; `public CheckpointSnapshot(PlayerRecord player, IReadOnlyDictionary<string,int> facilities, IReadOnlyDictionary<string,EnemyRecord> enemies, IReadOnlyDictionary<string,bool> encounters, IReadOnlyDictionary<string,bool> stations)` | null·중복·빈 ID 검증 |
| Snapshot | `public bool TryGetFacility(string id, out int amount)`; `public bool TryGetEnemy(string id, out EnemyRecord record)`; `public bool TryGetEncounter(string id, out bool complete)`; `public bool TryGetStation(string id, out bool used)` | 읽기 전용 조회; 없는 ID false |
| Snapshot | `private static Dictionary<string,T> Copy<T>(IReadOnlyDictionary<string,T> source)` | 생성자 입력을 독립 복사; null·빈 ID 예외 |
| `Gameplay/Checkpoints/CheckpointCoordinator.cs` 시작 | `private CheckpointSnapshot _snapshot`; `public void Capture()` | 명시 등록 대상의 값을 수집·새 Snapshot으로 교체 |
| `Gameplay/Checkpoints/EnemyCheckpointBinding.cs` | `[System.Serializable] public struct EnemyCheckpointBinding`; `public EncounterMember Member`; `public ResidualPowerDrop Reward` | 적 ID·생존과 그 적의 잔류 회수값을 한 등록 항목으로 연결 |

Coordinator의 Inspector 필드는 `[SerializeField] private Transform _player`, `Health _health`, `PowerInventory _inventory`, `FacilityPower[] _facilities`, `EnemyCheckpointBinding[] _enemies`, `EncounterController[] _encounters`, `OneUseStation[] _stations`다. 각 필드에 SerializeField를 적용한다. 적 항목은 Member.Id·IsDead와 Reward.Collected로 기록한다. Station·Enemy의 공개 Id는 고유 ID 규칙을 따르며 추가 때 제작 검사도 갱신한다.

## 기록 범위와 제한

| 기록 | 저장할 값 |
|---|---|
| 플레이어 | 안전한 체크포인트 위치·방향, 체력, 전력 |
| 시설 | ID별 저장 전력; 조명·포탑 표시값은 저장하지 않고 재계산 |
| 승강기 | 정차한 하단 체크포인트에서만 저장; 운행 중 Capture 거절 |
| 고정 적·조우 | 사망·잔류 회수·조우 완료; 살아있는 적은 제작한 초기 위치에서 복원 |
| 충전소 | ID별 사용 여부 |
| 보스 | 진입 전 저장만 허용; 중간 전투 저장 없음 |

## 질문·의사코드

`IReadOnlyDictionary`로 매개변수를 받으면 자동 복사되는가? 원본 Dictionary를 외부에서 바꿔도 저장값이 그대로여야 한다.

```text
저장 가능한 안정 상태인지 확인 → 등록 ID·범위 검사
현재 값을 새 Dictionary와 값 구조체로 복사 → 새 Snapshot 생성
모든 수집 성공 후에만 기존 _snapshot 교체
```

## TODO 실습

```csharp
public bool TryGetFacility(string id, out int amount)
{
    // TODO: 외부에 수정 가능한 Dictionary를 넘기지 않고 조회만 제공한다.
    amount = default;
    return false;
}
```

힌트: 인터페이스는 허용 연산을 좁히며 복사는 별도 → 값 구조체 안의 참조 필드도 깊이 점검 → ID 순서에 의존하지 않기.

[독립 Dictionary 복사 참고 구현](answers/03-power-checkpoint.md#snapshot)

## 따라하기·검증

1. PlayerRecord·EnemyRecord를 작성하고 생성 후 값이 바뀌지 않는지 확인한다.
2. Snapshot 생성자에서 입력 목록을 자기 Dictionary로 복사한다. 외부에 내부 Dictionary를 그대로 반환하지 않는다.
3. Coordinator는 등록 배열을 순회하며 중복·누락 ID를 먼저 검사한다. 실패한 수집은 기존 정상 Snapshot을 덮지 않는다.
4. Capture 후 플레이어 전력·시설 전력·적 상태·Station 상태를 바꾼다. Snapshot의 TryGet 결과가 과거값인지 EditMode 검사한다.
5. 체크포인트 Trigger는 마지막 안정 구역에서 한 번 Capture한다. 운행 중/흡수 중/복원 중 저장을 거절하거나 작업 취소 후 안정 상태에서 저장하는 정책을 명시한다.

## 완료 기준

- [ ] 현재 컬렉션을 바꿔도 저장값이 바뀌지 않는다.
- [ ] GameObject 참조를 저장 데이터로 취급하지 않는다.
- [ ] 중복·빈 ID와 범위 오류가 저장 전에 발견된다.
- [ ] 잘못된 새 저장이 기존 정상 저장을 보존한다.
- [ ] 저장 가능 시점과 자동 검사 결과를 남겼다.

## 구조의 이득·비용

독립 기록은 반복 복원의 기준점이 된다. 복사 메모리와 ID 관리 비용이 생긴다. 장면 전체 객체를 직렬화하는 대안은 간편해 보여도 이번 상태·취소·보상 규칙을 명확히 설명하기 어렵다.
