# T15 — 전력으로 조명·문·위험을 함께 바꾸기

소켓에 넣은 전력이 화면의 숫자만 바꾸면 핵심 플레이가 성립하지 않는다. 저장된 전력에서 통전 여부를 계산하고, 불빛·문·포탑은 같은 결과를 읽는다.

선행: [T13](13-absorption.md) · 다음: [T16](16-lift.md), [T18](18-turret.md)

## 책임과 수명

FacilityPower는 소켓의 Store와 요구량을 소유한다. FacilityView는 조명·문·표시만 바꾼다. 기존 `BlackoutSecurityGatePreview`는 환경 미리보기이므로 런타임 상태의 주인으로 사용하지 않는다. 같은 문 Transform을 두 컴포넌트가 동시에 바꾸지 않게 한다.

## 구현 계약

| 파일 | 필요한 멤버 | 동작·실패 |
|---|---|---|
| `Gameplay/Facilities/FacilityPower.cs` | `[SerializeField] private string _id`; `[SerializeField] private int _required = 1`; `public string Id => _id`; `public PowerStore Store { get; private set; }`; `public bool IsPowered => Store.Amount >= _required`; `public event System.Action Changed` | 안정적 ID·1~2칸 요구량·상태 알림 |
| Facility | `private void Awake()`; `public bool TryDeposit(PowerStore player)`; `public bool TryWithdraw(PowerStore player)`; `public void RestoreAmount(int amount)` | 초기화; 한 칸 이전 성공 뒤 Changed; 복원도 표시 갱신 |
| `Gameplay/Facilities/FacilitySocket.cs` | `[SerializeField] private FacilityPower _facility`; `[SerializeField] private PowerInventory _player`; `private bool _withdrawMode`; `public void SetWithdrawMode(bool withdraw)`; IInteractable 세 멤버 | 상호작용을 주입/회수 요청으로 연결 |
| `Presentation/VFX/FacilityView.cs` | `[SerializeField] private FacilityPower _facility`; `[SerializeField] private Light[] _lights`; `[SerializeField] private GameObject _poweredIndicators, _offIndicators`; `private void OnEnable()/OnDisable()`; `private void Refresh()` | 상태 구독·해제·최초 표시 |

주입/회수 선택은 접근 가능한 작은 패널 또는 별도 보조키로 명시한다. 에셋에 없는 입력을 문서만으로 존재한다고 가정하지 않는다. 두 칸 시설이 1칸만 받으면 비통전이며 그 1칸도 회수할 수 있다.

## 질문·의사코드

요구량 2 시설의 저장량이 1일 때 문은 열리는가? 회수 버튼의 실패 소리는 누가 재생하는가?

```text
주입/회수 요청 → PowerTransfer 한 칸 → 성공 뒤 Changed
View: 현재 IsPowered 읽기 → 불빛·표시·문 상태 적용
포탑: 같은 IsPowered를 읽어 행동 허용
```

## TODO 실습

```csharp
public bool TryDeposit(PowerStore player)
{
    // TODO: player에서 Store로 한 칸 이전한다.
    // TODO: 성공한 경우만 Changed를 발행한다.
    return false;
}
```

힌트: 두 Store가 변경된 뒤 알림 → OnEnable 최초 Refresh → 이벤트 수신자가 이전 함수를 재호출하지 않도록 표시만 처리.

## 따라하기·연결

1. 조명 회로 하나와 보안 문 회로 하나에 각각 고유 ID를 정한다. 예: `cargo.light.01`, `cargo.security.01`. GetInstanceID나 목록 순번을 저장 ID로 쓰지 않는다.
2. 시설 Root에 FacilityPower·Socket, 표시 자식에 View를 붙인다. 플레이어 참조와 조명 배열을 연결한다.
3. 문은 기존 미리보기 스크립트를 실습 복사본에서 비활성화하고 런타임 View/별도 문 표현이 제어하게 한다. 미리보기 코드를 삭제할 필요는 없다.
4. 주입 모드로 한 칸을 넣고 문·불빛·위험 표시가 같은 상태인지 확인한다. 두 칸 시설은 중간 상태를 UI에 1/2처럼 표시한다.
5. 회수 모드로 바꾸고 즉시 비통전되는지 본다. 문이 플레이어를 끼우는 경우 통과 중 닫힘 정책을 정하고 안전한 방향으로 이동시키거나 닫힘을 지연한다. 전력 상태를 거짓으로 통전 처리하지 않는다.
6. 완전 정전 때도 길·상호작용 지점은 읽히게 한다. 아트 바이블의 청록/적색을 형태·문구와 함께 사용한다.

## 완료 기준

- [ ] 한 칸 주입·회수가 실제 플레이어 보유량과 연결된다.
- [ ] 1/2칸 같은 미충족 상태를 구분한다.
- [ ] 조명·문·포탑 준비 상태가 같은 통전값을 쓴다.
- [ ] 표시를 껐다 켜도 현재 상태와 일치한다.
- [ ] 정전/통전 비교와 문 끼임 정책을 기록했다.

## 이득·비용

상태와 표시를 나누면 문 모양을 바꿔도 전력 규칙은 유지된다. 구독·초기 표시 관리가 추가된다. 작은 회로는 한 View로 시작하고 모든 조명에 개별 이벤트 객체를 만들 필요는 없다.
