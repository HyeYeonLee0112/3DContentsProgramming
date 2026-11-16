# T20 — 일시정지·사망·복원·승리의 행동 허용 통일하기

메뉴가 떠도 적 탄환이 날아오거나, 사망 뒤 입력이 풀리면 각 기능의 허용 규칙이 흩어진 것이다. 전체 상태의 주인을 한 곳에 둔다.

선행: [T19](19-encounters.md) · 다음: [T21](21-checkpoint-data.md)

## 책임·구현 계약

| 파일 | 필요한 멤버 | 역할·실패 |
|---|---|---|
| `Gameplay/GameState.cs` | `enum GameState { Initializing, Playing, Paused, Dead, Restoring, Won }` | 전체 상태 |
| `Gameplay/GameFlow.cs` | `public GameState State { get; private set; } = GameState.Initializing`; `public bool CanAct => State == GameState.Playing`; `public event System.Action<GameState> Changed`; `public void SetState(GameState next)` | 유효 전환·상태·파생 허용값; HP나 저장 처리 미소유 |
| `Gameplay/GameSession.cs` | `[SerializeField] private GameFlow _flow`; `[SerializeField] private PlayerControlGate _gate`; `[SerializeField] private PlayerInputReader _input`; `[SerializeField] private Health _playerHealth`; `private bool _initialized`; `private float _previousTimeScale`; `private CursorLockMode _previousCursorLock`; `private bool _previousCursorVisible` | 흐름 연결·전역 Unity 설정 적용과 복구 |
| Session | `public void Initialize()`; `private void OnEnable()/OnDisable()`; `private void OnPlayerDied()`; `private void OnFlowChanged(GameState state)` | 명시 연결 후 Playing; 사망 즉시 Dead 요청; 구독·해제는 초기화 여부 검사 |
| Session | `public void Pause()`; `public void Resume()`; `private void OnApplicationFocus(bool hasFocus)`; `private void OnDestroy()` | 유효 메뉴 전환·포커스 상실 정지·전역 설정 복구 |
| `Presentation/UI/PauseMenu.cs` | `[SerializeField] private GameSession _session`; `public void Resume()`; `public void Pause()` | UI가 세션에 요청 |
| Gun/Absorb/AI 변경 | Flow.Changed 구독·해제, 불허 상태에서 취소/정지 | 사망·정지 중 공격·흡수 완료 금지 |

이 분리는 [기술 설계 v0.1](../BLACKOUT_TechnicalDesign.md)의 기준이다. Flow는 상태를 소유하고 Session이 사망·메뉴·입력·커서·시간 적용을 연결한다. 각 기능은 자기 취소 함수를 구현하고 Flow의 상태를 받는다. T08·T14의 임시 Gate 변경 위치는 Session으로 합친다. T22의 실제 복원 적용은 Coordinator가 맡는다.

## 시간·전환 규칙

- Paused는 timeScale=0, 커서 해제, Gameplay 비활성화다. 메뉴 입력은 유지한다.
- Dead/Restoring/Won도 신규 전투를 막는다. 사망 표시 연출이 필요하면 unscaled 시간이나 별도 표현을 명시한다.
- Paused에서 Resume만 Playing으로 돌아간다. Dead에서 Resume로 부활시키지 않는다.
- T22 복원 전 Restoring, 성공 후 Playing이다. 실패 시 Dead에서 이유를 표시하고 안전하게 재시도한다.

```text
GameSession이 전환 요청 → GameFlow가 유효성 확인·State 갱신·Changed
Session이 입력·커서·시간 적용, 각 기능이 자기 작업 취소
재시도·복원은 알림 도중 바로 실행하지 않고 안전 지점에서 처리
```

## TODO 실습

```csharp
public void SetState(GameState next)
{
    // TODO: 현재 상태와 같은 요청은 중복 알림 없이 끝낸다.
    // TODO: 허용된 전환만 적용하고 Changed를 보낸다.
    // 시간·입력·커서는 GameSession.OnFlowChanged가 적용한다.
}
```

힌트: timeScale=0이어도 입력·일부 이벤트는 도착할 수 있음 → 타이머 정지만으로 취소했다고 생각하지 않기 → GameFlow 시작 전 참조 준비 확인.

## 따라하기·Unity 연결

1. 장면에 GameFlow·GameSession을 하나씩 두고 Session의 Flow·Gate·Reader·Health를 연결한다. SceneBootstrap 또는 준비 완료 담당이 Session.Initialize를 한 번 호출한다. Initializing에서는 게임 행동을 허용하지 않는다.
2. Pause 액션을 별도 Global/UI Map에 추가한다. Gameplay를 끄면 Resume 키까지 사라지는 구성을 피한다. 입력 생성 후 기존 콜백 인터페이스 변경 여부를 확인한다.
3. Canvas에 Pause·Dead·Won 패널을 만들고 현재 상태에 따라 표시한다. UI EventSystem은 InputSystemUIInputModule과 UI Actions를 연결한다. 게임 입력과 메뉴 클릭이 동시에 처리되지 않는지 본다.
4. Gun.CancelCharge, Interactor.CancelCurrent, 적 CancelAction을 상태 변경과 연결한다. 이미 비행 중인 탄환은 정지·정리 정책을 문서화한다.
5. Alt+Tab 때 자동 정지하고 복귀만으로 자동 재개하지 않는 정책을 적용한다. 사용자 Resume 이후 제어를 돌린다.
6. Play Mode를 끝내고 재진입했을 때 timeScale·커서·입력이 정상인지 확인한다.

## 완료 기준

- [ ] 메뉴·사망 중 신규 이동·발사·AI 공격·흡수가 없다.
- [ ] 정지 중 UI 조작과 재개가 가능하다.
- [ ] 충전/흡수 중 정지 후 재개해도 완료가 뒤늦게 발생하지 않는다.
- [ ] 포커스 상실·반복 실행 후 시간과 커서가 복구된다.
- [ ] 유효 전환표와 검사 영상을 남겼다.

## 선택과 한계

흐름 주인이 분명하면 사망·승리의 충돌을 찾기 쉽다. 기능마다 구독·취소를 연결해야 한다. 작을 때에는 직접 함수 호출로 연결해도 되며 범용 상태 머신 프레임워크는 필수가 아니다.
