# T01 — 짧은 클릭을 놓치지 않고 입력을 한 번 전달하기

> 문서: 있음 · 문서 검토 완료 · 점검일 2026-11-06
>
> Unity 6000.3.22f1 / URP 17.3.0 / Input System 1.20.0 · 실제 컴파일·플레이·빌드: 이번 문서 작업에서 미검증

발사 버튼을 아주 빨리 눌렀다 떼어도 게임은 한 번의 요청을 받아야 한다. 현재 Reader의 버튼 처리를 직접 검증하고 수정한다.

선행: [T00](00-baseline.md) · 다음: [T02](02-player-wiring.md) · 상세 개념: [기존 Reader 문서](../BLACKOUT_PlayerInputReader_Implementation.md)

## 먼저 볼 자료와 이번 선택

| 구분 | 자료와 읽을 부분 | 이 단계에서 참고할 것 |
|---|---|---|
| 직접 근거 | [Input Actions](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/manual/Actions.html) — callback phases·generated C# | started/performed/canceled와 생성 코드 |
| 유사 사례 | [Chop Chop 입력 설명](https://github.com/UnityTechnologies/open-project-1/wiki/Input) | 입력 담당을 이동·공격과 분리 |
| BLACKOUT용 설계 판단 | [입력 단일 소비 계약](CONTRACTS.md) | 이벤트 즉시 전달 대신 Snapshot으로 한 번 소비 |

웹 문서는 1.20.1을 표시할 수 있다. 로컬 설치 선언은 1.20.0이고 로컬 패키지 소스에서도 CallbackContext.ReadValueAsButton을 확인했다. Chop Chop의 ScriptableObject·이벤트 구조 전체를 그대로 사용하지 않는다.

## 개념과 파일 책임

`Aim`은 현재 누르고 있는 상태다. `Jump`, `Fire`, `Interact`는 다음 소비까지 보관할 요청이다. 요청은 버튼을 뗐다고 지우면 안 된다. 단, 이 bool 방식은 한 프레임 사이의 여러 클릭을 하나로 합친다. 모든 클릭 기록이 필요해질 때 큐를 검토한다.

대상: `Scripts/Runtime/Gameplay/Player/PlayerInputReader.cs`. 입력 객체와 요청은 Reader가 소유한다. 생성은 Awake, 등록 해제·Dispose는 OnDestroy다. 이동·피해는 알지 않는다.

## 변경할 멤버 계약

| 멤버 | 정확한 선언 | 역할·실패 처리 |
|---|---|---|
| 기존 요청 필드 | `private bool _jumpRequested, _fireRequested, _interactRequested` | 다음 소비까지 유지 |
| 점프 콜백 교체 | `public void OnJump(InputAction.CallbackContext context)` | performed 때 _jumpRequested=true |
| 발사 콜백 교체 | `public void OnFire(InputAction.CallbackContext context)` | performed 때 _fireRequested=true |
| 상호작용 콜백 교체 | `public void OnInteract(InputAction.CallbackContext context)` | performed 때 _interactRequested=true |
| 포커스 처리 | `private void OnApplicationFocus(bool hasFocus)` | 포커스 상실 때 `ClearAllInput()` 호출; 복귀만으로 사망·메뉴를 해제하지 않음 |
| 조준 콜백 변경 | `public void OnAim(InputAction.CallbackContext context)` | `context.ReadValueAsButton()`으로 유지 상태 읽기 |
| 기존 소비 | `public PlayerInputSnapshot CaptureSnapshot()` | 복사 후 일회 요청 삭제; 비활성 상태는 None |
| 기존 정리 | `private void ClearAllInput()` | 벡터·조준·요청 모두 기본값 |

호출자는 T02의 `PlayerController.Update` 하나다. 아직 없으면 중단점 또는 임시 개발 호출로 소비 결과를 본다. 소비 테스트용 두 번째 컴포넌트는 T02 연결 전에 제거한다.

## 먼저 생각하기와 의사코드

“눌림 → 해제 → 소비” 순서에서 bool을 현재 버튼값으로 덮어쓰면 무엇이 남는가?

```text
performed → 요청 true
canceled → 요청을 건드리지 않음
CaptureSnapshot → 현재 요청 복사 → 요청 false
포커스 상실·비활성화 → 모든 입력 삭제
```

## TODO 실습

아래는 기존 메서드 한 개를 교체할 자리다. 나머지 두 콜백도 계약대로 작성한다.

```csharp
public void OnFire(InputAction.CallbackContext context)
{
    // TODO: 실행 조건에 해당할 때만 _fireRequested를 true로 만든다.
    // 해제 콜백에서 false를 넣지 않는다.
}
```

힌트 순서: 현재 상태와 요청을 구별 → canceled로 요청을 덮지 않기 → `context.performed` 확인.

[세 콜백과 포커스 처리 참고 구현](answers/01-input-movement.md#input)

## 따라하기와 Unity 연결

1. 생성된 `BlackoutInputActions.cs`에서 AddCallbacks가 started/performed/canceled를 모두 등록하는 부분을 읽는다. 생성 코드는 수정하지 않는다.
2. 위 콜백 세 개를 고친다. `OnAim`은 `ReadValue<bool>()` 대신 `ReadValueAsButton()`으로 현재 누름 상태를 읽는다. 불필요한 UIElements using은 사용 여부를 확인해 정리할 수 있다.
3. GameObject에 Reader를 정확히 하나 붙인다. `PlayerInput` 컴포넌트를 별도 추가해 같은 입력을 중복 처리하지 않는다.
4. 기본 Update Mode가 Dynamic Update인지 프로젝트 Input System 설정을 확인한다. 이 안내는 Update에서 한 번 소비하는 구성이다.
5. `_fireRequested`가 바뀌는 줄과 Snapshot 반환 줄에 중단점을 걸어 빠른 클릭을 추적한다. 계속 찍는 Debug.Log는 검증 후 제거한다.

## 01-1. 현재 코드와 바꿀 지점을 찾기

1. 직접 작성한 `PlayerInputReader.cs`를 연다. 생성된 `BlackoutInputActions.cs`와 혼동하지 않는다.
2. 현재 OnFire/OnJump/OnInteract는 콜백마다 현재 버튼값으로 요청을 덮는다. 눌림 뒤 해제가 먼저 오면 소비할 요청이 사라질 수 있다.
3. OnAim은 유지 상태다. 버튼의 값 형식을 임의로 bool로 가정하지 않고 패키지가 제공하는 `ReadValueAsButton()`을 사용한다.
4. Snapshot 구조체의 필드·생성자·None, 기존 Awake/OnDestroy와 등록/해제는 보존한다. 이번에는 기존 콜백 네 개와 포커스 처리만 바꾼다.
5. 기존 Reader와 Snapshot은 전역 타입으로 유지한다. 뒤 단계의 namespace 안으로 복사해서 같은 이름의 타입을 추가하지 않는다.

## 01-2. 순서로 입력을 추적하기

| 순서 | Reader의 요청 | 소비 결과 |
|---|---|---|
| Fire performed | true로 보관 | 아직 소비하지 않음 |
| 같은 입력 갱신의 canceled | true를 유지 | 아직 소비하지 않음 |
| 첫 CaptureSnapshot | 복사 후 false | FirePressed true |
| 두 번째 CaptureSnapshot | false 유지 | FirePressed false |

이는 **소비 전에 도착한 같은 종류의 요청들을 하나로 합치는 방식**이다. 두 클릭 모두를 기록하는 입력 큐라고 부르지 않는다. 조준은 매번 현재 상태를 읽으므로 같은 보관 규칙을 적용하지 않는다.

### 단계별 힌트

<details>
<summary>힌트 1 — 무엇을 기억해야 하나</summary>

현재 눌림 상태와 “아직 전달하지 않은 행동 요청”을 각각 생각한다. 해제는 유지 상태를 끄지만 이미 받은 요청을 취소하지 않는다.

</details>

<details>
<summary>힌트 2 — 언제 지우나</summary>

요청은 Snapshot으로 복사한 직후 또는 비활성화·포커스 상실 때 지운다. 무기와 이동이 각자 Snapshot을 다시 가져가지 않게 한다.

</details>

<details>
<summary>힌트 3 — 패키지 API</summary>

`context.performed`는 실행 단계인지, `context.ReadValueAsButton()`은 버튼 누름 기준을 넘는 현재 값인지를 알려준다. 둘은 목적이 다르다.

</details>

[네 콜백·포커스 처리의 부분 교체 참고 구현](answers/01-input-movement.md#input)

## 코드가 연결되는 위치

Input System이 콜백을 호출하고 → 직접 작성한 Reader가 값을 보관하고 → T02 Controller가 Snapshot을 한 번 가져간다. `CaptureSnapshot`은 Unity가 자동 호출하는 함수가 아니다. `Awake/OnEnable/OnDisable/OnDestroy`는 Unity가 생명주기에 따라 호출한다. `_actions`는 직접 new로 만든 입력 래퍼이며 GameObject에 붙이는 컴포넌트가 아니다.

실제 짧은 입력이 처리된 시점을 중단점으로 비교한다. 로그가 필요하면 performed·canceled·소비 지점에 한시적으로 남기고, 매 프레임 문자열 출력을 계속 켜두지 않는다.

## 증상별 문제 해결

| 증상 | 먼저 확인할 곳 | 판단 |
|---|---|---|
| 버튼을 누르면 값 형식 오류 | OnAim의 읽기 API | ReadValueAsButton 사용 여부 |
| 아주 짧게 누르면 무반응 | canceled와 CaptureSnapshot 호출 위치 | 해제에서 요청 삭제 또는 소비자 중복 |
| 클릭 한 번에 두 번 반응 | Reader 개수·PlayerInput·다른 입력 코드 | 실제 입력 수집 경로가 둘인지 확인 |
| 다시 켠 뒤 요청이 반복 | AddCallbacks/RemoveCallbacks 수명 | OnEnable마다 추가 등록하지 않았는지 확인 |
| Alt+Tab 뒤 행동이 남음 | OnApplicationFocus·ClearAllInput | 과거 캐시 정리 여부; 실제 포커스 복귀도 확인 |

## 검증

| 순서 | 기대 결과 |
|---|---|
| 눌렀다 뗀 뒤 한 번 소비 | FirePressed true |
| 입력 없이 다시 소비 | FirePressed false |
| Aim 누른 채 여러 프레임 | IsAiming true 유지 |
| Gameplay 끄고 다시 소비 | 모든 값 기본값 |
| 버튼 누른 채 Alt+Tab | 복귀 후 과거 요청으로 점프·발사하지 않음 |

## 완료 기준

- [ ] 한 클릭의 요청이 한 번 전달된다.
- [ ] 조준은 누르는 동안 유지되고 해제하면 꺼진다.
- [ ] 비활성화·포커스 상실 뒤 과거 입력이 남지 않는다.
- [ ] 비활성화·재활성화 10회 후 콜백이 중복되지 않는다.
- [ ] 관찰값과 영상 또는 스크린샷을 기록했다.

## 왜 이 구조인가

입력을 임시 보관하면 무기가 실행되는 시점과 장치 콜백 시점이 달라도 요청을 유지한다. 비용은 언제 소비하고 지우는지 관리해야 한다는 점이다. 단순한 메뉴 버튼은 이벤트로 즉시 처리해도 되지만 게임 행동의 프레임 순서는 한 곳에서 정한다.

기록: 빠른 클릭 / 소비 두 번 / 포커스 / 재활성화 / 남은 한계.

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
