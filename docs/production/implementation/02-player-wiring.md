# T02 — 플레이어를 조립하고 입력 소비를 한 곳으로 모으기

> 문서: 있음 · 문서 검토 완료 · 점검일 2026-11-06
>
> Unity 6000.3.22f1 / URP 17.3.0 / Input System 1.20.0 · 실제 컴파일·플레이·빌드: 이번 문서 작업에서 미검증

이동과 총이 각자 입력 묶음을 가져가면 먼저 가져간 쪽만 버튼 요청을 받는다. 한 컴포넌트가 입력을 한 번 읽고 필요한 곳에 전달하도록 만든다.

선행: [T01](01-input.md) · 다음: [T03](03-movement.md) · [공통 호출 순서](CONTRACTS.md)

## 먼저 볼 자료와 이번 선택

| 구분 | 자료와 읽을 부분 | 이 단계에서 참고할 것 |
|---|---|---|
| 직접 근거 | [Unity 설계 책](https://unity.com/kr/resources/design-patterns-solid-ebook) — 단일 책임 원칙 | 입력 전달과 실제 이동 규칙 분리 |
| 직접 근거 | [Unity 실행 순서](https://docs.unity.com/en-us/engine/6000.3/manual/scripting/managing-update-order/execution-order) — Limitations | 서로 다른 Awake·Update 순서를 추측하지 않기 |
| BLACKOUT용 설계 판단 | [전체 구조와 초기 실습의 차이](../BLACKOUT_TechnicalDesign.md#migration) | 작은 Controller에서 시작해 후속 Driver·Session으로 이동 |

Controller라는 이름만으로 MVC 구조라고 부르지 않는다. 이번 역할은 입력 소비와 협력 대상 호출이다.

## 책임·소유권

`PlayerController`는 호출 순서만 정한다. `PlayerControlGate`는 지금 행동해도 되는지 한 값을 보관한다. T02~T07은 Gate의 시작값 true를 쓰는 실습 임시 정책이고, T08에서는 Controller가 사망 때 닫는다. T20 통합에서는 GameFlow 정책을 GameSession만 동기화한다. 여러 기능이 임의로 true/false를 덮으면 사망 뒤 조작이 풀릴 수 있다.

| 파일 | 생성·종료 / 협력 | 알아서는 안 되는 것 |
|---|---|---|
| `Gameplay/Player/PlayerControlGate.cs` | Player_Root와 함께 생성; 초기 실습·이후 GameSession이 변경 | 키·체력 계산 |
| `Gameplay/Player/PlayerController.cs` | Player_Root와 함께 생성; Reader·Gate 참조 | 중력·탄환 충돌 세부 |

## 구현 계약

| 타입 | 직접 작성할 멤버 | 핵심 로직·실패 |
|---|---|---|
| Gate | `public bool Allowed { get; private set; } = true` | 현재 허용값 |
| Gate | `public void SetAllowed(bool allowed)` | 값 변경; 기존과 같아도 안전 |
| Controller | `[SerializeField] private PlayerInputReader _input` | Inspector 참조 |
| Controller | `[SerializeField] private PlayerControlGate _gate` | Inspector 참조 |
| Controller | `public PlayerInputSnapshot LastInput { get; private set; }` | 이번 프레임 진단값 |
| Controller | `private void Awake()` | 필수 참조 누락을 명확히 한 번 로그하고 자신 비활성화 |
| Controller | `private void OnDisable()` | LastInput을 None으로 정리; 이후 추가되는 발사 의도도 정리 |
| Controller | `private void Update()` | Snapshot 한 번 소비; 불허면 None 사용 |

기존 `Player.cs`와 역할이 겹친다. 새 Controller를 채택하면 빈 Player 컴포넌트는 Inspector에서 사용 여부를 확인한 뒤 제거한다. 파일을 임의로 삭제하지 않는다.

## 생각할 질문·의사코드

Gate가 false여도 입력을 소비하면 무엇이 좋아지는가? 다시 true가 된 첫 프레임에 예전 클릭이 실행돼도 되는가?

```text
Awake → 참조 검사
Update → 입력 한 번 소비 → Allowed면 보관, 아니면 None
뒤 단계에서 이 묶음을 Motor·Facing·Gun에 전달
```

## TODO 실습

클래스에 표의 필드를 선언한 후 아래 메서드를 채운다. 아직 없는 Motor·Camera·Gun 참조는 넣지 않는다.

```csharp
private void Update()
{
    // TODO: _input에서 한 번 가져온다.
    // TODO: _gate.Allowed에 따라 LastInput을 결정한다.
}
```

힌트: 입력값을 로컬 변수 하나에 담기 → 조건에 따라 저장 → 다른 기능에서 CaptureSnapshot 검색 결과가 없어야 함.

## 따라하기와 Inspector

1. 기존 Player_Root와 자식 Visual·CameraTarget·WeaponSocket을 확인한다. 이 이름은 표시명이므로 문서 이름으로 강제 변경할 필요는 없다.
2. Root의 스케일을 `(1,1,1)`로 맞출 수 있는지 자식 모델 스케일을 검토한다. 모델을 직접 이동 주인으로 삼지 않는다.
3. Gate·Controller 스크립트를 각각 만들고 컴파일 후 Root에 붙인다. 필드에 같은 Root의 Reader·Gate를 드래그한다.
4. Update의 LastInput 값을 IDE Watch로 확인한다. 기능 없이 입력 경로만 동작하는 최소 단계를 먼저 끝낸다.
5. Root를 `Prefabs/Characters/Player_Blackout.prefab`으로 저장한다. 같은 장면에 플레이어와 AudioListener가 중복되지 않는지 확인한다.
6. T03에서 Motor 참조·호출을 추가하고 T05~07에서 카메라·총을 차례로 연결한다.

## 02-1. 스크립트의 종류와 연결 순서

| 대상 | 만드는 방법 | Unity에서 연결하는 곳 |
|---|---|---|
| PlayerControlGate | C# 파일 → MonoBehaviour | Player_Root의 Add Component |
| PlayerController | C# 파일 → MonoBehaviour | 같은 Player_Root의 Add Component |
| Reader | 기존 컴포넌트를 사용 | Controller의 Input 필드 |
| Gate | 위에서 붙인 개체를 사용 | Controller의 Gate 필드 |

1. 두 새 파일에 `using UnityEngine;`와 `namespace Blackout.Gameplay.Player`를 사용한다. 기존 Reader에는 namespace를 새로 붙이지 않는다.
2. Gate에는 표의 Allowed와 SetAllowed만 작성한다. 키·HP·총을 참조하지 않는다.
3. Controller는 Awake에서 자기 필드가 연결되었는지만 검사한다. 다른 객체의 Awake가 먼저 실행되었다고 가정해 SetGameplayEnabled를 호출하지 않는다.
4. Input이 없으면 `Debug.LogError("PlayerController: Input 참조가 없습니다.", this)`처럼 개체 문맥을 포함해 한 번 알리고 Controller를 비활성화한다. Gate 누락도 같은 정책이다.
5. Update에서 Snapshot을 로컬 변수 하나로 받고, Gate 허용에 따라 LastInput을 정한다. 아직 이동하지 않는 것이 이 단계의 예상 결과다.

### 단계별 힌트

<details>
<summary>힌트 1 — 누가 입력을 가져오나</summary>

Gate는 허용 결과만 제공한다. Controller가 Snapshot을 가져온 뒤 여러 행동에 같은 값을 전달한다.

</details>

<details>
<summary>힌트 2 — 조작을 막았을 때</summary>

입력 소비 자체를 건너뛰면 과거 요청이 남을 수 있다. 소비한 뒤 사용할 입력을 None으로 바꾼다.

</details>

<details>
<summary>힌트 3 — 실제 호출부</summary>

직접 작성한 `CaptureSnapshot()`을 호출한다. `Update`, `enabled`, `Debug.LogError`는 Unity가 제공한다.

</details>

[Gate·Controller 전체 파일 참고 구현](answers/01-input-movement.md#wiring)

## 02-2. 실행하며 연결을 확인하기

1. 스크립트를 저장하고 Console의 첫 컴파일 오류부터 해결한다. 컴포넌트를 붙일 수 없으면 파일명·클래스명·상속·첫 오류를 확인한다.
2. Controller의 Input·Gate 필드에 **같은 Player_Root**의 개체를 드래그한다.
3. Play에서 IDE 중단점·Watch로 LastInput을 확인한다. C# 속성은 기본 Inspector에 자동 표시되지 않는다.
4. 디버거의 즉시 실행·식 평가에서 `_gate.SetAllowed(false)`를 호출해 불허 상태를 시험하고, 클릭 뒤 true로 바꿔 오래된 요청이 나오지 않는지 확인한다. 디버거 실행이 지원되지 않으면 임시 개발 버튼을 사용하되 별도 입력 소비자는 만들지 않고 검사 뒤 제거한다.
5. Root를 프리팹으로 저장하고 검증 복사본에서 재배치한다. 원래 Player.cs는 다른 사용처를 확인한 뒤 컴포넌트 연결만 정리하며 파일을 임의로 삭제하지 않는다.

T08 이후에는 사망을 무시하고 디버거로 true를 강제로 넣는 것을 정상 재개 경로로 사용하지 않는다. T20의 세션 연결 전까지 자동 부활 기능은 없다.

## 검증과 오류

- Reader 참조를 잠깐 비우면 구체적인 누락 오류 한 번으로 멈춰야 한다. 매 프레임 NullReferenceException을 만들지 않는다.
- Gate false 중 클릭하고 true로 바꿔도 보관된 발사가 실행되면 안 된다.
- 비활성 Root를 다시 켠 뒤 콜백 수가 두 배가 되면 T01 등록·해제를 본다.

## 완료 기준

- [ ] 입력 묶음을 소비하는 호출 위치가 하나다.
- [ ] 조작 불허 중 요청이 쌓이지 않는다.
- [ ] 참조 누락의 위치와 이유를 알 수 있다.
- [ ] 프리팹 재배치 후 입력 경로가 같다.

## 선택 이유와 한계

얇은 조정자는 호출 순서를 읽기 쉽게 하지만 Inspector 연결이 필요하다. 처음부터 전역 관리자나 DI 패키지를 넣을 필요는 없다. 한 파일이 단순한 경우 직접 참조가 가장 적은 비용의 대안이다.

기록: 프리팹 / 참조 누락 결과 / 불허 중 클릭 / 증거.

## 실습 기록 — 실제 확인 후 작성

아래는 빈 기록 양식이다. 문서 검토와 구현 결과를 구분하며 실행하지 않은 항목은 미확인으로 남긴다.

```text
태스크 / 확인 날짜:
직접 작성·변경한 파일 / Inspector 설정:
재현 순서:
예상 결과 / 실제 관찰:
정상·실패·취소·중복·재활성화 결과:
Console 첫 오류 또는 오류 없음 확인 조건:
영상·스크린샷·빌드 위치:
원인 근거 / 선택한 해결 / 추가 비용 / 남은 한계:
```
