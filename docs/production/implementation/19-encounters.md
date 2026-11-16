# T19 — 정해진 조우를 한 번씩 완료하고 다음 목표 열기

적을 계속 무작위 생성하면 체크포인트가 무엇을 복원해야 하는지 불분명해진다. 고정 조우별 적과 완료 상태를 명확히 관리한다.

선행: [T14](14-rewards-station.md), [T17](17-pursuer.md), [T18](18-turret.md) · 다음: [T20](20-game-flow.md)

## 책임·구현 계약

| 파일 | 필요한 멤버 | 동작·실패 |
|---|---|---|
| `Gameplay/AI/EncounterMember.cs` | `[SerializeField] private string _id`; `[SerializeField] private Health _health`; `public string Id => _id`; `public bool IsDead => _health.IsDead`; `public event System.Action Changed`; `private void OnEnable()/OnDisable()` | 적의 고정 ID와 사망 알림 전달 |
| `Gameplay/AI/EncounterController.cs` | `[SerializeField] private string _id`; `[SerializeField] private EncounterMember[] _members`; `public string Id => _id`; `public bool Completed { get; private set; }`; `public event System.Action CompletedChanged` | 조우 ID·등록 적·완료 상태 |
| Encounter | `private void Awake()`; `private void OnEnable()/OnDisable()`; `private void Refresh()`; `public void RestoreCompleted(bool completed)` | ID 중복·빈 목록 검사, 모두 사망 때 한 번 완료 |
| `Presentation/UI/ObjectivePresenter.cs` | `[SerializeField] private EncounterController _encounter`; `[SerializeField] private TMPro.TMP_Text _label`; `private void OnEnable()/OnDisable()`; `private void Refresh()` | 현재 목표·완료 이유 표시 |

## 질문·의사코드

적 두 마리가 같은 프레임에 죽으면 완료 보상은 두 번 나오는가? 복원 때문에 죽은 적을 숨겼을 때 OnDisable을 사망으로 세도 되는가?

```text
고정 ID와 등록 목록 검사 → 사망 변경 구독
현재 모든 멤버 IsDead 검사 → 미완료에서 완료로 갈 때만 알림
복원은 저장값 적용 후 표시 재계산; 신규 보상 발생 금지
```

## TODO 실습

```csharp
private void Refresh()
{
    // TODO: _members의 현재 사망 상태로 완료 여부를 결정한다.
    // TODO: 이미 완료된 조우의 성공 효과를 반복하지 않는다.
}
```

힌트: 활성 오브젝트 개수 대신 등록된 적 상태 → OnDisable은 사망이 아님 → 완료 이벤트는 상태 전환 알림.

## 따라하기·연결

1. 컨테이너 통로 조우에 적 2마리를 고정 배치하고 각 `cargo.e01.robot01/02` 같은 ID를 준다.
2. 별도 Encounter 오브젝트를 만들고 두 Member를 배열로 연결한다. 임의 전역 Find로 장면 전체 적을 모으지 않는다.
3. 한 마리 처치→남은 목표 표시, 모두 처치→다음 진입 조건을 연결한다. 공간 연출은 목표 상태를 읽는다.
4. 개별 적이 꺼져도 사망했다고 세지 않는지 확인한다. 풀링·비활성은 행동 수명이며 죽음의 증거가 아니다.
5. 조우 완료 시 전력 보상을 추가 지급하지 않는다. 이미 T14 적 잔류 전력이 있는지 확인해 보상 경로를 하나로 유지한다.
6. 체크포인트에 필요한 조우 ID·적 ID·사망·잔류 회수 상태를 T21 목록으로 넘긴다.

## 완료 기준

- [ ] 등록된 조우 적만 완료 판정에 들어간다.
- [ ] 동시 사망에도 완료 연출이 한 번 실행된다.
- [ ] 비활성화가 사망으로 오인되지 않는다.
- [ ] 중복 ID·누락 참조를 찾을 수 있다.
- [ ] 다음 목표와 보상 중복 없는 결과를 기록했다.

## 구조의 이득과 한계

고정 목록은 저장·복원·레벨 테스트를 쉽게 한다. 적 배치 변경 때 목록을 관리해야 한다. 무한 웨이브는 별도 규칙이며 이번 유한 조우의 복원 모델에 무작정 섞지 않는다.
