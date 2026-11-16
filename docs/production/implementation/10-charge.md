# T10 — 짧은 발사와 충전·해제·취소 나누기

충전 사격을 추가하면서 누르는 순간 기본탄을 쏘고, 뗄 때 충전탄까지 쏘면 의도치 않게 두 발이 나온다. 입력 의미를 명시적으로 바꾼다.

선행: [T08](08-health.md) · 다음: [T11](11-feedback-pool.md)

## 이번 단계의 규칙

T07은 누르는 순간 기본탄을 발사했다. 여기서는 **짧게 눌렀다 떼면 기본탄, 충전 기준 이상 누른 뒤 떼면 충전탄**으로 교체한다. 충전 중 사망·메뉴·포커스 상실은 발사 없이 취소한다. 일반 비치명 피격의 충전 취소 여부는 실험 정책으로 기록한다. PRD가 반드시 요구한 피격 취소는 전력 흡수다.

## 변경 계약

| 대상 | 추가·변경 멤버 | 소유·처리 |
|---|---|---|
| Snapshot | `public bool FireHeld { get; }`; `public bool FireReleased { get; }` | FirePressed는 시작 요청으로 유지 |
| Snapshot 생성자·None | 기존 인자 뒤 `bool fireHeld, bool fireReleased` 추가 | 모든 생성 호출을 함께 갱신 |
| Reader | `private bool _fireHeld, _fireReleased` | 실제 유지값·해제 요청 |
| Reader.OnFire 변경 | performed: 시작 true·held true; canceled: 기존 held였으면 released true·held false | started와 performed에서 시작 중복 생성 금지 |
| Reader.CaptureSnapshot 변경 | held 복사, released 복사 후 false | 한 번 요청만 소비 |
| Reader.ClearAllInput 변경 | held·released도 false | 강제 정리는 해제 발사 요청이 아님 |
| Gun | `private bool _charging`; `private float _chargeElapsed`; `[SerializeField] private float _chargeThreshold = 0.7f` | 0.7은 충전 실험값, 흡수 규칙과 별개 |
| Gun | `public void TickCharge(bool pressed, bool held, bool released, Ray aimRay, float dt)` | 시작·누적·해제·한 발 확정 |
| Gun | `public void CancelCharge()` | 상태·진행률 삭제, 발사 없음 |
| Gun.TryFire 변경 | charged면 피해·속도 등 정한 설정 선택 | 발사 주체를 이 메서드 하나로 유지 |
| Controller.Update 변경 | FirePressed 직접 TryFire 제거, TickCharge로 교체 | 불허·포커스 상실이면 CancelCharge |

## 질문·의사코드

누름과 해제가 같은 입력 갱신에 오면 held가 false라도 짧은 발사가 가능한가? 메뉴 때문에 맵을 Disable한 canceled도 발사로 받아도 되는가?

```text
허용 안 됨 → CancelCharge, 발사 금지
pressed → 충전 시작
held → dt 누적
released이며 충전 중 → 시간 기준으로 한 발 요청 → 충전 종료
기존 충전 중인데 held도 released도 없음 → 포커스 등 강제 정리로 보고 취소
```

## TODO 실습

```csharp
public void CancelCharge()
{
    // TODO: 다음 해제가 발사로 처리되지 않도록 상태를 비운다.
}
```

힌트: 해제 요청과 강제 취소 구별 → 처리 순서를 pressed, held, released로 적기 → 해제 전에 충전 상태를 먼저 종료해 중복 방지.

## 따라하기·연결

1. Snapshot 필드·생성자·None·Reader 소비 코드를 한 번의 수정 단위로 바꾸고 컴파일한다. 인자 수 오류부터 해결한다.
2. Gun에 충전 타이머를 추가한다. Update 시간은 Time.deltaTime을 사용한다. 일시정지 중 시간이 진행되는지 T20에서 다시 검증한다.
3. Canvas 조준점 옆 Slider에 충전 비율을 표시한다. 표시를 위해 Snapshot을 두 번째로 가져오지 않는다. Gun의 읽기 전용 진행값 또는 이벤트를 추가해 사용한다.
4. charged 피해·크기·속도는 설정으로 분리한다. 같은 입력으로 기본탄과 충전탄이 함께 나오는지 확인한다.
5. 충전 후 Gate false, Reader 비활성, Alt+Tab, 플레이어 사망을 각각 시험한다. 취소 후 뗐을 때 발사되면 취소 경로를 점검한다.

## 완료 기준

- [ ] 짧은 누름/긴 누름이 각각 한 발로 구분된다.
- [ ] 같은 프레임의 누름·해제도 기본 발사로 처리한다.
- [ ] 강제 취소 후 해제로 탄환이 생기지 않는다.
- [ ] 충전 UI와 실제 판정이 같은 시간을 쓴다.
- [ ] 입력 규칙 변경 이유와 영상을 기록했다.

## 구조의 이유·한계

충전 시간을 무기가 소유하면 키와 게임패드가 바뀌어도 규칙이 유지된다. 입력 데이터와 생성자의 변경 비용이 생긴다. 처음부터 복잡한 상태 머신 도구를 넣는 대신 상태 두 개로 시작하고 과열·장전 등 전환이 늘면 명시적 enum으로 확장한다.
