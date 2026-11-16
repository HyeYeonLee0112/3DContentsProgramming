# T13 — 대상을 골라 0.7초 흡수하고 피격 때 취소하기

흡수 도중 맞거나 대상이 사라지면 기다린 결과를 지급하면 안 된다. 진행 표시와 실제 전력 이전을 나누고, 마지막 순간 조건을 다시 확인한다.

선행: [T12](12-power-rules.md) · 다음: [T14](14-rewards-station.md), [T15](15-facilities.md)

## 책임·소유권

Interactor가 가까운 대상 선택을, AbsorbInteraction이 한 대상의 흡수 진행을, PowerTransfer가 최종 이전을 담당한다. 0.7초는 PRD의 흡수 규칙이다. 타이머는 Update에서 dt를 누적하며 별도 스레드가 필요하지 않다.

## 구현 계약

| 파일 | 작성할 멤버 | 동작·실패 |
|---|---|---|
| `Gameplay/Interaction/IInteractable.cs` | `bool CanInteract()`; `bool TryInteract()`; `void CancelInteraction()` | 사용 가능/시작 성공/취소; UI 문구는 나중 별도 정보 |
| `Gameplay/Interaction/PlayerInteractor.cs` | `[SerializeField] private ThirdPersonCameraRig _camera`; `[SerializeField] private float _range = 2.5f`; `[SerializeField] private LayerMask _targets`; `private MonoBehaviour _currentComponent`; `private IInteractable _current` | 카메라 Ray+거리로 대상 선택; 파괴 확인은 Unity component로 |
| Interactor | `public void Tick(bool pressed)`; `public void CancelCurrent()` | 선택 변경 때 이전 취소; 누름 때 TryInteract; 실패는 안내 |
| `Gameplay/Power/AbsorbInteraction.cs` | `[SerializeField] private PowerInventory _player`; `[SerializeField] private Health _playerHealth`; `public PowerStore Source { get; private set; }`; `private float _elapsed`; `private bool _active` | 한 번의 흡수 상태 |
| Absorb | `public void Initialize(PowerStore source)` | Source 연결; null 거절; 진행 전 호출 |
| Absorb | IInteractable의 세 멤버, `public float Progress { get; }`, `private void Update()`, `private void OnEnable()/OnDisable()` | 체력 Damaged 구독·해제; 비활성·피격 취소; 0.7초 뒤 재검증·이전 |
| Controller 변경 | Interactor 참조·Tick(InteractPressed) | 조작 불허 때 CancelCurrent |

이 단계의 `TryInteract`는 시작만 성공한다. 전력 지급 성공은 별개다. 거리·시야 이탈 때 Interactor가 CancelCurrent를 호출하도록 선택 갱신을 매 프레임 실행한다.

## 먼저 생각하기·의사코드

흡수 시작 때 빈칸이 있었지만 완료 전 다른 보상을 받으면 어떻게 되는가? cancel을 두 번 호출해도 안전한가?

```text
시작: 거리·대상·용량·생존 확인 → active true, elapsed 0
진행: dt 누적 → 조건 이탈·피격·사망·비활성은 취소
완료: active false를 먼저 기록 → 조건 재검증 → TryTransfer 1칸
실패·취소: 지급 없음, 진행 표시 0
```

## TODO 실습

```csharp
public void CancelInteraction()
{
    // TODO: 여러 번 불려도 안전하게 진행 상태와 시간을 비운다.
    // 전력을 옮기거나 완료 알림을 보내지 않는다.
}
```

힌트: 완료 코드는 한 곳 → 이전 성공 때만 소리·HUD 갱신 → 피격/완료가 같은 프레임에 겹칠 때 실제 실행 순서를 로그로 확인해 정책 기록.

## 따라하기·Unity 연결

1. 잔류 전력 대역 구체와 상호작용 Collider를 만들고 Interactable 레이어를 지정한다. 탄환은 이 Trigger를 맞히지 않는다.
2. Source를 한 칸 Store로 초기화한다. 플레이어 Inventory·Health는 명시적으로 전달하거나 Inspector로 연결한다. 나중의 생성 보상은 T14의 초기화 담당이 연결한다.
3. PlayerInteractor를 Player_Root에 붙이고 Camera·마스크·거리를 설정한다. 겹치는 콜라이더는 가장 먼저 보이는 유효 대상 하나만 선택한다.
4. 월드 앞 안내와 진행 Slider를 붙인다. UI는 Absorb의 Progress를 표시하고 Snapshot을 다시 소비하지 않는다.
5. 한 번 눌러 시작하고 완료까지 시야·거리 조건을 유지하는 정책으로 먼저 구현한다. 누르고 있어야 하는 방식은 별도 입력 확장 후 비교한다.
6. 0.2/0.6/완료 직전 피격, 연타, 대상 파괴, 가득 참, 비활성화, 사망을 각각 재현한다. 완료 전에는 Store가 바뀌지 않아야 한다.

## 완료 기준

- [ ] 0.7초 유지 후 한 칸을 정확히 한 번 얻는다.
- [ ] 피격·대상 소멸·거리 이탈·비활성은 지급 없이 취소된다.
- [ ] 연타로 동시 흡수 작업이 늘어나지 않는다.
- [ ] 완료 직전 가득 차면 양쪽 상태가 보존된다.
- [ ] 진행 표시·거절 이유·취소 증거를 남겼다.

## 구조의 이득·한계

선택·진행·규칙을 나누면 취소 원인과 자원 버그를 따로 찾는다. 연결할 컴포넌트가 늘어난다. 단일 대상만 있는 초기 장면은 한 스크립트로 실험해도 되지만 시설과 보상이 함께 등장하면 이 경계가 필요해진다.
